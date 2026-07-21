# /ia — Esquemas de reconstrucción

Usar solo para crear o reparar contexto. No incluir datos sensibles.

## Convenciones de SQL Server

Las tablas del catálogo MCP inician con `tb` y usan lower camel case, por ejemplo,
`tbImportTemplates`. Sus columnas también usan lower camel case. Las claves
primarias y foráneas expresan la entidad relacionada, por ejemplo,
`idImportTemplates` e `idBankAccounts`; no se usa una columna SQL genérica `Id`.

Cada tabla y cada columna nueva debe declarar un comentario en español mediante
`HasComment(...)` de EF Core. Los nombres de tablas y columnas se conservan en
inglés. La migración debe incluir esos metadatos para que SQL Server los publique
como descripciones del esquema.

## Componentes `00` a `08`

* `00_context.md`: identidad, stack, límites, mapa y validación.
* `01_requirements.md`: requisitos observables, reglas e ítems fuera de alcance.
* `02_architecture.md`: componentes, flujo, contratos y seguridad.
* `03_plan.md`: fases, hitos, dependencias y tareas vinculadas.
* `04_tasks.md`: índice; cada tarea tiene objetivo, alcance, exclusiones, aceptación, riesgo, aprobación, validación y rollback.
* `05_progress.md`: puntero a estado actual e historial.
* `06_decisions.md`: solo índice; un archivo por ADR.
* `07_issues.md`: índice; detalle separado por issue.
* `08_retrospective.md`: aprendizajes accionables por fase.

## Tarea

```markdown
# TASK-{INICIALES}-{ÁREA}-{NN} — {título}
> Estado: Borrador | Lista | En progreso | Bloqueada | En revisión | Completada
> Riesgo: Bajo | Medio | Alto
> Aprobación: Pendiente | Aprobada explícitamente
## Contexto
## Objetivo
## Incluye
## No incluye
## Criterios de aceptación
## Plan técnico
## Validación
## Rollback
```

Tareas de riesgo alto no avanzan sin aprobación explícita.
