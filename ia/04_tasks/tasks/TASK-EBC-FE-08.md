# TASK-EBC-FE-08 — Página de Movimientos por Categoría con filtro y reclasificación

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-20 19:33 CR
**Fecha cierre:** —
**Área:** FE
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** pendiente

---

## Título

Página de Movimientos por Categoría con filtro y reclasificación

## Contexto

El usuario necesita revisar transacciones clasificadas como "General" y reasignarlas. Existe /review pero no permite filtrar por categoría ni editar clasificación. Datos en Transactions con CategoryId y ClassificationStatus. API ya tiene endpoints de categorías y clasificación.

## Objetivo

Crear ruta /categories con listado de transacciones filtrable por categoría, descripción, monto, fecha y acción inline para cambiar categoría. Mostrar total del filtro activo.

## Alcance permitido

* src/Bancos.Web/src/app/features/categories/
* src/Bancos.Web/src/app/app.routes.ts
* src/Bancos.Api/Features/Transactions/

## Fuera de alcance

* Clasificación automática por IA
* Reglas de clasificación
* Exportación

## Criterios de aceptación

* [ ] Selector de categoría filtra la lista en tiempo real
* [ ] Cada fila muestra fecha, descripción, monto CRC y categoría actual
* [ ] Dropdown inline permite cambiar categoría y persiste en BD
* [ ] Total del filtro activo visible en el encabezado
* [ ] Ruta /categories registrada en app.routes.ts

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Nuevo endpoint GET /api/transactions?categoryId=&page=&pageSize= con CategoryId opcional
2. PATCH /api/transactions/{id}/category con body {categoryId}
3. Feature Angular standalone categories con CategoriesApi y CategoriesPage
4. Registrar ruta en app.routes.ts

## Pasos

1. Crear endpoints BE GET y PATCH en TransactionsModule
2. Crear feature FE categories con data-access y página
3. Registrar ruta en app.routes.ts
4. Verificar en browser con categoría General

## Salida esperada

Página /categories funcional mostrando transacciones filtrables y reclasificables

## Validación

* [ ] GET /api/transactions?categoryId=X devuelve transacciones paginadas
* [ ] PATCH /api/transactions/{id}/category cambia categoría en BD
* [ ] UI filtra y actualiza sin recargar página

## Rollback

Eliminar ruta de app.routes.ts y el feature categories; los endpoints BE no afectan datos existentes

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

Sin notas adicionales.

## Issues vinculados

* ninguno
