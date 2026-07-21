# TASK-EBC-MCP-03 — Clasificar archivo local mediante Bancos.Mcp reutilizando el flujo de Bancos.Api

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-20 22:24 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** alto
**Aprobación:** aprobada

---

## Título

Clasificar archivo local mediante Bancos.Mcp reutilizando el flujo de Bancos.Api

## Contexto

Bancos.Mcp expone actualmente solo health_status y no accede a archivos ni datos. Bancos.Api ya detecta formatos de importación, extrae movimientos y los clasifica mediante coincidencias aprobadas, reglas, sugerencia de IA y General pendiente de revisión. Se requiere una herramienta MCP local que reciba una referencia de archivo y entregue una clasificación reproducible sin ampliar el acceso a datos ni revelar contenido financiero en logs.

## Objetivo

Incorporar una herramienta MCP local para clasificar un archivo de importación usando el mismo flujo funcional de detección, extracción y clasificación de Bancos.Api, con controles explícitos para archivos sensibles y una respuesta mínima por movimiento.

## Alcance permitido

* Extender src/Bancos.Mcp con una herramienta MCP específica de clasificación de archivos locales.
* Extraer o compartir contratos y servicios puros de detección, parsing y clasificación desde Bancos.Api solo si preserva sus reglas actuales.
* Aceptar únicamente una referencia de archivo dentro de un directorio de entrada configurado y confinado.
* Validar extensión, tamaño máximo, formato soportado, ruta real y contenido antes de procesar.
* Devolver un resultado estructurado con estado, categoría y fuente de clasificación, sin persistir el archivo ni crear transacciones.
* Agregar pruebas unitarias y de protocolo para éxito, formato no soportado, ruta fuera del directorio permitido, archivo demasiado grande y error de parsing.
* Documentar configuración local, límites, diagnóstico y eliminación del archivo de entrada por responsabilidad del usuario.

## Fuera de alcance

* Aceptar bytes, contenido codificado, rutas arbitrarias, URLs remotas o archivos adjuntos sin confinamiento.
* Escribir movimientos, categorías, reglas o cualquier dato en SQL Server.
* Exponer identificadores bancarios, importes, descripciones completas, archivo original, secretos o trazas sensibles en la respuesta MCP o logs.
* Modificar la clasificación de importaciones ya persistidas por Bancos.Api.
* Publicar el servidor, configurar OAuth, Azure, Copilot Studio o acceso multiusuario.

## Criterios de aceptación

* [ ] La herramienta MCP recibe solo una ruta relativa dentro del directorio de entrada configurado y rechaza traversal, enlaces simbólicos hacia fuera y rutas absolutas.
* [ ] La herramienta usa la misma prioridad de clasificación funcional de Bancos.Api: coincidencia aprobada, regla, IA cuando esté habilitada y General pendiente de revisión.
* [ ] La herramienta detecta y procesa exclusivamente los formatos de importación soportados; los demás devuelven un error de dominio sin información sensible.
* [ ] La respuesta MCP devuelve resultados mínimos por elemento con categoría, fuente y estado, sin contenido financiero innecesario ni persistencia.
* [ ] No se realizan escrituras en SQL Server ni se cargan secretos para el flujo local de clasificación.
* [ ] Las pruebas cubren controles de archivo, paridad de clasificación y el contrato tools/call.
* [ ] La documentación permite configurar el directorio local, ejecutar la herramienta y diagnosticar fallos sin incluir datos de ejemplo sensibles.

## Riesgos

Riesgo alto: requiere aprobación explícita antes de implementar.

## Archivos afectados / probables

* `src/Bancos.Mcp/Tools/`
* `src/Bancos.Mcp/Protocol/`
* `src/Bancos.Mcp/appsettings.json`
* `src/Bancos.Api/Features/Classification/`
* `src/Bancos.Api/Features/Imports/`
* `tests/Bancos.Mcp.Tests/`
* `src/Bancos.Mcp/README.md`

## Plan técnico

1. Definir un contrato de entrada basado en path relativo y un resultado mínimo, con límites de tamaño y formatos permitidos configurables.
2. Crear un servicio de confinamiento de archivos que resuelva la ruta real bajo un único directorio de entrada y rechace cualquier salida de ese límite.
3. Extraer o adaptar componentes reutilizables de detección, parsing y clasificación sin introducir una dependencia de Bancos.Mcp hacia la base de datos o a configuración secreta.
4. Registrar la herramienta MCP y hacer que devuelva errores de dominio seguros y contenido textual/estructurado según el contrato existente.
5. Escribir pruebas de seguridad de rutas, límites de archivo, clasificación y protocolo; documentar el arranque y diagnóstico locales.

## Pasos

1. Inventariar los parsers y dependencias del flujo de importación para identificar el subconjunto reutilizable sin persistencia.
2. Diseñar el contrato de la herramienta y las opciones de configuración no secretas para el directorio y límites de archivo.
3. Implementar confinamiento, validaciones y lectura temporal del archivo.
4. Integrar detección, extracción y clasificación con paridad verificable respecto a Bancos.Api.
5. Registrar la herramienta MCP y añadir pruebas de protocolo.
6. Documentar configuración, uso, diagnósticos, retención cero y rollback.

## Salida esperada

Bancos.Mcp ofrece una herramienta local de clasificación de archivos que procesa solo archivos permitidos dentro de un directorio confinado, aplica las reglas de clasificación de Bancos.Api sin persistir datos y devuelve una respuesta mínima y segura.

## Validación

* [ ] dotnet build src/Bancos.Mcp
* [ ] dotnet test tests/Bancos.Mcp.Tests
* [ ] Pruebas de ruta relativa, traversal, enlace simbólico y límite de tamaño.
* [ ] Pruebas de paridad para coincidencia aprobada, regla, IA deshabilitada y General pendiente de revisión.
* [ ] tools/list descubre la nueva herramienta y tools/call valida éxito y errores seguros.
* [ ] Verificación de que no se crean conexiones SQL ni se escriben datos o archivos.

## Rollback

Eliminar la herramienta MCP, sus opciones y documentación asociada. No debe haber migraciones, persistencia ni cambios en datos que revertir.

## Dependencias

* TASK-EBC-MCP-02

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-20 22:25 CR.

Riesgo alto: los archivos de entrada pueden contener datos financieros. La implementación requiere aprobación explícita antes de pasar a Lista y debe mantener retención cero, respuestas minimizadas y controles de ruta.

## Issues vinculados

* ninguno
