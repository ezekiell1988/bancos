---
name: m1on1-integration-2
description: >
  Arquitectura completa, bugs corregidos y guía de extensión de la Integración 2 de Marketing1on1:
  proceso Hangfire que sincroniza clientes desde SQL Server externo (del cliente) hacia
  Elasticsearch. Cubre el flujo de 3 jobs (Planner → Chunks → Finalizer), estado en Redis,
  repositorios SQL/ES, sanitización de documentos y lecciones aprendidas.
  Usar SIEMPRE que se trabaje con IntegrationTwo, sus jobs, repositorios o estado Redis.
applyTo:
  - "src/MarketingOneOnOneApi/Jobs/IntegrationTwo/**"
  - "src/MarketingOneOnOneApi/Infrastructure/IntegrationTwo/**"
---

# Integration Two — SQL Server Externo → Elasticsearch

## 1. Qué hace esta integración

Sincroniza los clientes del cliente (tenant) desde su propio SQL Server hacia el índice
Elasticsearch de Marketing1on1. El connection string al SQL externo se guarda en
`ClientIntegration.Param1` codificado en **Base64**.

Para Integration 2, el request de inicio debe traer `idClient` desde frontend.
El API lo valida (`> 0`) y usa ese valor para poblar `IntegrationTwoStartRequest.IdClient`,
`IntegrationTwoState.IdClient` y los documentos indexados en Elasticsearch.

El proceso es **incremental por defecto**: solo indexa clientes con `idCustomer` mayor al
máximo ya indexado en ES. Se puede forzar un reindex completo con `ForceReindex: true`.

---

## 2. Arquitectura de Jobs (Hangfire)

```
Controller POST /api/integration/two/start
      │
      ▼
IIntegrationTwoPlannerJob.PlanAsync()      ← Job 1/3 (queue: "integration", AutomaticRetry=0)
      │
      ├── Determina total de pendientes (SP spElasticSearchCustomerGetTotal)
      ├── Calcula cursores para N chunks
      ├── Inicializa estado en Redis
      └── Encola N × IIntegrationTwoChunkJob  ──┐  paralelo
                                                 │
             ┌───────────────────────────────────┘
             ▼
IIntegrationTwoChunkJob.ProcessChunkAsync()    ← Job 2/3 (AutomaticRetry=3, delays: 30/120/300s)
      │
      ├── Lee lote SQL (SP spElasticSearchCustomerGet)
      ├── Sanitiza documentos (SanitizeDocument)
      ├── Bulk index en Elasticsearch
      └── Incrementa contadores atómicos en Redis
             │
             └── Si fue el ÚLTIMO chunk → encola Finalizer
                        │
                        ▼
IIntegrationTwoFinalizerJob.FinalizeAsync()    ← Job 3/3
      │
      └── Calcula status final: "completed" o "completed_with_errors"
```

---

## 3. Archivos y responsabilidades

| Archivo | Responsabilidad |
|---|---|
| `Jobs/IntegrationTwo/IntegrationTwoPlannerJob.cs` | Job 1: planificación, cálculo de cursores, encolado de chunks |
| `Jobs/IntegrationTwo/IntegrationTwoChunkJob.cs` | Job 2: lectura SQL → sanitización → bulk ES → contadores |
| `Jobs/IntegrationTwo/IntegrationTwoFinalizerJob.cs` | Job 3: cierre del proceso, status final |
| `Jobs/IntegrationTwo/IntegrationTwoRedisHelper.cs` | Todas las operaciones Redis: estado, chunks, contadores |
| `Jobs/IntegrationTwo/IntegrationTwoJobState.cs` | DTOs: `IntegrationTwoState`, `IntegrationTwoChunkState`, request/response |
| `Infrastructure/IntegrationTwo/ElasticIntegrationTwoRepository.cs` | Bulk index ES, mapping de índice, sanitización de docs |
| `Infrastructure/IntegrationTwo/SqlIntegrationTwoRepository.cs` | Lectura de SPs del cliente externo |

---

## 4. Claves Redis

Todas las claves usan `{jobId}` entre llaves para soportar Redis Cluster.

| Clave | Tipo | TTL | Contenido |
|---|---|---|---|
| `integration-two:{jobId}:state` | String (JSON) | 48h | `IntegrationTwoState` serializado |
| `integration-two:{jobId}:counters` | Hash | 48h | `completedChunks`, `failedChunks`, `totalIndexed`, `totalErrors` |
| `integration-two:{jobId}:chunk:{id}` | String (JSON) | 48h | `IntegrationTwoChunkState` por chunk |
| `integration-two:{jobId}:chunk-finalized:{chunkId}` | String | 48h | `"1"` si ese chunk ya fue contado (dedup) |

### Regla crítica de contadores
Los contadores del hash (`counters`) son la **fuente de verdad**. `GetStateAsync` siempre
sobreescribe los campos `CompletedChunks`, `FailedChunks`, `TotalIndexed`, `TotalErrors`
del estado JSON con los valores del hash. Nunca confiar en el JSON para las métricas numéricas.

### Resiliencia ante timeouts de Redis
`IntegrationTwoRedisHelper` aplica reintentos con backoff para operaciones Redis transitorias
(`RedisTimeoutException`, `RedisConnectionException`, `TimeoutException`) antes de fallar.

Delays actuales por operación: `200ms`, `500ms`, `1000ms`.

En `POST /api/v1/client-integration/2/start`, si Redis falla al guardar el estado inicial,
el API responde `503 Service Unavailable` con mensaje accionable en vez de dejar excepción sin controlar.

---

## 5. Stored Procedures del cliente

El SQL externo del cliente debe tener estos dos SPs:

### `spElasticSearchCustomerGetTotal`
- Parámetro: `@json NVarChar` → `{"idCustomer": <lastId>}`
- Retorna: columna `totalPending` (bigint), `totalCustomers` (bigint)

### `spElasticSearchCustomerGet`
- Parámetro: `@json NVarChar` → `{"idCustomer": <cursorStart>, "page": 1, "itemPerPage": <batchSize>}`
- Retorna: filas de clientes donde `idCustomer > cursorStart`, máx `itemPerPage` filas
- La columna `idCustomer` debe existir y ser numérica (uso como cursor)

---

## 6. Campos especiales del documento

| Campo | Tipo ES | Tipo SQL | Notas |
|---|---|---|---|
| `idClient` | `long` | — | **ID de nuestro sistema** (Marketing1on1) — lo envía el frontend en el request y el ChunkJob lo estampa en cada documento antes del bulk index |
| `idCustomer` | `long` | numérico | Clave del documento (`_id` en ES) — obligatorio, documento descartado si es inválido |
| `idCompany` | `long` | numérico | Campo numérico, si es inválido se omite el campo (no el doc) |
| `idLogin` | `keyword` | string | **PUEDE** ser email, número o código — siempre se trata como string |
| `customerAddress` | objeto/array | string JSON | Se parsea con `TryParseJson`; propiedades vacías y arrays vacíos se filtran |
| `deviceInfo` | objeto/array | string JSON | Igual que `customerAddress` — fallback a `[]` si es null |
| columnas `TIME` | string `HH:mm:ss` | `TimeSpan` | Se formatean a string antes de serializar |

### Sanitización (`SanitizeDocument`)
El método `SanitizeDocument` en `ElasticIntegrationTwoRepository` procesa cada campo:

1. **null** → pasa como `null`
2. **`StringFields`** (ej. `idLogin`) → `Convert.ToString().Trim()`, null si vacío
3. **`NumericIdFields`** (ej. `idClient`, `idCustomer`, `idCompany`) → `TryToLong`
   - Si `idCustomer` no es parseable → **doc descartado** (retorna `[]`)
   - Otros campos numéricos inválidos → campo omitido con warning
4. **resto** → pasa sin transformación

---

## 7. Mapping explícito del índice ES

El índice se crea con mapping explícito para evitar que el tipo dinámico quede mal definido:

```json
{
  "mappings": {
    "dynamic": true,
    "properties": {
      "idClient":   { "type": "long"    },
      "idCustomer": { "type": "long"    },
      "idCompany":  { "type": "long"    },
      "idLogin":    { "type": "keyword" }
    }
  }
}
```

### `ForceReindex: true` → `RecreateIndexAsync()`
Cuando se pide reindex total, el Planner llama `RecreateIndexAsync()` que:
1. **Borra** el índice existente (si existe)
2. **Crea** el índice nuevo con el mapping explícito de arriba

Esto es esencial: sin borrar el índice, un mapping previo corrupto (ej. `idLogin` como
`integer`) sigue rechazando documentos aunque el código ya envíe strings.

### Guard de compatibilidad de mapping (sin `ForceReindex`)
Cuando el proceso arranca en modo incremental (`forceReindex = false`), el Planner valida
el tipo actual de `idLogin` en el índice con `IsIdLoginMappingCompatibleAsync()`.

- Si `idLogin` es `keyword`, `text` o `wildcard` → sigue el proceso normal.
- Si `idLogin` es numérico u otro tipo incompatible → el Planner marca el proceso como
  `failed` con mensaje accionable, y **no** encola chunks.

Objetivo: evitar miles de errores de bulk (`number_format_exception`) cuando el índice ya
venía contaminado por un mapping anterior.

### Contrato API para `idClient`
- Endpoint: `POST /api/v1/client-integration/2/start`
- Body: incluye `idClient` (int obligatorio, `> 0`)
- Si falta o es `<= 0`, el API responde `400 BadRequest`
- El API no toma `idClient` desde configuración de dominio para este flujo; usa el valor del frontend.

---

## 8. Bugs corregidos y lecciones aprendidas

### Bug 1 — Contadores reseteados (porcentaje 100→0→100)
**Causa**: `SaveStateAsync` inicializaba el hash de contadores Redis cada vez que era
llamado con `TotalChunks > 0` y contadores a 0. Si un chunk terminaba entre el primer y
segundo llamado del Planner, sus incrementos se borraban.

**Fix** ([RedisHelper línea ~70](../Jobs/IntegrationTwo/IntegrationTwoRedisHelper.cs)):
```csharp
// Solo inicializar si la clave NO existe aún
var exists = await db.KeyExistsAsync(cKey);
if (!exists)
{
    await db.HashSetAsync(cKey, [ ... ]);
}
```

---

### Bug 2 — Doble conteo en reintentos de Hangfire
**Causa**: cuando un chunk fallaba, se incrementaba `failedChunks`. Si Hangfire
reintentaba y el segundo intento tenía éxito, también se incrementaba `completedChunks`.
El total de chunks contados superaba `TotalChunks`, disparando el Finalizer prematuramente.

**Fix** ([RedisHelper — deduplicación por chunkId](../Jobs/IntegrationTwo/IntegrationTwoRedisHelper.cs)):
```csharp
var finalizedKey = ChunkFinalizedKey(jobId, chunkId);
var firstFinalization = await db.StringSetAsync(finalizedKey, "1", ChunkTtl, When.NotExists);
if (!firstFinalization)
    return; // Este chunk ya fue contado antes
```

---

### Bug 3 — Finalizer sobreescribía estado `cancelled`
**Causa**: si el usuario cancelaba el proceso, un chunk tardío podía completarse después
y encolar el Finalizer, que sobreescribía el estado a `completed`.

**Fix** ([FinalizerJob](../Jobs/IntegrationTwo/IntegrationTwoFinalizerJob.cs)):
```csharp
if (state.Status is "completed" or "completed_with_errors" or "cancelled")
    return; // Ya finalizado, no hacer nada
```

---

### Bug 4 — Finalizer prematuro cuando un chunk fallaba con reintentos pendientes
**Causa**: cuando un chunk fallaba, el catch del ChunkJob inmediatamente incrementaba
`failedChunks` y encolaba el Finalizer — incluso aunque Hangfire todavía fuera a
reintentar ese chunk 2 o 3 veces más.

**Fix** ([ChunkJob — PerformContext](../Jobs/IntegrationTwo/IntegrationTwoChunkJob.cs)):
Se inyecta `PerformContext` (Hangfire lo provee en runtime, no se serializa).
**Solo en el último intento** se incrementa `failedChunks` y se encola el Finalizer:

```csharp
var retryCount    = performContext?.GetJobParameter<int>("RetryCount") ?? 0;
var isLastAttempt = retryCount >= MaxRetryAttempts;

if (isLastAttempt)
{
    await _redisHelper.IncrementChunkCompletedAsync(..., success: false, ...);
    // encolar Finalizer si es el último chunk
}

throw; // siempre re-lanzar para que Hangfire reintente
```

---

### Bug 5 — ES rechazaba `idLogin` con `number_format_exception`
**Causa**: si el índice fue creado cuando los primeros documentos tenían `idLogin`
numérico, ES mapeó ese campo como `integer`. Documentos posteriores con `idLogin` como
email fallaban con `document_parsing_exception / number_format_exception`.

**Fix**:
1. `idLogin` se clasifica en `StringFields` (no en `NumericIdFields`) en el repositorio ES
2. `SqlIntegrationTwoRepository` normaliza `idLogin` explícitamente como string
3. El índice se crea con `"idLogin": { "type": "keyword" }` en el mapping explícito
4. `ForceReindex` borra y recrea el índice para limpiar el mapping corrupto

---

### Bug 6 — Nulls del SQL se convertían a `string.Empty`
**Causa**: `row[col] = val ?? string.Empty` hacía que campos nulos de SQL llegaran a ES
como cadena vacía en lugar de `null`. Esto puede interferir con el mapping dinámico.

**Fix**: `row[col] = val` — los nulls llegan como null y se manejan en `SanitizeDocument`.

---

### Bug 7 — Proceso incremental seguía intentando bulk con mapping incompatible
**Causa**: aun con el código correcto, si el índice histórico tenía `idLogin` como tipo
numérico, el proceso incremental continuaba y terminaba en rechazo masivo de documentos.

**Fix**:
1. `ElasticIntegrationTwoRepository` expone `IsIdLoginMappingCompatibleAsync()`
2. `IntegrationTwoPlannerJob` falla temprano si el mapping no es compatible y
  `forceReindex = false`
3. El mensaje de error instruye explícitamente reintentar con `forceReindex = true`

Resultado: diagnóstico inmediato, menos ruido operativo y resolución guiada.

---

## 9. Flujo de estados del proceso

```
"planning"
    │
    ▼ (PlannerJob exitoso)
"running"
    │
    ├── (todos los chunks ok)    → "completed"
    ├── (algún chunk falló)      → "completed_with_errors"
    ├── (error en PlannerJob)    → "failed"
    └── (cancelación manual)    → "cancelled"
```

### `ProgressPercentage`
Calculado en el modelo como propiedad derivada:
```csharp
public double ProgressPercentage => TotalChunks == 0
    ? 0
    : Math.Round((double)(CompletedChunks + FailedChunks) / TotalChunks * 100, 1);
```
Sube de forma **monótona** gracias a la deduplicación SET NX por chunkId.

---

## 10. Request de inicio

```json
POST /api/integration/two/start
{
  "idClient":     123,        // int: ID del cliente en nuestro sistema (Marketing1on1) — obligatorio
  "batchSize":    1000,       // Registros por chunk (default: 1000)
  "forceReindex": false,      // true: borra índice y reindexar desde 0
  "forceFromId":  null        // long?: forzar cursor inicial (override de lógica incremental)
}
```

Prioridad del cursor de inicio:
1. `ForceFromId` si tiene valor
2. `0` si `ForceReindex = true`
3. `GetMaxCustomerIdAsync()` (máximo actual en ES) en caso normal

---

## 11. Response de estado

```json
GET /api/integration/two/status/{idClientCustomer}?includeChunks=false
{
  "integrationJobId": "...",
  "status":           "running",
  "progressPercentage": 45.2,
  "totalChunks":      10,
  "completedChunks":  4,
  "failedChunks":     0,
  "totalIndexed":     4821,
  "totalErrors":      3,
  "totalPending":     10000,
  "lastIndexedIdBefore": 0,
  "batchSize":        1000,
  "startedAt":        "...",
  "endedAt":          null,
  "duration":         null,
  "errorMessage":     null,
  "chunks":           []   // poblado si includeChunks=true
}
```

---

## 12. Consideraciones para extender

### Agregar un campo nuevo del SP
1. Si es numérico → agregar a `NumericIdFields` en `ElasticIntegrationTwoRepository`
2. Si es string con posibles valores mixtos → agregar a `StringFields`
3. Si es JSON embed → manejar en `SqlIntegrationTwoRepository.GetCustomersAsync` igual que `customerAddress`/`deviceInfo`
4. Si es `TIME` de SQL → ya manejado automáticamente (formatea a `"HH:mm:ss"`)

### Cambiar el mapping del índice
Editar `IndexCreateJson` en `ElasticIntegrationTwoRepository`. Luego usar
`ForceReindex: true` en el endpoint para aplicar el nuevo mapping.

### Añadir más chunks paralelos
Cambiar `batchSize` en el request. El Planner calcula automáticamente
`totalChunks = ceil(totalPending / batchSize)`.

### Depurar chunks individuales
Usar `GET /api/integration/two/status/{id}?includeChunks=true` para ver estado
de cada chunk: su `status`, `successCount`, `errorCount`, `errorMessage` y `durationSeconds`.

---

## 13. Advertencias y anti-patrones

- **NO** llamar `SaveStateAsync` después de que los chunks ya empezaron — puede reinicializar
  contadores si la clave de contadores expiró. Usar siempre `IncrementChunkCompletedAsync`.
- **NO** quitar el `throw` en el catch del ChunkJob — Hangfire necesita la excepción para
  registrar el reintento y calcular `RetryCount`.
- **NO** usar `ForceReindex` en producción sin asegurarse de que `totalPending` sea el total
  correcto — el SP `GetCustomerCountsAsync` con `lastIndexedId=0` debe retornar todos los clientes.
- **NO** modificar `IntegrationTwoState.CompletedChunks/FailedChunks` directamente en código:
  son sobrescritos en `GetStateAsync` desde el hash de Redis. Los cambios manuales se pierden.
