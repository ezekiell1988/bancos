---
title: IA Workflow 02 Architecture Example
description: Ejemplo y checklist para 02_architecture.md en un workflow /ia genérico.
---

## Propósito

`02_architecture.md` explica cómo está construido el sistema. Mapea componentes, flujo de datos, puntos de integración y contratos técnicos para que los agentes puedan hacer cambios en el límite correcto.

## Cuándo leer

* Al agregar servicios, APIs, jobs, flujos de datos o integraciones.
* Al cambiar infraestructura, autenticación, acceso a datos o límites de módulos.
* Al revisar si una implementación viola decisiones de arquitectura.

## Pertenece a

* Responsabilidades de componentes
* Topología en tiempo de ejecución
* Diagramas de flujo de datos
* Contratos de interfaz y límites de propiedad
* Modelo de despliegue y entorno
* Preocupaciones transversales: autenticación, logging y configuración

## No pertenece a

* Reglas del negocio. Usar `01_requirements.md`.
* Tareas activas. Usar `04_tasks/`.
* Decisiones arquitectónicas históricas. Usar `06_decisions/`.

## Esquema recomendado

```markdown
# 02 — Arquitectura del Sistema

> Última actualización: {YYYY-MM-DD}
> Alcance: {proyecto, servicio o workspace}

## Resumen

{resumen corto de la arquitectura}

## Mapa de componentes

| Componente | Responsabilidad | Ruta fuente | Responsable |
|------------|-----------------|-------------|-------------|
| {componente} | {qué posee} | `{ruta}` | {equipo/persona} |

## Flujo en tiempo de ejecución

```text
[Usuario]
  -> [Frontend]
  -> [API]
  -> [Servicio de dominio]
  -> [Base de datos / Servicio externo]
```

## Contratos de datos

| Contrato | Productor | Consumidor | Ubicación |
|----------|-----------|------------|-----------|
| {DTO/evento/tabla/vista} | {componente} | {componente} | `{ruta}` |

## Configuración

| Parámetro | Propósito | Fuente de entorno |
|-----------|-----------|------------------|
| `{PARAMETRO}` | {por qué existe} | {env, key vault, archivo de config} |

## Validación

| Verificación | Comando o método |
|--------------|-----------------|
| Build | `{comando}` |
| Smoke test | `{comando o URL}` |
```

## Checklist

* Cada componente principal tiene una responsabilidad clara.
* El flujo de datos muestra dónde ocurren los cambios de estado.
* Las integraciones externas listan autenticación, propiedad de endpoints y comportamiento ante fallo.
* Los nombres de configuración se incluyen sin valores de secretos.
* El documento apunta a ADRs para decisiones que cambiaron con el tiempo.

## Escalado a subdirectorio (`02_architecture/`)

Cuando `02_architecture.md` supera **~20 000 caracteres** (~5 000 tokens), mantenerlo monolítico genera truncado silencioso en los intents compactos de `ia_get_context`. El patrón de escalado es idéntico al que usa `06_decisions.md` → `06_decisions/`.

### Cuándo aplicar

* El archivo supera ~20 000 chars medidos con `(Get-Content 02_architecture.md -Raw).Length`.
* Se detectan dominios claramente separados (ej. reportes, facturación, sponsorship, chat, mcp).
* El truncado causa que contratos clave queden fuera del contexto de los agentes.

### Estructura resultante

```text
ia/
├── 02_architecture.md         ← índice compacto (< 24 000 chars)
│   - pipeline principal (obligatorio, siempre en el índice)
│   - tabla de componentes esenciales (~15–20 filas)
│   - patrón stub + config runtime + contratos críticos de código
│   - build/deploy + observabilidad
│   - enlace a cada archivo de dominio
└── 02_architecture/
    ├── componentes.md          ← tabla completa + arquitectura de caché
    ├── reportes.md             ← flujo de reportes + SignalR
    ├── sponsorship.md          ← feature Sponsorship
    ├── chat-qubi.md            ← flujo chat/Qubi
    └── mcp-powerbi.md          ← servidor MCP / Copilot Studio
```

### Reglas del índice compacto

* Pipeline principal completo — nunca mover a subdirectorio (es el contrato global).
* Tabla de componentes: solo las filas más consultadas (~15–20); link a tabla completa en `02_architecture/componentes.md`.
* Contratos de código (`Program.cs`, `HangfireExtensions`, `environment.ts`, etc.) — mantener en el índice, son los más usados en implementación.
* Cada sección movida queda reducida a una nota de 1–2 líneas + enlace al archivo de dominio.
* El índice **no debe superar 24 000 chars** para que quepa en el contexto compacto de `ia_get_context`.

### Reglas de los archivos de dominio

* Cada archivo `02_architecture/{dominio}.md` comienza con un header estándar:
  ```markdown
  # 02 — {Título del dominio}

  > **Detalle completo** extraído de [`../02_architecture.md`](../02_architecture.md).
  > Índice de dominio: ver tabla al inicio de `02_architecture.md`.
  ```
* Todo el contenido original del dominio se mueve sin reescribir ni corregir.
* Los archivos de dominio no tienen límite de tamaño (el LLM los carga bajo demanda).

## Errores comunes

* Escribir un recorrido completo del código en vez de límites y contratos.
* Almacenar secretos en ejemplos de configuración.
* Tratar detalles de implementación temporales como arquitectura.
* Dejar que los diagramas se desvíen del camino de tiempo de ejecución actual.
* Dejar `02_architecture.md` monolítico cuando supera ~20 000 chars sin separar dominios.
