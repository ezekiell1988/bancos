# TASK-EBC-BE-07 — Pre-revisión automática de archivos importados

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 15:29 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Pre-revisión automática de archivos importados

## Contexto

La revisión QA confirmó que exigir un auxiliar antes de cargar contradice el flujo deseado: la persona usuaria sube archivos heterogéneos y el sistema debe identificar su tipo por contenido. Los ejemplos de src/input existen para respaldar esa detección. Los nombres de auxiliares actuales incluyen CRC y pueden confundirse con la moneda de cada archivo.

## Objetivo

Permitir cargar varios archivos sin seleccionar auxiliar previamente, detectar y mostrar el tipo de cada archivo en una revisión inicial, solicitar intervención solo para los no identificados y usar nombres de auxiliares neutrales respecto de moneda.

## Alcance permitido

* Incorporar una etapa de análisis previo de archivos basada en firmas/contenido existentes.
* Actualizar API y UI de Importaciones para subir lotes sin auxiliar obligatorio y mostrar una cola de revisión por archivo.
* Persistir o enviar a procesamiento solo archivos identificados o confirmados explícitamente por el usuario.
* Renombrar auxiliares semilla para eliminar la moneda del nombre sin alterar las reglas de moneda de movimientos.

## Fuera de alcance

* Modificar archivos de ejemplo en src/input.
* Exponer información financiera de archivos en logs, respuestas o documentación.
* Cambiar reglas contables, conversión de moneda o publicar el sistema.

## Criterios de aceptación

* [ ] La pantalla permite seleccionar o arrastrar varios archivos sin elegir un auxiliar.
* [ ] Cada archivo muestra su tipo detectado a partir del contenido y su estado de confianza antes de confirmar la importación.
* [ ] Los archivos no identificados requieren una selección o confirmación explícita; los identificados no requieren auxiliar manual.
* [ ] Los auxiliares semilla tienen nombres neutrales a la moneda.
* [ ] La importación conserva idempotencia y las validaciones existentes; API y frontend compilan y las pruebas relevantes pasan.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Imports`
* `src/Bancos.Api/Data/BancosDbContext.cs`
* `src/Bancos.Api/Migrations`
* `src/Bancos.Web/src/app/features/imports`
* `src/Bancos.Api.Tests`

## Plan técnico

1. Reutilizar el detector de plantillas para analizar el contenido antes del procesamiento definitivo.
2. Definir la respuesta de pre-revisión con tipo detectado, asociación propuesta, confianza y motivo de intervención cuando aplique.
3. Adaptar el endpoint y el job para recibir la asociación confirmada o resuelta automáticamente, sin bytes en argumentos de Hangfire.
4. Actualizar la UI a una cola por archivo con detección y controles solo para excepciones.
5. Aplicar migración/actualización segura para nombres neutrales de auxiliares y validar regresión.

## Pasos

1. Inspeccionar contratos, detector de plantillas y modelo de importación existentes.
2. Implementar análisis previo y resolución automática con manejo de casos inciertos.
3. Actualizar la pantalla de Importaciones.
4. Actualizar datos semilla o migración de nombres y pruebas.
5. Compilar, ejecutar pruebas y validar el flujo en navegador.

## Salida esperada

Flujo de importación con pre-revisión automática por archivo, resolución de excepciones y auxiliares con nombres neutrales.

## Validación

* [ ] dotnet build y dotnet test en la solución/API.
* [ ] npm run build en src/Bancos.Web.
* [ ] Prueba manual con múltiples archivos de prueba no sensibles: identificado, no identificado y confirmación.
* [ ] Verificación de que no exista desbordamiento horizontal en la pantalla de Importaciones.

## Rollback

Revertir los cambios de API/UI y la migración asociada; los archivos temporales siguen siendo descartables y no se modifican ejemplos fuente.

## Dependencias

* TASK-EBC-BE-02
* TASK-EBC-BE-03
* TASK-EBC-FE-02

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: Se implementó la pre-revisión automática de importaciones. La UI ya no exige seleccionar auxiliar: analiza cada archivo antes de confirmar, muestra el tipo detectado y solo presenta selector de tipo cuando no puede identificarlo. La API expone /api/imports/preview, reutiliza las firmas de contenido existentes, asocia automáticamente el auxiliar compatible por clase contable cuando existe una coincidencia única y bloquea tipos reconocidos sin extractor habilitado. El upload conserva la asociación resuelta y el job procesa el tipo confirmado sin pasar bytes a Hangfire. Se añadieron nombres neutrales para auxiliares semilla: Cuenta bancaria y Créditos y financiamientos; la migración RenameSeedAuxiliaries fue aplicada localmente.

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 15:29 CR.

Decisión UX de la revisión QA: el sistema detecta por contenido y solicita intervención solo ante incertidumbre.

## Issues vinculados

* ninguno
