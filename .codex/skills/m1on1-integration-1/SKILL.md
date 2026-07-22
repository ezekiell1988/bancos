---
name: m1on1-integration-1
description: >
  Arquitectura completa, bugs corregidos y guia de extension de la Integracion 1 de Marketing1on1:
  proceso Hangfire que sincroniza facturas desde SQL Server externo (del cliente) hacia
  Elasticsearch. Cubre el flujo de 3 jobs (Planner -> Chunks -> Finalizer), estado en Redis,
  repositorios SQL/ES, sanitizacion de documentos, uso obligatorio de idClient y avance manual
  por endpoint de status (sin SignalR).
  Usar SIEMPRE que se trabaje con IntegrationOne, sus jobs, repositorios o estado Redis.
applyTo:
  - "src/MarketingOneOnOneApi/Jobs/IntegrationOne/**"
  - "src/MarketingOneOnOneApi/Infrastructure/IntegrationOne/**"
  - "src/MarketingOneOnOneWeb/src/app/pages/process/integration-one/**"
  - "src/MarketingOneOnOneWeb/src/app/service/integration-one-progress.service.ts"
  - "scripts/create-clickeat-invoices-index.sh"
---

# Integration One - SQL Server Externo -> Elasticsearch (Facturas)

## 1. Que hace esta integracion

Sincroniza facturas del cliente (tenant) desde su SQL Server externo hacia el indice
Elasticsearch de Marketing1on1 para facturas.

El connection string al SQL externo vive en `ClientIntegration.Param1` codificado en Base64.

Para Integration 1, el request de inicio debe incluir `idClient` desde frontend.
El API lo valida (`> 0`) y usa ese valor para:

- `IntegrationOneStartRequest.IdClient`
- `IntegrationOneState.IdClient`
- cada documento indexado en Elasticsearch (`idClient`)

El proceso es incremental por defecto: indexa solo facturas con `idInvoice` mayor al maximo
ya indexado en ES. Se puede forzar reindex total con `ForceReindex: true`.

Importante: Integration 1 NO usa SignalR para progreso. El avance se consulta manualmente
con `GET /api/v1/client-integration/status/{integrationJobId}`.

---

## 2. Arquitectura de Jobs (Hangfire)

```
Controller POST /api/v1/client-integration/1/start
      |
      v
IIntegrationOnePlannerJob.PlanAsync()      <- Job 1/3 (queue: "integration", AutomaticRetry=0)
      |
      |- Determina total de pendientes (SP spElasticSearchInvoiceGetTotal)
  |- Define cursorStart fijo por corrida (startFromId)
      |- Inicializa estado en Redis
      \- Encola N x IIntegrationOneChunkJob  -- paralelo

IIntegrationOneChunkJob.ProcessChunkAsync() <- Job 2/3 (AutomaticRetry=3)
      |
  |- Lee lote SQL (SP spElasticSearchInvoiceGet) usando page = chunkId + 1
      |- Sanitiza documentos
      |- Estampa idClient en cada documento
      |- Bulk index en Elasticsearch
      \- Incrementa contadores atomicos en Redis
             |
             \- Si fue el ultimo chunk -> encola Finalizer

IIntegrationOneFinalizerJob.FinalizeAsync() <- Job 3/3
      |
      \- Calcula status final: "completed" o "completed_with_errors"
```

---

## 3. Archivos y responsabilidades

| Archivo | Responsabilidad |
|---|---|
| `Jobs/IntegrationOne/IntegrationOnePlannerJob.cs` | Job 1: planificacion, validaciones, calculo de cursores, encolado de chunks |
| `Jobs/IntegrationOne/IntegrationOneChunkJob.cs` | Job 2: lectura SQL -> sanitizacion -> bulk ES -> contadores |
| `Jobs/IntegrationOne/IntegrationOneFinalizerJob.cs` | Job 3: cierre del proceso y status final |
| `Jobs/IntegrationOne/IntegrationOneRedisHelper.cs` | Estado en Redis: state, chunks y counters |
| `Jobs/IntegrationOne/IntegrationOneJobState.cs` | DTOs request/response/state/chunk |
| `Infrastructure/IntegrationOne/SqlIntegrationOneRepository.cs` | Lectura de SPs del SQL externo |
| `Infrastructure/IntegrationOne/ElasticIntegrationOneRepository.cs` | Creacion/recreacion de indice, max id, bulk index, sanitizacion |
| `service/integration-one-progress.service.ts` | Frontend: start + consulta manual de estado |
| `scripts/create-clickeat-invoices-index.sh` | Script para recrear indice `clickeat-invoices` con mapping explicito |

---

## 4. Contrato API (Integration 1)

### Endpoint de inicio

`POST /api/v1/client-integration/1/start`

Body:

```json
{
  "idClient": 123,
  "batchSize": 5000,
  "forceReindex": false,
  "forceFromId": null
}
```

Reglas:

- `idClient` es obligatorio y debe ser `> 0`
- si falta o es invalido, responde `400 BadRequest`
- `batchSize <= 0` se normaliza a default en backend (1000)

### Endpoint de estado

`GET /api/v1/client-integration/status/{integrationJobId}`

Devuelve progreso, contadores, status y error si aplica. El frontend usa este endpoint para
el boton `Ver avance`.

---

## 5. Estado en Redis

Claves principales:

- `integration-one:{jobId}:state` (JSON global)
- `integration-one:{jobId}:chunk:{id}` (JSON por chunk)
- `integration-one:{jobId}:counters` (hash de contadores)

TTL esperado: 48 horas.

### Regla de contadores

Los contadores del hash son fuente de verdad para metricas numericas:

- `completedChunks`
- `failedChunks`
- `totalIndexed`
- `totalErrors`

`GetStateAsync` debe sobreescribir estos campos del JSON de estado con valores del hash.

---

## 6. Stored Procedures esperados

### `spElasticSearchInvoiceGetTotal`

Input:

- `@json` con `{"idInvoice": <lastId>}`

Output:

- `totalPending`
- `totalInvoices`

### `spElasticSearchInvoiceGet`

Input:

- `@json` con `{"idInvoice": <cursorStart>, "page": <N>, "itemPerPage": <batchSize>}`

Output:

- filas de facturas con `idInvoice > cursorStart`
- maximo `itemPerPage` filas
- `idInvoice` debe existir y ser numerico (id de documento)

Nota operativa importante:

- `cursorStart` debe mantenerse fijo por corrida (startFromId).
- cada chunk debe variar `page` como `chunkId + 1`.
- NO usar `cursorStart = startFromId + i * batchSize` para dividir trabajo.

---

## 7. Mapeo y sanitizacion en Elasticsearch (Integration 1)

Mapping minimo explicito del indice de facturas:

```json
{
  "mappings": {
    "dynamic": true,
    "properties": {
      "idClient":  { "type": "long" },
      "idInvoice": { "type": "long" },
      "idCustomer": { "type": "long" },
      "idCompany": { "type": "long" },
      "idDeliveryType": { "type": "keyword" }
    }
  }
}
```

Reglas de sanitizacion recomendadas:

1. Campos id numericos (`idClient`, `idInvoice`, `idCustomer`, `idCompany`) se parsean a long.
2. Si `idInvoice` es invalido -> documento se descarta.
3. Si otro id numerico es invalido -> se omite el campo, no el documento.
4. Campos SQL `TIME` (`TimeSpan`) se convierten a string `HH:mm:ss`.
5. No convertir nulls de SQL a string vacio (`row[col] = val`, no `val ?? string.Empty`).
6. `idDeliveryType` puede venir alfanumerico (`R`, `E`, etc.) segun el SP; debe tratarse como texto (`keyword`), no como `long`.

---

## 8. ForceReindex en Integration 1

Cuando `forceReindex = true`, el Planner debe:

1. borrar indice existente (si existe),
2. recrearlo con mapping explicito,
3. iniciar cursor desde `0`.

Sin `forceReindex`, solo se crea indice si no existe y se usa max `idInvoice` en ES.

---

## 9. Frontend (sin SignalR)

Integration 1 frontend debe seguir este patron:

- Formulario con `idClient` obligatorio.
- Boton `Iniciar Integracion`.
- Boton `Ver avance` que dispara `GET status`.
- Estado local con:
  - `isRunning`
  - `isLoadingStatus`
  - `activeStatus`
  - `activeJobId`

No debe haber:

- `@microsoft/signalr` en `integration-one-progress.service.ts`
- `HubConnectionBuilder`
- dependencia al hub `/hub/integration-progress`

---

## 10. Flujo de estados esperado

```
"planning"
    |
    v
"running"
    |
    |- (todo ok)               -> "completed"
    |- (errores en chunks)     -> "completed_with_errors"
    |- (error en planner)      -> "failed"
    \- (cancelacion manual)    -> "cancelled"
```

`ProgressPercentage` se calcula como:

```csharp
TotalChunks == 0 ? 0 : round((CompletedChunks + FailedChunks) / TotalChunks * 100, 1)
```

---

## 11. Script de indice de facturas

Script de referencia:

- `scripts/create-clickeat-invoices-index.sh`

Debe cubrir:

1. ping de Elasticsearch,
2. comprobacion de existencia del indice,
3. delete del indice si existe,
4. create con mapping explicito,
5. verificacion final de campos clave (`idClient`, `idInvoice`, etc.).

---

## 12. Antipatrones a evitar

- NO volver a introducir SignalR en Integration 1 para progreso.
- NO omitir `idClient` en start o en documentos indexados.
- NO dejar `forceReindex` como no-op.
- NO confiar en contadores del JSON si existe hash `counters`.
- NO mapear `idInvoice` como texto; debe ser numerico (`long`).
- NO convertir null de SQL a `""` porque rompe mapeo dinamico.
- NO hardcodear `"page": 1` en todos los chunks.
- NO calcular cursor por aritmetica (`startFromId + i*batchSize`) cuando el SP ya pagina por `page`.

---

## 13. Checklist rapido al tocar Integration 1

- [ ] `idClient` obligatorio validado en controller para `idIntegration=1`
- [ ] `IntegrationOneStartRequest` incluye `IdClient`
- [ ] `IntegrationOneState` incluye `IdClient`
- [ ] Chunk estampa `idClient` en cada doc
- [ ] Start maneja `RedisTimeoutException` y `RedisConnectionException` con respuesta 503
- [ ] Planner valida conectividad SQL con retry corto (2s/5s) antes de contar/enqueuar chunks
- [ ] Chunk contabiliza fallo solo en ultimo intento y deduplica finalizacion por `chunkId`
- [ ] `forceReindex` usa `RecreateIndexAsync()`
- [ ] Frontend sin SignalR y con boton `Ver avance`
- [ ] Build .NET OK
- [ ] Build Angular OK

---

## 14. Runbook: Planner falla con SQL (provider TCP error 35)

### Sintoma

En Hangfire, el job `IntegrationOnePlannerJob.PlanAsync` pasa a `Failed` casi de inmediato con:

- `System.InvalidOperationException`
- mensaje: "No se pudo conectar a la base de datos del cliente..."
- detalle SQL: `provider: Proveedor de TCP, error: 35`

### Hallazgo importante

El proceso SI inicia (se encola y entra a `Processing`), pero falla en la verificacion de conexion SQL
del Planner. No confundir con "no arranca".

Puede ocurrir aun cuando una prueba manual (`sqlcmd SELECT 1`) funcione minutos despues.
Eso indica intermitencia de red/VPN/ruta/DNS o disponibilidad momentanea del SQL externo.

### Diagnostico minimo recomendado

1. Confirmar que el planner se encolo y fallo en Hangfire (`Job` + `State`).
2. Leer `State.Data` del `Failed` para ver `ExceptionMessage` real.
3. Confirmar `Param1` (base64) y decodificar server/database/user.
4. Ejecutar prueba manual de conectividad con la misma cadena (`sqlcmd -C ... SELECT 1`).
5. Relanzar `start` y observar si el nuevo planner pasa a `Succeeded`.

### SQL util para soporte rapido

```sql
SELECT TOP 10 s.Name, s.CreatedAt, CAST(s.Data AS NVARCHAR(MAX)) AS DataJson
FROM HangFire.State s
WHERE s.JobId = @PlannerJobId
ORDER BY s.Id DESC;
```

### Mitigaciones ya aplicadas en Integration 1

- Retry corto de conectividad SQL en Planner (`2s`, `5s`) antes de fallar.
- Manejo de errores Redis en `start` con respuesta controlada `503`.
- Deduplicacion de contadores por `chunkId` para evitar inflar progreso por reintentos.

---

## 15. Runbook: Delta residual despues de corrida "completa"

### Sintoma

Los jobs salen en `Succeeded`, pero `BD totalInvoices` sigue mayor que `ES totalDocs`.

### Causa historica ya corregida

Habia una combinacion incorrecta en la particion de chunks:

- Planner usaba `cursorStart = startFromId + i*batchSize`.
- Repository enviaba siempre `"page": 1` al SP.

En rangos de IDs sparse eso provocaba solapamientos y huecos.

### Contrato correcto (post-fix)

- Planner: `cursorStart = startFromId` para todos los chunks.
- ChunkJob: `page = chunkId + 1`.
- Repository: recibe `page` y lo pasa al SP.

### Diagnostico rapido recomendado

1. correr `scripts/validate-invoices-totals-bd-vs-es.sh` con `ID_INVOICE=0`.
2. partir por rango historico vs alto para ubicar el faltante.
3. validar cola final con checkpoint alto (por ejemplo `ID_INVOICE=8509270`).

### Interpretacion practica

- Si el rango historico ya coincide y el faltante esta en rango alto: repetir corrida incremental focalizada con `forceFromId` en zona alta (sin `forceReindex`).
- Si el faltante esta distribuido en todo el rango: verificar que el despliegue tenga el fix de paginacion por chunk antes de volver a ejecutar.
