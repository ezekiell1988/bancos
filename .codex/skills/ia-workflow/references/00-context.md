---
title: IA Workflow 00 Context Example
description: Ejemplo y checklist para 00_context.md en un workflow /ia genérico.
---

## Propósito

`00_context.md` es la tarjeta de identidad estable del proyecto. Los agentes la leen primero para entender qué es el sistema, qué no debe cambiarse accidentalmente y dónde vive el código activo.

Usar este archivo para hechos duraderos, no para notas de sesión.

## Cuándo leer

* Siempre, antes de planificar, implementar, revisar o depurar.
* Cuando un agente nuevo necesita orientarse en el proyecto.
* Cuando cambia el stack, el layout del repositorio o las constantes críticas.

## Pertenece a

* Nombre, propósito y audiencia del proyecto
* Alcance activo y límites del repositorio
* Stack tecnológico por capa
* Constantes críticas e invariantes
* Mapa de carpetas clave
* Comandos de validación local a nivel general

## No pertenece a

* Requisitos de funcionalidades. Usar `01_requirements.md`.
* Flujo de implementación. Usar `02_architecture.md`.
* Trabajo actual. Usar `04_tasks/current.md`.
* Historial de progreso. Usar `05_progress/`.

## Esquema recomendado

```markdown
# 00 — Contexto del Proyecto

> Última actualización: {YYYY-MM-DD}
> Alcance activo: {ruta del repo o módulo}

## Identidad del proyecto

| Campo | Valor |
|-------|-------|
| Nombre | {nombre del proyecto} |
| Propósito | {una o dos oraciones} |
| Usuarios principales | {quién lo usa} |
| Responsable | {equipo o persona} |

## Stack tecnológico

| Capa | Tecnología | Notas |
|------|------------|-------|
| Frontend | {framework} | {versión o convenciones} |
| Backend | {runtime} | {versión o modelo de hosting} |
| Base de datos | {motor} | {esquemas principales o modelo de almacenamiento} |
| Infraestructura | {cloud o host} | {modelo de despliegue} |
| Pruebas | {frameworks de prueba} | {comandos} |

## Constantes críticas

* {constante o invariante de negocio que no debe cambiar sin una decisión}
* {autenticación, tenant, región, URL pública, convención de nombres, etc.}

## Mapa del repositorio

| Ruta | Propósito |
|------|----------|
| `{ruta}` | {qué vive aquí} |

## Comandos de validación

| Alcance | Comando |
|---------|--------|
| Build | `{comando}` |
| Tests | `{comando}` |
| Lint | `{comando}` |

## Notas para agentes

* {regla corta que ayuda a los agentes a evitar errores}
```

## Checklist

* El propósito es comprensible sin leer el código fuente.
* El alcance activo es explícito cuando el repositorio contiene múltiples proyectos.
* Las versiones del stack son específicas para elegir los patrones correctos.
* Las constantes críticas están listadas como barreras, no enterradas en prosa.
* No hay secretos ni cadenas de conexión.

## Errores comunes

* Convertir este archivo en un reporte de estado diario.
* Duplicar detalles de requisitos o arquitectura que ya están en archivos posteriores.
* Listar todas las carpetas del repositorio en vez de solo los anclajes de navegación.
* Almacenar credenciales, tokens o nombres de host privados que deben mantenerse fuera del contexto del modelo.
