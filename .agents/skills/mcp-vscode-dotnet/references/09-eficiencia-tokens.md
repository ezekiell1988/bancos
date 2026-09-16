# 09 — Eficiencia de tokens en catálogos MCP

## Qué consume tokens

El transporte no es el factor principal. `stdio` y Streamable HTTP pueden exponer el mismo
catálogo y producir prácticamente el mismo costo para el modelo. El costo relevante viene
de:

1. Definiciones de tools disponibles: nombre, descripción, `inputSchema`, `outputSchema` y hints.
2. Resultados de cada llamada que se incorporan al contexto de la conversación.
3. Llamadas innecesarias causadas por tools ambiguas, duplicadas o demasiado genéricas.

Clientes con tool search o virtual tools pueden diferir schemas pesados hasta que sean
necesarios. Esto reduce overhead, pero no justifica catálogos desordenados: los nombres y
descripciones siguen participando en el routing y los resultados continúan consumiendo contexto.

## Un MCP o varios MCP

Regla base: **un MCP por dominio coherente, con muchas tools de workflow relacionadas**.

| Diseño | Efecto esperado |
|---|---|
| 1 MCP con 30 tools habilitadas | El agente considera las 30 definiciones |
| 3 MCP con 10 tools cada uno, todos habilitados | Costo similar; añade lifecycle y configuración |
| 3 MCP con 10 tools, solo uno habilitado | Menor conjunto activo y menor espacio de decisión |
| 1 mega-tool con muchas acciones | Menos definiciones, pero schema, routing, validación y permisos peores |

Separar catálogos únicamente cuando al menos una condición sea cierta:

- Los dominios rara vez se usan juntos y el cliente puede deshabilitar los no necesarios.
- Tienen permisos, secretos o identidades diferentes.
- Pertenecen a equipos o propietarios diferentes.
- Cambian y se despliegan con ciclos independientes.
- Necesitan límites operativos o aislamiento distintos.

No separar por tool, feature folder, transporte ni endpoint. `CompanyMcp.Stdio` y
`CompanyMcp.Http` son dos hosts del mismo MCP y no reducen tokens por sí solos.

## Diseñar menos tools, pero mejores

Preferir tools orientadas a una intención completa:

```text
Bien: work_items_search, work_items_get, work_items_create
Evitar: work_items_get_title, work_items_get_state, work_items_get_owner
Evitar: company_execute(action, payload) con acciones sin relación
```

- Combinar lecturas que siempre se necesitan juntas.
- Separar operaciones con efectos, permisos o confirmaciones diferentes.
- Eliminar tools duplicadas aunque llamen downstreams distintos.
- Mantener nombres `snake_case`, específicos del dominio y diferenciables.
- Hacer que cada descripción explique cuándo usar y cuándo no usar la tool.

Una heurística inicial es mantener un catálogo cotidiano pequeño y revisar su partición al
acercarse a varias decenas de tools. No usar un número fijo como objetivo: medir selección,
schemas y resultados reales. Si el cliente reporta un límite de tools habilitadas, reducir
el conjunto activo o usar su mecanismo vigente de tool search/virtual tools.

## Reducir definiciones y schemas

- Descripción breve y específica; no repetir nombre, título ni lista de parámetros.
- Parámetros planos y con nombres claros.
- No exponer valores que se obtienen de identidad, configuración o contexto del servidor.
- Evitar DTOs profundamente anidados y enums gigantes.
- No agregar opciones preventivas sin un caso de uso real.
- Usar nullability y defaults para expresar opcionalidad sin texto redundante.
- Agregar `outputSchema` solo cuando el structured output aporte valor al consumidor.
- Comparar el `tools/list` observado después de cambios del SDK.

No sacrificar seguridad o claridad por reducir unos pocos tokens. `apply`, `confirm`, scopes
y parámetros que evitan ambigüedad siguen siendo obligatorios.

## Reducir resultados de tools

Los resultados suelen crecer más que las definiciones. Aplicar:

- Retornar únicamente campos que cambien la siguiente decisión del agente.
- Añadir `limit` con un default conservador y un máximo explícito.
- Paginar listados grandes y devolver cursor/continuación compacto.
- Ofrecer `summary`, `includeDetails`, `includeText` o `maxChars` cuando aporte control real.
- Truncar texto largo indicando que fue truncado y cómo pedir la siguiente parte.
- No devolver payloads HTTP, HTML, headers, trazas ni metadata completa del downstream.
- Para mutaciones, devolver preview/resultado, identificador y estado; no reimprimir el input completo.
- Usar structured content pequeño. TOON/CSV solo cuando una medición demuestre ahorro y el
  consumidor pueda interpretarlo de forma confiable.

## Controlar el conjunto activo en VS Code

- Deshabilitar tools individuales o servidores completos que no aplican al workflow.
- Crear tool sets para lectura, escritura, despliegue u otros escenarios recurrentes.
- En custom agents/prompts, declarar únicamente las tools necesarias.
- Para catálogos grandes, usar tool search/virtual tools si la versión y el harness lo soportan.
- No asumir que instalar/configurar un MCP implica que todas sus tools deban estar habilitadas.

Dividir un MCP solo produce ahorro si la división permite que parte del catálogo quede fuera
del conjunto activo. Si todos los MCP permanecen habilitados, la división no es una técnica
de ahorro.

## Checklist de medición

1. Capturar `tools/list` y revisar cantidad, descripciones y schemas.
2. Identificar tools nunca usadas, duplicadas o seleccionadas incorrectamente.
3. Medir tamaño y utilidad de resultados representativos, especialmente listados.
4. Verificar que defaults de `limit` y detalle sean conservadores.
5. Comparar antes/después con prompts y fixtures iguales.
6. Revisar la vista de uso/token del cliente cuando esté disponible.
7. Confirmar que la optimización no redujo seguridad, exactitud ni capacidad de autocorrección.

Fuentes primarias:

- <https://modelcontextprotocol.io/specification/2025-11-25/server/tools>
- <https://code.visualstudio.com/docs/copilot/concepts/tools>
- <https://code.visualstudio.com/docs/agents/run/tools>
- <https://code.visualstudio.com/docs/agent-customization/tool-sets>
- <https://code.visualstudio.com/docs/agents/guides/optimize-usage>
