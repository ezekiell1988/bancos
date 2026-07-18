# TASK-EBC-FE-01 — Dashboard Angular inicial para importaciones y revisión

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `main`
**Fecha inicio:** 2026-07-18 13:42 CR
**Fecha cierre:** —
**Área:** FE
**Prioridad:** media
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Dashboard Angular inicial para importaciones y revisión

## Contexto

El backend de importaciones tendrá catálogo de auxiliares, estados y clasificación determinística. Se requiere una interfaz local para cargar archivos y revisar resultados.

## Objetivo

Crear el primer dashboard Angular standalone para importar archivos y revisar su estado.

## Alcance permitido

* Inicializar o integrar Angular standalone servido por la API.
* Crear vistas de importación, estado y movimientos pendientes de revisión.
* Consumir endpoints locales del backend.
* Agregar accesibilidad básica y pruebas de build.

## Fuera de alcance

* Autenticación publicada.
* Despliegue.
* Edición contable avanzada.

## Criterios de aceptación

* [ ] Se puede seleccionar auxiliar y cargar un archivo.
* [ ] Se muestra el estado de importación y plantilla detectada.
* [ ] Se listan movimientos pendientes de revisión.
* [ ] La aplicación compila.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api`
* `src/Bancos.Web`
* `tests`

## Plan técnico

1. Definir contratos API.
2. Crear shell y rutas standalone.
3. Implementar páginas de importación y revisión.
4. Validar build.

## Pasos

1. Preparar frontend.
2. Implementar carga.
3. Implementar revisión.
4. Validar.

## Salida esperada

Interfaz local funcional para importaciones.

## Validación

* [ ] npm run build
* [ ] Pruebas frontend configuradas
* [ ] Prueba manual local

## Rollback

Retirar las rutas Angular sin afectar API ni datos.

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

Trabajar directamente en main durante construcción.

## Issues vinculados

* ninguno
