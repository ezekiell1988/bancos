# TASK-EBC-DB-05 — Estrategia de dos entornos de base de datos (dev y prod)

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 20:30 CR
**Fecha actualización:** 2026-07-20 CR
**Fecha cierre:** —
**Área:** DB
**Prioridad:** crítica
**Riesgo:** alto
**Aprobación:** aprobada

---

## Título

Estrategia de dos entornos de base de datos: dev (local) y prod (Azure SQL)

## Contexto

El proyecto Bancos requiere mantener dos entornos de BD con estrategias radicalmente distintas:

- **Dev (local)**: SQL Server en Docker. Se puede borrar y recrear libremente para experimentar con el esquema sin acumular migraciones innecesarias. No hay datos persistentes valiosos.
- **Prod**: Azure SQL (aún no provisionado). Contiene datos históricos reales de la familia Baltodano-Soto. Solo acepta migraciones EF incrementales, nunca drops.

Los secretos de conexión se mantienen en:
- `.local-secrets/db.json` → dev local
- `.local-secrets/dbProd.json` → prod

La tarea original se limitaba al reset de dev. Esta versión amplía el alcance para formalizar la estrategia dual y crear el skill `bancos-db-env` con herramientas para:
1. Reset completo de dev
2. Aplicar migraciones EF a prod sin pérdida de datos
3. Sincronizar registros de prod → dev para pruebas
4. Comparar estado entre ambos entornos (bidireccional)

## Objetivo

Definir, documentar e implementar la estrategia dual de entornos de BD, entregando:
- Un skill `bancos-db-env` (para uso por LLMs) con ejemplos PS1/Python ejecutables
- Reset de dev desde migraciones EF
- Migración segura de prod
- Flujo prod→dev para bajar datos reales a local
- Diagnóstico bidireccional dev↔prod

## Alcance permitido

### Dev (local)
* Detener temporalmente la API local para evitar conexiones activas
* Eliminar la base de datos de dev mediante EF Core (`database drop --force`)
* Aplicar todas las migraciones EF existentes
* Iniciar la API local y verificar que Hangfire queda operativo
* Cargar `src/input.zip` como prueba end-to-end tras el reset

### Prod
* Aplicar migraciones EF pendientes usando connection string de `dbProd.json`
* Listar migraciones pendientes en modo preview (`--WhatIf`)
* **Solo** `dotnet ef database update --connection "..."` apuntando a prod

### Skill y ejemplos
* Crear `.agents/skills/bancos-db-env/` con `SKILL.md`, `examples/` y `references/`
* Ejemplos: `01_dev_reset.ps1`, `02_prod_migrate.ps1`, `03_prod_to_dev.py`, `04_diff_envs.py`
* El skill usa `create-skill` como base y está destinado a LLMs (no uso directo del usuario)

## Fuera de alcance

* Provisionar el servidor Azure SQL (tarea de infraestructura separada)
* Modificar archivos de migración o modelos del dominio
* Autenticación, seguridad o CI/CD de prod
* Copiar datos financieros de prod en logs, prompts o archivos de IA
* Ejecutar `database drop` en ningún contexto que no sea dev local

## Criterios de aceptación

### Dev reset
* [ ] La BD dev se elimina y recrea desde migraciones EF sin errores.
* [ ] `dotnet ef migrations list` no muestra migraciones pendientes tras la recreación.
* [ ] La API local inicia y Hangfire queda operativo desde cero.
* [ ] `GET /api/imports` devuelve lista vacía (200).

### Prueba end-to-end: carga de `src/input.zip` en dev
* [ ] El ZIP se carga sin errores y genera los `ImportJob` esperados.
* [ ] Todos los jobs completan en estado `Succeeded` en Hangfire.
* [ ] Tablas de transacciones tienen registros (conteo > 0).
* [ ] Sin jobs en estado `Failed`.

### Skill bancos-db-env
* [ ] El skill existe en `.agents/skills/bancos-db-env/SKILL.md` con frontmatter válido.
* [ ] `01_dev_reset.ps1` detecta si el servidor es prod y aborta si lo es.
* [ ] `02_prod_migrate.ps1 -WhatIf` lista pendientes sin aplicar cambios.
* [ ] `02_prod_migrate.ps1 -Confirm` aplica migraciones a prod sin borrar datos.
* [ ] `03_prod_to_dev.py` hace upsert (MERGE) de tablas de referencia sin duplicar.
* [ ] `04_diff_envs.py` reporta conteos y diferencias por tabla sin modificar datos.
* [ ] Ningún ejemplo expone valores de contraseñas en su salida estándar.

## Riesgos

| Riesgo | Mitigación |
|---|---|
| Ejecutar drop en prod por error | `01_dev_reset.ps1` verifica que el servidor no sea Azure; `02_prod_migrate.ps1` no tiene opción drop |
| Exposición de credenciales en logs | Los scripts leen secretos pero nunca imprimen `Password` |
| Sync prod→dev con datos financieros sensibles | `03_prod_to_dev.py` no copia datos por defecto; requiere `--transactions` explícito |
| Migraciones prod fallidas sin rollback automático | EF no tiene rollback DDL en MSSQL; se requiere respaldo previo a prod (tarea INF pendiente) |

## Archivos afectados / probables

* `.agents/skills/bancos-db-env/SKILL.md` ← nuevo
* `.agents/skills/bancos-db-env/examples/01_dev_reset.ps1` ← nuevo
* `.agents/skills/bancos-db-env/examples/02_prod_migrate.ps1` ← nuevo
* `.agents/skills/bancos-db-env/examples/03_prod_to_dev.py` ← nuevo
* `.agents/skills/bancos-db-env/examples/04_diff_envs.py` ← nuevo
* `.agents/skills/bancos-db-env/references/env-strategy.md` ← nuevo
* `src/Bancos.Api/Infrastructure/LocalDatabaseConfiguration.cs` ← referencia, no modificar
* `src/Bancos.Api/Migrations/` ← referencia, no crear migraciones en esta tarea

## Plan técnico

1. Crear skill `bancos-db-env` con `create-skill` como guía base.
2. Escribir `SKILL.md` con descripción, triggers y referencias a ejemplos.
3. Escribir `01_dev_reset.ps1`: stop API → `database drop --force` → `database update` → verificar.
4. Escribir `02_prod_migrate.ps1`: leer `dbProd.json` → `migrations list` → `database update --connection`.
5. Escribir `03_prod_to_dev.py`: conectar prod (read) + dev (write) → MERGE por PK en tablas de referencia.
6. Escribir `04_diff_envs.py`: conectar ambos en modo lectura → comparar COUNT por tabla + PKs faltantes.
7. Escribir `references/env-strategy.md` con reglas, flujo típico y decisiones ADR.
8. Actualizar esta tarea (TASK-EBC-DB-05) con el alcance dual.
9. Ejecutar reset de dev con `01_dev_reset.ps1` y validar criterios de aceptación.

## Pasos

1. ✅ Crear estructura de skill `bancos-db-env`.
2. ✅ Escribir SKILL.md con instrucciones para LLMs.
3. ✅ Escribir `01_dev_reset.ps1` (dev reset).
4. ✅ Escribir `02_prod_migrate.ps1` (prod migrate safe).
5. ✅ Escribir `03_prod_to_dev.py` (sync prod→dev).
6. ✅ Escribir `04_diff_envs.py` (diff bidireccional).
7. ✅ Escribir `references/env-strategy.md`.
8. ✅ Actualizar TASK-EBC-DB-05.
9. [ ] Ejecutar `01_dev_reset.ps1` y validar criterios de aceptación de dev.
10. [ ] Registrar progreso en `/ia`.

## Salida esperada

- Skill `bancos-db-env` publicado en `.agents/skills/` con 4 ejemplos ejecutables.
- Entorno dev reseteado y validado con carga de `src/input.zip`.
- Estrategia de entornos documentada en el skill y referenciable por el LLM.

## Validación

* [ ] `dotnet ef migrations list` sin pendientes tras reset dev.
* [ ] `GET /api/imports` → 200 lista vacía.
* [ ] Dashboard Hangfire responde sin trabajos históricos.
* [ ] POST carga `src/input.zip` → 200 y genera jobs.
* [ ] Jobs completan en `Succeeded`, transacciones con conteo > 0.
* [ ] `pwsh 02_prod_migrate.ps1 -WhatIf` lista pendientes sin error de conexión a prod.
* [ ] `python 04_diff_envs.py` reporta diferencias sin modificar datos.

## Rollback

- **Dev**: no hay rollback de datos; el usuario autorizó la eliminación. Respaldo externo si se necesita.
- **Prod**: no ejecutar `database drop` nunca. Si una migración falla en prod, revertir manualmente el DDL o restaurar desde backup de Azure SQL (tarea INF pendiente de crear).

## Dependencias

* Provisioning Azure SQL → tarea de infraestructura pendiente (aún no definida)

## Checklist

* [x] Alcance revisado
* [x] Riesgo revisado
* [x] Aprobación registrada
* [ ] Implementación completa (skill: ✅ | reset dev: pendiente ejecución)
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: Se eliminó la base de datos local de desarrollo mediante EF Core, removiendo todas las tablas de dbo y Hangfire. Se omitió su recreación por solicitud del usuario.

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 20:30 CR.
* Ampliada el 2026-07-20 para cubrir estrategia dual de entornos.
* El skill `bancos-db-env` está diseñado para ser invocado por LLMs (Claude, Copilot), no directamente por el usuario en la terminal — aunque los scripts son ejecutables de forma independiente.
* `dbProd.json` existe en `.local-secrets/`. El contenido no debe aparecer en ninguna salida de IA.
* Cuando se provisione Azure SQL, actualizar `references/env-strategy.md` con el servidor real.

## Issues vinculados

* ninguno
