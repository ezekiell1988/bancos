# Guía de diagnóstico y resolución — Importación masiva y parsers

> Creado: 2026-07-20 | Tarea: TASK-EBC-INF-08
> Aplica a: `Features/Imports/ImportJobs.cs`, `Features/Parsing/`, `Features/Classification/`

Este documento captura los errores encontrados durante la validación completa de los 19 archivos de `src/input.zip` y los patrones de solución para reutilizar en futuros formatos.

---

## 1. Race condition en creación de categoría "General"

### Síntoma
`InvalidOperationException: Sequence contains more than one element` en `ClassificationModule.cs` línea 114.  
Algunos imports quedaban en `status=3 Failed` con "La importación no pudo procesarse."

### Causa raíz
Hangfire arranca 5 workers en paralelo. Cada worker tiene su propio `DbContext`. Al clasificar transacciones, todos buscan la categoría General — si ninguno la encuentra (BD vacía), todos crean `new Category { Name = "General" }` y llaman `SaveChangesAsync`. El índice único `(Name, ParentId)` permite esto porque `ParentId IS NULL` en todos. Resultado: 4–5 filas duplicadas de "General" en BD. La siguiente llamada a `SingleOrDefaultAsync` lanza la excepción.

### Solución aplicada
1. **`ClassificationModule.cs`**: `SingleOrDefaultAsync` → `FirstOrDefaultAsync` (evita el crash cuando ya hay duplicados).
2. **`BancosDbContext.SeedDefaults`**: Se agregó "General" como dato semilla con ID fijo `00000000-0000-0000-0000-000000000301` para que siempre exista desde la migración.
3. **Migración `SeedGeneralCategory`**: Aplica el seed a BD.

### Lección para futuros formatos
- Cualquier entidad "singleton de referencia" (categorías raíz, cuentas por defecto) debe estar en seed data de migración, no crearse on-demand desde el job.
- Nunca usar `SingleOrDefaultAsync` en un contexto donde múltiples jobs concurrentes podrían insertar el mismo registro.

---

## 2. Imports atascados en status=Processing (nunca pasan a Failed)

### Síntoma
Tres imports de `coopealianza-loan-pdf-v1` quedaban permanentemente en `status=1` (Processing) después de que Hangfire reportaba el job como `Failed`.

### Causa raíz
Cuando `db.SaveChangesAsync()` lanza una excepción (p.ej. `DbUpdateException` por clave duplicada), el `DbContext` queda en estado inconsistente — las entidades problemáticas siguen rastreadas como `Added`. El bloque `catch (Exception)` intenta:
```csharp
import.Status = ImportStatus.Failed;
await db.SaveChangesAsync(); // TAMBIÉN falla porque el contexto sigue rastreando la entidad duplicada
```
El segundo `SaveChangesAsync` también lanza. La excepción resultante sale del catch y sube a Hangfire (job = Failed), pero el import en BD **nunca se actualizó** a Failed — permanece en Processing.

### Solución aplicada
En ambos bloques `catch`, limpiar el change tracker y re-attachear solo el import antes de intentar guardar el fallo:
```csharp
catch (Exception)
{
    db.ChangeTracker.Clear();       // desacopla entidades problemáticas
    db.Imports.Attach(import);      // re-attachea solo el import
    import.Status = ImportStatus.Failed;
    import.FailureReason = "La importación no pudo procesarse.";
    await db.SaveChangesAsync();    // ahora solo guarda el import
    throw;
}
```

### Lección para futuros formatos
- **Patrón obligatorio** en todo `catch (Exception)` de `ProcessAsync`: `ChangeTracker.Clear()` + `Attach(import)` antes de guardar el fallo.
- Sin este patrón, cualquier `DbUpdateException` (por clave duplicada, constraint violada, etc.) dejará el import atascado en Processing para siempre.

---

## 3. Race condition en inserción de LoanStatements (mismo fingerprint, jobs concurrentes)

### Síntoma
Tres de los cuatro PDFs de Coopealianza tenían el mismo contenido financiero (mismo saldo y pagos) aunque diferente nombre de archivo. Los cuatro jobs corrían simultáneamente:
- Job 1: Lee BD → no hay LoanStatement → inserta → OK
- Jobs 2, 3, 4: Leen BD → no hay LoanStatement (aún no committeado) → intentan insertar → `DbUpdateException: Cannot insert duplicate key... IX_LoanStatements_AccountAuxiliaryId_SourceFingerprint`

### Solución aplicada
Guardar el LoanStatement **antes** de continuar, con manejo específico del caso "el ganador ya insertó":
```csharp
db.LoanStatements.Add(statement);
try { await db.SaveChangesAsync(); }
catch (DbUpdateException)
{
    db.ChangeTracker.Clear();
    if (!await db.LoanStatements.AnyAsync(x => x.AccountAuxiliaryId == import.AccountAuxiliaryId && x.SourceFingerprint == loanFingerprint))
        throw; // fallo genuino, no es race condition
    // Ganó el job concurrente — ya existe, continuar normalmente
    db.Imports.Attach(import); // re-attachear para que el status=Completed se guarde
}
```

### Lección para futuros formatos
- Cuando múltiples archivos pueden representar los mismos datos (mismo extracto de diferentes períodos, por ejemplo), la fingerprint basada en contenido financiero puede colisionar.
- El patrón "try-insert, catch-duplicate, verify-exists" es la forma correcta de manejar race conditions en Hangfire. El lock de BD (select-then-insert) no es confiable en múltiples workers.

---

## 4. El finally block borra el archivo temporal aunque el status no se guardó

### Síntoma
Después de una ejecución fallida, al reintentar los imports, todos fallaban con `FileNotFoundException` — los archivos `.upload` habían sido eliminados.

### Causa raíz
```csharp
finally { if (import.Status == ImportStatus.Completed && File.Exists(import.TemporaryPath)) File.Delete(import.TemporaryPath); }
```
`import.Status` se evalúa del **objeto en memoria**, no del valor en BD. Cuando el `SaveChangesAsync` de línea 87 falla (o no guarda por context cleared sin re-attach), `import.Status` en memoria sigue siendo `Completed` aunque BD diga `Processing`. El archivo se borra.

### Solución relacionada
El re-attach correcto (`db.Imports.Attach(import)`) asegura que la BD quede en sync con la memoria. Si se necesita reintentar un import cuyos archivos ya fueron borrados, se deben restaurar manualmente del ZIP original:
```bash
BASE="src/Bancos.Api/.local-secrets/imports"
unzip -p src/input.zip "ruta/en/zip/archivo.pdf" > "$BASE/<uuid>.upload"
# El UUID se obtiene de la columna TemporaryPath en la tabla Imports
```

### Lección para futuros formatos
- El endpoint `POST /api/imports/{id}/retry` solo funciona si el archivo temporal todavía existe.
- Si se hacen múltiples reintetnos manuales después de un fallo, verificar que los archivos `.upload` estén presentes antes de reintentar.

---

## 5. BalanceRegex de Coopealianza captura solo el primer dígito

### Síntoma
`LoanStatement.OutstandingBalance = 4` en lugar de `4372249.85`.

### Causa raíz
PdfPig extrae el texto del PDF de Coopealianza (generado por Bankingly) usando **non-breaking space** (U+00A0) como separador de miles, no el espacio ASCII regular (U+0020). El texto real es:
```
Saldo actual:₡ 4 372 249,85
```
La regex original `[\d., ]` solo incluye U+0020 en la clase de caracteres. Al encontrar U+00A0 después del "4", la captura se detiene y el grupo `balance` solo contiene `"4"`.

`MoneyParser.TryParse("4", ...)` devuelve `4m` sin error — fallo silencioso.

### Solución aplicada
Cambiar el espacio literal por `\s` en la clase de caracteres de la regex:
```csharp
// Antes:
[GeneratedRegex(@"(?i)Saldo actual:₡\s*(?<balance>[\d][\d., ]*)")]

// Después:
[GeneratedRegex(@"(?i)Saldo actual:₡\s*(?<balance>[\d][\d.,\s]*)")]
```
`\s` en .NET incluye U+0020, U+00A0, U+0009, U+000A y otras variantes de whitespace Unicode.

### Comportamiento de PdfPig relevante para parsers PDF
| Característica | Descripción |
|---|---|
| Sin saltos de línea | `page.Text` concatena todo el texto sin `\n`. Usar regex sin `^`/`$`. |
| Separadores de miles | Pueden ser U+0020, U+00A0 o U+202F (thin space). Usar `\s` o `[\s\p{Zs}]` en clases de cantidad. |
| Delimitadores naturales | Usar caracteres especiales del dominio (₡, $, símbolos) como anclas en lugar de whitespace. |
| Concatenación de tokens | Texto de celdas contiguas se pega sin separador: `"08/07/2026Pago₡242 852,70"`. Diseñar regex para esto. |

### Lección para futuros formatos PDF (Bankingly, BCCR, etc.)
- Siempre usar `\s` en lugar de espacio literal en regex que capturen cantidades monetarias.
- Para delimitar entre campos en PDFs sin estructura, preferir símbolos del dominio (₡, $) o patrones de fecha como anclas.
- El patrón `₡[^₡]*` es más robusto que `₡[\d.,\s]*` para capturar un monto hasta el siguiente símbolo ₡.

---

## 6. PaymentRegex de Coopealianza con PdfPig

### Síntoma
`LoanStatement.LoanPayments = 0` — no se detectaban pagos.

### Causa raíz
La regex original esperaba whitespace entre campos (`\s+`) porque el parser estaba diseñado asumiendo texto con saltos de línea. El texto real de PdfPig es concatenado sin separadores:
```
593661608/07/2026Pago₡242 852,70₡67 303,60₡ 0,00₡15 080,84₡325 237,14₡4 372 249,85
```
Estructura real: `{TxNum}{dd/MM/yyyy}Pago{₡Capital}{₡Interés}{₡Mora}{₡Otros}{₡Total}{₡SaldoRestante}`

### Solución aplicada
Usar el símbolo ₡ como delimitador natural entre campos:
```csharp
[GeneratedRegex(@"(?<date>\d{2}/\d{2}/\d{4})Pago(?<capital>₡[^₡]*)(?<interest>₡[^₡]*)(?<lateFee>₡[^₡]*)(?<other>₡[^₡]*)(?<total>₡[^₡]*)₡")]
```
`[^₡]*` captura todo hasta el siguiente ₡ — incluye dígitos, comas, puntos, espacios y non-breaking spaces sin necesitar listarlos explícitamente.

---

## 7. Exchange rates requeridos para tarjetas de crédito en USD

### Síntoma
Import `bac-credit-csv-v1` fallaba con: `"No existe tipo de cambio USD para la fecha 2026-05-18"`.

### Causa raíz
El CSV de tarjeta de crédito BAC tiene columnas separadas para transacciones en colones (`Local`) y dólares (`Dollars`). Las transacciones en USD no incluyen el equivalente en CRC — el sistema necesita consultar la tabla `ExchangeRates` para convertir.

La tabla `ExchangeRates` estaba vacía en la BD recién creada.

### Solución aplicada
Insertar tipos de cambio de referencia en la tabla `ExchangeRates`. Para BD de desarrollo:
```sql
-- Ejecutar en el contenedor Docker
docker exec bancos-sql-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'DevLocal_Bancos1!' -d dbbancos -No -Q "
DECLARE @rate DECIMAL(18,4) = 519.50;
DECLARE @d DATE = '2026-05-01';
WHILE @d <= '2026-07-31'
BEGIN
    INSERT INTO dbo.ExchangeRates (Id, RateDate, CurrencyCode, CrcPerUnit, CreatedUtc)
    VALUES (NEWID(), @d, 'USD', @rate, GETUTCDATE());
    SET @d = DATEADD(DAY, 1, @d);
END"
```

### Lección para futuros formatos con moneda extranjera
- Antes de importar archivos con transacciones USD (o cualquier moneda no-CRC), verificar que la tabla `ExchangeRates` tenga datos para el rango de fechas del archivo.
- En BD de desarrollo, usar un tipo de cambio aproximado (ej. 519.50 CRC/USD).
- En producción, el sistema deberá cargar tipos de cambio oficiales (BCCR API o similar).

---

## 8. Formato BAC crédito CSV — columnas Local/Dollars

### Descripción
El CSV de estados de tarjeta BAC usa un formato con dos columnas de monto en lugar de una:

| Date | (descripción sin header) | Local | Dollars |
|------|--------------------------|-------|---------|

- Si hay valor en `Local` → transacción en CRC
- Si hay valor en `Dollars` → transacción en USD (requiere exchange rate)

El parser `ParseBacDualAmountRows` en `CardStatementParser.cs` maneja este caso cuando no se detecta el header estándar `Amount`.

---

## 9. Formato BAC crédito PDF online — PdfPig sin saltos de línea

### Descripción
Los PDFs del estado de cuenta online de BAC tienen texto concatenado sin newlines. Formato real:
```
16/06/2026DLC*UBER EATS\ SAN JOSE\ CRI6,166.00 CRC17/06/2026...
```

El parser `ParseBacOnlinePdfConcatenated` en `CardStatementParser.cs` detecta la tabla por el marcador `"fechaconceptomonto colones"` y usa lookahead de fecha para delimitar registros.

**Falso positivo detectado:** El header de la sección "resumen de saldos" también contiene patrones similares. Se usa la firma `"tarjeta de credito" + "saldo en colones" + "saldo en dolares"` para detectar y descartar el snapshot antes de intentar parsear movimientos.

---

## 10. Endpoint de reintento de importaciones

### Descripción
Se agregó `POST /api/imports/{id}/retry` para re-encolar imports fallidos o atascados sin necesidad de volver a subir el archivo.

### Restricción importante
Solo funciona si el archivo temporal (`.local-secrets/imports/{uuid}.upload`) todavía existe. Los imports con `status=2 Completed` tienen el archivo eliminado — para reintentarlos se debe restaurar el archivo del ZIP original.

### Cuándo usar
- Import en `status=3 Failed` con causa conocida y corregida
- Import en `status=1 Processing` atascado (Hangfire job ya terminó pero el status no se actualizó)

---

## Checklist para diagnóstico de imports fallidos

```
[ ] ¿El job de Hangfire está en estado Failed o Succeeded?
    → Failed con DbUpdateException: ver sección 2 (context poisoning)
    → Failed con FileNotFoundException: restaurar archivo .upload del ZIP
    → Succeeded pero status=1: ver sección 3 (race condition sin re-attach)
    → Failed con "No existe tipo de cambio": ver sección 7 (exchange rates)

[ ] ¿El import quedó en status=1 Processing permanentemente?
    → El job de Hangfire falló y el catch también falló (sección 2)
    → Verificar: ¿ChangeTracker.Clear() + Attach(import) en ambos catch blocks?

[ ] ¿El balance del préstamo es un número pequeño (ej. 4)?
    → Regex usando espacio literal en lugar de \s (sección 5)
    → Verificar con el texto real: imprimir page.Text del PDF en un test

[ ] ¿Los pagos del préstamo están vacíos?
    → Regex espera \s+ entre campos pero PdfPig concatena sin separadores (sección 6)
    → Reescribir regex usando delimitadores del dominio (₡, $, fecha)

[ ] ¿El archivo fue importado como BalanceSnapshot en lugar de Movements?
    → El PDF tiene una sección de resumen que se detecta antes de los movimientos
    → Revisar orden de chequeos en ParsePdf y las firmas de detección
```
