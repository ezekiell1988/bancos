# TASK-EBC-BE-08 — Revisión de archivos dentro de ZIP

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 15:41 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Revisión de archivos dentro de ZIP

## Contexto

La persona usuaria quiere subir un ZIP que puede contener carpetas y múltiples archivos. Cada archivo interno debe participar de la misma revisión previa por contenido, sin requerir extraerlo manualmente ni revelar su contenido.

## Objetivo

Permitir seleccionar un ZIP, recorrer de forma segura sus entradas de archivos y mostrar cada una en la cola de revisión previa para detectar tipo, solicitar decisión solo ante incertidumbre y procesar las entradas confirmadas como importaciones independientes.

## Alcance permitido

* Aceptar archivos ZIP en la pantalla y API de Importaciones.
* Analizar recursivamente las entradas de archivos del ZIP sin escribir rutas de la archivación al disco.
* Crear importaciones temporales independientes solo para entradas confirmadas y compatibles.
* Aplicar límites de tamaño, cantidad de entradas y rutas para mitigar ZIP bomb y traversal.

## Fuera de alcance

* Soportar formatos de archivo comprimido distintos de ZIP.
* Modificar archivos en src/input o exponer contenido financiero en logs, respuestas o documentación.
* Extraer archivos a sus rutas originales o cambiar reglas contables.

## Criterios de aceptación

* [ ] La pantalla acepta un ZIP y muestra en la revisión cada archivo contenido, incluyendo su ruta relativa segura.
* [ ] Las carpetas vacías se ignoran y las entradas se detectan por contenido como archivos individuales.
* [ ] Una entrada incierta solicita tipo solo para esa entrada; una entrada no compatible no se procesa.
* [ ] Las entradas confirmadas generan importaciones independientes sin pasar bytes a Hangfire.
* [ ] El ZIP se rechaza de forma segura si excede límites de cantidad, tamaño descomprimido o incluye rutas inseguras.
* [ ] API y frontend compilan; se validan ZIP válido, entrada incierta y archivo/ZIP inválido.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Imports/ImportsModule.cs`
* `src/Bancos.Api/Features/Imports`
* `src/Bancos.Web/src/app/features/imports`

## Plan técnico

1. Incorporar lector ZIP basado en System.IO.Compression que enumere entradas no directorio y valide límites antes de materializar datos.
2. Extender el contrato de pre-revisión para devolver una colección de entradas con ruta relativa y detección individual.
3. Extender la confirmación para recibir decisiones por entrada y crear una importación temporal por archivo confirmado.
4. Actualizar la cola Angular para agrupar visualmente las entradas bajo el ZIP y mantener controles por entrada.
5. Probar entradas normales, inciertas y límites de seguridad sin imprimir contenido.

## Pasos

1. Inspeccionar contratos de pre-revisión y flujo de upload actual.
2. Implementar análisis seguro de ZIP y contratos de lote.
3. Actualizar UI y confirmación de lote.
4. Compilar y ejecutar validaciones técnicas y manuales no sensibles.

## Salida esperada

Un ZIP con carpetas se revisa como un conjunto de archivos individuales y solo las entradas confirmadas se importan.

## Validación

* [ ] dotnet build y dotnet test.
* [ ] npm run build.
* [ ] Prueba API con ZIP válido de archivos de prueba no sensibles, archivo sin firma dentro del ZIP y ZIP inválido/límite.
* [ ] Validación visual en escritorio y móvil de la cola anidada sin overflow horizontal.

## Rollback

Revertir contratos y UI de ZIP; los temporales por entrada se eliminan si ocurre un fallo y no se escriben rutas del ZIP.

## Dependencias

* TASK-EBC-BE-07

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: ZIP seguro implementado: recorre entradas permitidas, ignora directorios y metadatos de macOS, limita tamaño y crea importaciones individuales por entrada.

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 15:41 CR.

Límites iniciales propuestos: máximo 100 entradas, 20 MB por entrada y 50 MB descomprimidos por ZIP; podrán ajustarse con evidencia.

## Issues vinculados

* ninguno
