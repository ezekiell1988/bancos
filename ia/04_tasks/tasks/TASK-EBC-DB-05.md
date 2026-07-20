# TASK-EBC-DB-05 — Reiniciar base de datos de desarrollo desde migraciones EF

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 20:30 CR
**Fecha cierre:** —
**Área:** DB
**Prioridad:** crítica
**Riesgo:** alto
**Aprobación:** aprobada

---

## Título

Reiniciar base de datos de desarrollo desde migraciones EF

## Contexto

El usuario solicita eliminar todas las tablas existentes, incluidas las de Hangfire, e iniciar desde cero aplicando únicamente las migraciones EF ya versionadas.

## Objetivo

Eliminar completamente la base de datos configurada para desarrollo y reconstruirla desde las migraciones EF, incluyendo las tablas de infraestructura de Hangfire creadas al iniciar la aplicación.

## Alcance permitido

* Detener temporalmente la API local para evitar conexiones activas
* Eliminar la base de datos destino mediante EF Core
* Aplicar todas las migraciones EF existentes
* Iniciar la API local y verificar la creación de infraestructura Hangfire
* Validaciones agregadas sin datos financieros

## Fuera de alcance

* Modificar archivos de migración o modelos
* Alterar credenciales, secretos o configuración de despliegue
* Restaurar datos, importaciones, huellas o movimientos eliminados
* Actuar sobre una base de datos distinta a la configurada por el proyecto local

## Criterios de aceptación

* [ ] La base de datos configurada por el proyecto local se elimina por completo.
* [ ] Las migraciones EF quedan aplicadas sin pendientes.
* [ ] La aplicación local inicia y Hangfire queda operativo desde una base vacía.
* [ ] El listado de importaciones responde desde cero sin registros previos.

### Prueba end-to-end: carga de `src/input.zip`

* [ ] El ZIP se carga mediante el endpoint de importación sin errores.
* [ ] Se generan los `ImportJob` esperados (uno por cada archivo/plantilla detectada dentro del ZIP).
* [ ] Todos los jobs se ejecutan y completan sin error en Hangfire.
* [ ] Los movimientos/transacciones quedan almacenados en las tablas definidas (verificar conteo > 0).
* [ ] No quedan jobs en estado `Failed` en Hangfire.

## Riesgos

Riesgo alto: requiere aprobación explícita antes de implementar.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Usar dotnet ef database drop --force con el proyecto y startup project configurados.
2. Usar dotnet ef database update para reconstruir esquema.
3. Permitir que Hangfire inicialice sus tablas durante el arranque de la API.
4. Validar sólo conteos y códigos HTTP.

## Pasos

1. Verificar la migración y el destino configurado sin exponer secretos.
2. Detener la API local.
3. Ejecutar el borrado EF con confirmación forzada.
4. Aplicar todas las migraciones EF.
5. Reiniciar API y validar API/Hangfire.

## Salida esperada

Entorno de desarrollo reiniciado desde cero con EF y Hangfire limpios.

## Validación

* [ ] dotnet ef migrations list sin migraciones pendientes tras la recreación.
* [ ] GET /api/imports devuelve respuesta correcta y lista vacía.
* [ ] Dashboard Hangfire responde y no contiene trabajos históricos.
* [ ] POST carga de `src/input.zip` retorna 200 y genera jobs.
* [ ] Todos los jobs corren y completan (estado `Succeeded` en Hangfire).
* [ ] Tablas de movimientos/transacciones tienen registros tras la ejecución de los jobs.

## Rollback

No existe rollback de datos: el usuario autorizó su eliminación. Para recuperar información se requeriría un respaldo externo no incluido en esta tarea.

## Dependencias

* ninguna

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 20:30 CR.

Operación destructiva de alto riesgo solicitada explícitamente por el usuario.

## Issues vinculados

* ninguno
