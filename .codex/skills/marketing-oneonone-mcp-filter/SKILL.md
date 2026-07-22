---
name: marketing-oneonone-mcp-filter
description: >
  Arquitectura completa del sistema MCP (Model Context Protocol) de Marketing1on1 y su
  integración con SignalR para modificar filtros de tabla desde el chat con IA.
  Usar siempre que se agregue un tool MCP nuevo, un evento SignalR nuevo, o se extienda
  el chat de IA para afectar la UI de la tabla de clientes.
applyTo:
  - "src/MarketingOneOnOneApi/Services/Mcp/**"
  - "src/MarketingOneOnOneApi/Hubs/**"
  - "src/MarketingOneOnOneApi/Services/Chat/**"
  - "src/MarketingOneOnOneWeb/src/app/service/elasticsearch-websocket.service.ts"
  - "src/MarketingOneOnOneWeb/src/app/pages/home/home.ts"
  - "src/MarketingOneOnOneWeb/src/app/pages/home/components/client-filter/**"
---

# MCP + SignalR — Arquitectura y guía de extensión

## 1. Visión general del flujo

```
Usuario escribe en chat
        │
        ▼
ElasticsearchChatHub (SignalR)
        │  SendMessage(sessionId, message)
        ▼
ChatService.ProcessMessageAsync()
        │  OpenAI API — function calling loop (máx 5 iteraciones)
        ├──► onToolCall  ──► SignalR "ToolCall" (notificar UI)
        ├──► ExecuteToolAsync(toolName, args)
        │         │
        │         └──► IElasticsearchTool.ExecuteAsync()
        │                    │
        │                    └──► IElasticsearchService (Elasticsearch)
        │
        ├──► onToolResult ──► SignalR "ClientsData" (solo tools con structuredData)
        │
        └──► SignalR "Response" (texto final del asistente)
```

---

## 2. Capas del backend

### 2.1 Hub SignalR
**Archivo:** `src/MarketingOneOnOneApi/Hubs/ElasticsearchChatHub.cs`  
**Ruta:** `wss://.../hub/elasticsearch/chat`

Métodos que puede llamar el cliente:
| Método Hub | Parámetros | Descripción |
|---|---|---|
| `SendMessage` | `sessionId: string, message: string` | Procesa un mensaje con IA |
| `ClearHistory` | `sessionId: string` | Limpia historial de sesión |
| `GetHistory` | `sessionId: string` | Recupera historial |

Eventos que emite el Hub al cliente (SignalR):
| Evento | Parámetros | Cuándo se emite |
|---|---|---|
| `Connected` | `message: string, toolsCount: number` | Al conectarse |
| `Processing` | `message: string` | Al empezar a procesar |
| `ToolCall` | `toolName: string, args: any` | Cuando OpenAI elige un tool |
| `ClientsData` | `data: ClientsResponse` | Cuando `list_clients` o `search_clients` retornan datos |
| `Response` | `content: string` | Respuesta final del asistente |
| `Error` | `errorMessage: string` | Error en procesamiento |
| `HistoryCleared` | `message: string` | Historial limpiado |
| `History` | `messages: ChatMessage[]` | Historial recuperado |

**`toolsCount` es dinámico** — inyectado via `IEnumerable<IElasticsearchTool>`.

### 2.2 ChatService
**Archivo:** `src/MarketingOneOnOneApi/Services/Chat/ChatService.cs`

- Convierte todos los `IElasticsearchTool` a formato OpenAI `ChatTool` en constructor
- Loop de hasta **5 iteraciones** de function calling
- Detecta el patrón wrapper `{ toon, structuredData }`:
  - `toon` → se envía a OpenAI (ahorra tokens)
  - `structuredData` → se pasa a `onToolResult` → Hub → cliente via `ClientsData`

```csharp
// Patrón que chatService detecta automáticamente:
return new {
    toon = Helpers.ToonConverter.ToToon(summaryForAI),  // Para OpenAI
    structuredData = result                              // Para el frontend
};
```

### 2.3 Auto-descubrimiento de tools
**Archivo:** `src/MarketingOneOnOneApi/Extensions/McpExtensions.cs`

```csharp
// Se registran automáticamente todos los tipos que implementen IElasticsearchTool
var toolTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract &&
                t.GetInterfaces().Any(i => i.Name == "IElasticsearchTool"))
    .ToList();
// → AddScoped(IElasticsearchTool, TipoConcreto)
```

**Para agregar un tool nuevo: solo crear la clase.** No hay registro manual.

---

## 3. Anatomía de un Tool MCP

Todo tool hereda de `BaseTool` e implementa `IElasticsearchTool`.

```csharp
// Archivo: src/MarketingOneOnOneApi/Services/Mcp/Tools/MiNuevoTool.cs
public class MiNuevoTool : BaseTool
{
    private readonly IElasticsearchService _elasticsearchService;

    // Nombre único — OpenAI lo usa para invocar el tool
    public override string Name => "elasticsearch_mi_nuevo_tool";

    public MiNuevoTool(IElasticsearchService elasticsearchService,
                       ILogger<MiNuevoTool> logger) : base(logger)
    {
        _elasticsearchService = elasticsearchService;
    }

    public override ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Name = Name,
            Title = "Título legible",
            Description = "Descripción detallada para OpenAI. Cuanto más precisa, " +
                "mejor será la selección automática del tool.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    mi_param = new
                    {
                        type = "string",
                        description = "Descripción del parámetro"
                    }
                },
                required = new[] { "mi_param" }   // o new string[] { } si no hay requeridos
            }
        };
    }

    public override async Task<object> ExecuteAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        var param = GetRequiredParameter<string>(arguments, "mi_param");
        // var optional = GetOptionalParameter<int>(arguments, "limit", 20);

        var result = await _elasticsearchService.MiMetodoAsync(param);

        // Opción A: solo retornar TOON para OpenAI (sin actualizar tabla)
        return Helpers.ToonConverter.ToToon(result);

        // Opción B: actualizar tabla + dar resumen a OpenAI (ver sección 4)
    }
}
```

### Helpers de parámetros en BaseTool
```csharp
GetRequiredParameter<T>(arguments, "nombre")
// Lanza ArgumentException si falta

GetOptionalParameter<T>(arguments, "nombre", defaultValue)
// Retorna defaultValue si falta; tipos soportados: string, int, bool, List<string>, etc.
```

---

## 4. Pattern: Tool que actualiza la tabla (structuredData)

Usar cuando el resultado debe **reflejarse en la tabla de clientes** de la UI.  
El `ChatService` detecta el objeto anónimo `{ toon, structuredData }` y:
1. Envía `toon` a OpenAI
2. Envía `structuredData` al Hub → `ClientsData` → frontend

```csharp
public override async Task<object> ExecuteAsync(...)
{
    var result = await _elasticsearchService.GetClientsAsync(...); // ClientsResponse

    // Resumen para OpenAI — ahorrar tokens, no enviar los clientes
    var summaryForAI = new
    {
        Success = result.Success,
        Total = result.Total,
        TotalInvoices = result.TotalInvoices,
        Pagination = result.Pagination,
        AppliedFilters = result.AppliedFilters,
        Message = $"Se encontraron {result.Total} clientes. Datos enviados a la tabla."
    };

    return new
    {
        toon = Helpers.ToonConverter.ToToon(summaryForAI),
        structuredData = result   // ClientsResponse completo con .clients[]
    };
}
```

**Hub detecta el tool y emite `ClientsData`:**
```csharp
// ElasticsearchChatHub.cs — OnToolResult callback
var clientTools = new HashSet<string> {
    "elasticsearch_list_clients",
    "elasticsearch_search_clients"
};
if (clientTools.Contains(toolName) && result != null)
    await Clients.Caller.SendAsync("ClientsData", result);
```

> ⚠️ Si creas un tool nuevo que deba actualizar la tabla, agrega su nombre al `HashSet` en el Hub.

---

## 5. TOON (Token-Oriented Object Notation)
**Archivo:** `src/MarketingOneOnOneApi/Helpers/ToonConverter.cs`

Formato propio que reduce ~40% de tokens vs JSON al eliminar comillas y estructuras verbosas.  
Llamar siempre con `Helpers.ToonConverter.ToToon(objeto)`.  
Solo retornar `toon` a OpenAI — **nunca enviar arrays grandes de clientes al LLM**.

---

## 6. Frontend: ElasticsearchWebSocketService

**Archivo:** `src/MarketingOneOnOneWeb/src/app/service/elasticsearch-websocket.service.ts`

### Event handlers registrados
```typescript
// Conectar
hubConnection.on('Connected', (message: string, toolsCount: number) => { ... })

// Progreso
hubConnection.on('Processing', (message: string) => { loadingSubject.next(true) })
hubConnection.on('ToolCall', (toolName: string, args: any) => { addToolMessage(...) })

// Datos de tabla (el más importante para filtros)
hubConnection.on('ClientsData', (clientsData: any) => {
  customEventsSubject.next({ type: 'clients_data', data: clientsData, timestamp: new Date() })
})

// Respuesta final
hubConnection.on('Response', (content: string) => { loadingSubject.next(false); addAssistantMessage(content) })
hubConnection.on('Error', (errorMessage: string) => { ... })
hubConnection.on('HistoryCleared', (message: string) => { ... })
hubConnection.on('History', (messages: any[]) => { ... })
```

### Para agregar un evento nuevo del Hub
```typescript
// En registerEventHandlers():
this.hubConnection.on('MiEvento', (param1: string, param2: any) => {
  this.customEventsSubject.next({
    type: 'mi_evento',
    data: { param1, param2 },
    timestamp: new Date()
  });
});
```

---

## 7. Frontend: home.ts — consumo de customEvents$

**Archivo:** `src/MarketingOneOnOneWeb/src/app/pages/home/home.ts`

```typescript
// ngOnInit — suscripción central a todos los eventos del WebSocket
this.elasticsearchWsService.customEvents$.subscribe(event => {
  if (event?.type === 'clients_table_update') {
    this.handleClientsTableUpdate(event);
  }
  else if (event?.type === 'clients_data') {
    // Datos estructurados del chat MCP → actualizar tabla de clientes
    this.handleClientsTableUpdate(event);
  }
  // Agregar aquí nuevos tipos de eventos
});
```

### handleClientsTableUpdate
Recibe `{ type, data: ClientsResponse, timestamp }`.  
- Lee `data.appliedFilters` → llama `updateFiltersFromApplied()` → sincroniza toda la UI de filtros
- Lee `data.clients`, `data.pagination` → actualiza la tabla vía `handleClientResponse()`

### updateFiltersFromApplied — campos que sincroniza
```typescript
appliedFilters.registered       → selectedFilter ('all'|'registered'|'guest')
appliedFilters.paid             → selectedPaid
appliedFilters.province/canton/district/neighborhood → selectedXxx + recarga dependientes
appliedFilters.timeOfDay        → selectedTimeOfDay[]
appliedFilters.restaurant       → selectedRestaurant[]
appliedFilters.device           → selectedDevice[]
appliedFilters.products         → selectedProducts[]
appliedFilters.excludedProducts → selectedExcludedProducts[]
appliedFilters.customers        → selectedCustomers[]
appliedFilters.deliveryTypes    → selectedDeliveryTypes[]
appliedFilters.daysSinceMin/Max → daysRange
appliedFilters.ordersMin/Max    → ordersRange
appliedFilters.dateFrom/dateTo  → dateRange + dateRangeFrom + dateRangeTo + selectedDateRange
appliedFilters.pageSize         → clientsPageSize
```

---

## 8. ClientFilterComponent — sincronización ng-select con SignalR

**Archivo:** `src/MarketingOneOnOneWeb/src/app/pages/home/components/client-filter/client-filter.component.ts`

### Cómo recibe filtros del bot
`home.ts` tiene `@Input() selectedProducts: string[]` sobre `<app-client-filter>`.  
Cuando cambian via SignalR → Angular detecta cambio de referencia → `ngOnChanges` en `ClientFilterComponent`.

### ngOnChanges — qué hace con productos
```typescript
if (changes['selectedProducts']) {
  this.localSelectedProducts = [...(this.selectedProducts || [])];
  // Pre-pobla productLookupItems para que ng-select muestre los valores
  // aunque el usuario no haya buscado nada
  const existingNames = new Set(this.productLookupItems.map(p => p.name));
  const missing = (this.selectedProducts || [])
    .filter(name => !existingNames.has(name))
    .map(name => ({ name, count: 0 } as ProductFilterItem));
  if (missing.length) this.productLookupItems = [...this.productLookupItems, ...missing];
}
// Idem para selectedExcludedProducts → excludedProductLookupItems
```

> **Por qué es necesario:** ng-select con `[searchable]="false"` y `bindValue="name"` solo
> puede mostrar un valor seleccionado si existe en `[items]`. Si los items llegan del bot
> sin que el usuario haya buscado, el ngOnChanges los inyecta automáticamente.

Lo mismo aplica a `selectedCustomers` → `localSelectedCustomers` (ClientLookupItem).

### ngOnChanges — datepicker (ngx-daterangepicker-material)

`ngx-daterangepicker-material` v6 usa **moment.js** internamente.  
El `@Input() selectedDateRange: { startDate: any; endDate: any } | null` **debe contener objetos `moment.Moment`**, nunca `dayjs` ni `Date` simples.

**En `home.ts`, al construir `selectedDateRange` desde appliedFilters:**
```typescript
import moment from 'moment';  // ← NO usar dayjs aquí

// En updateFiltersFromApplied():
this.selectedDateRange = {
  startDate: moment(appliedFilters.dateFrom),  // e.g. moment("2025-01-01")
  endDate:   moment(appliedFilters.dateTo),
};
```

**En `client-filter.component.ts`, el `ngOnChanges` para `selectedDateRange` fuerza un re-render del picker con doble ciclo:**
```typescript
private _suppressPickerEvent = false;
private readonly cdr = inject(ChangeDetectorRef);

if (changes['selectedDateRange']) {
  const newValue = this.selectedDateRange;
  // Paso 1: poner null + detectChanges para que el picker limpie su estado interno
  this._suppressPickerEvent = true;
  this.localSelectedDateRange = null;
  this.cdr.detectChanges();
  // Paso 2: 50ms después, asignar el valor moment y forzar CD para que el input muestre el texto
  setTimeout(() => {
    this.localSelectedDateRange = newValue;
    this._suppressPickerEvent = false;
    this.cdr.detectChanges();
  }, 50);
}
```

> **Por qué el doble ciclo:** el picker de `ngx-daterangepicker-material` no reacciona a cambios
> de `[(ngModel)]` si el input anterior y el nuevo son ambos no-nulos y distintos — solo actualiza
> el calendario interno pero no re-pinta el `<input>`. El truco null → valor + `detectChanges` lo
> obliga a llamar `writeValue(null)` y luego `writeValue(moment(...))`, forzando el re-render del
> texto en el input.

**El flag `_suppressPickerEvent`** bloquea `onDateRangePickerChangeLocal` durante estas escrituras
programáticas para evitar que el picker emita `datesUpdated` con fechas incorrectas.  
También se activa en `clearDesktopDateRange()` con un timeout de 50ms.

> ⚠️ **Regla de oro:** cualquier valor que se pase a `selectedDateRange` desde `home.ts` debe
> ser `null` o `{ startDate: moment(...), endDate: moment(...) }`. Usar `dayjs` o `new Date`
> hace que el picker ignore el valor silenciosamente.

---

## 9. Catálogo de tools actuales

| Tool name | Clase | Retorna structuredData | Descripción |
|---|---|---|---|
| `elasticsearch_list_clients` | `ListClientsTool` | ✅ | Lista clientes con todos los filtros |
| `elasticsearch_search_clients` | `SearchClientsTool` | ✅ | Búsqueda fuzzy por nombre/tel/email |
| `elasticsearch_lookup_products` | `ProductsLookupTool` | ❌ | Búsqueda parcial de productos (lookup) |
| `elasticsearch_list_products` | `ProductsTool` | ❌ | Lista estática de productos con conteo |
| `elasticsearch_general_stats` | `GeneralStatsTool` | ❌ | Estadísticas generales del índice |
| `elasticsearch_list_companies` | `CompaniesTool` | ❌ | Lista compañías disponibles |
| `elasticsearch_list_provinces` | `ProvincesTool` | ❌ | Lista provincias |
| `elasticsearch_list_cantons` | `CantonsTool` | ❌ | Lista cantones por provincia |
| `elasticsearch_list_districts` | `DistrictsTool` | ❌ | Lista distritos por provincia+cantón |
| `elasticsearch_list_neighborhoods` | `NeighborhoodsTool` | ❌ | Lista barrios |
| `elasticsearch_list_restaurants` | `RestaurantsTool` | ❌ | Lista restaurantes |
| `elasticsearch_list_devices` | `DevicesTool` | ❌ | Lista dispositivos |
| `elasticsearch_list_invoices` | `InvoicesTool` | ❌ | Facturas de un cliente |
| `elasticsearch_list_addresses` | `AddressesTool` | ❌ | Direcciones de un cliente |
| `elasticsearch_autocomplete_clients` | `AutocompleteClientsTool` | ❌ | Autocompletado de clientes |

---

## 10. Checklist: agregar un tool nuevo que actualiza la tabla

1. **Crear** `src/MarketingOneOnOneApi/Services/Mcp/Tools/MiTool.cs` heredando `BaseTool`
2. **Definir** `Name => "elasticsearch_mi_tool"` (único, snake_case, prefijo `elasticsearch_`)
3. **Implementar** `GetDefinition()` con descripción clara para el LLM
4. **Implementar** `ExecuteAsync()`:
   - Si solo informa al LLM → `return Helpers.ToonConverter.ToToon(result)`
   - Si debe actualizar tabla → `return new { toon = ..., structuredData = result }`
5. **Si actualiza tabla:** agregar el nombre al `HashSet<string> clientTools` en `ElasticsearchChatHub.cs → SendMessage()`
6. **Frontend (opcional):** si el evento es distinto a `ClientsData`, agregar handler en `registerEventHandlers()` del WebSocket service y consumirlo en `ngOnInit` de `home.ts`
7. **Build:** el auto-descubrimiento registra el tool. Verificar con `dotnet build`

---

## 11. Checklist: agregar un evento SignalR nuevo (no clientes)

1. **Backend:** en `ElasticsearchChatHub.cs`, emitir el evento en el callback correspondiente:
   ```csharp
   await Clients.Caller.SendAsync("MiEvento", payload);
   ```
2. **Frontend service:** en `registerEventHandlers()`:
   ```typescript
   this.hubConnection.on('MiEvento', (payload: any) => {
     this.customEventsSubject.next({ type: 'mi_evento', data: payload, timestamp: new Date() });
   });
   ```
3. **Frontend page:** en `ngOnInit` de `home.ts`, dentro de `customEvents$.subscribe`:
   ```typescript
   else if (event?.type === 'mi_evento') {
     this.handleMiEvento(event.data);
   }
   ```
4. Implementar `handleMiEvento()` en `home.ts`
