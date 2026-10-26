# 01 — Requisitos del Sistema

> Última actualización: 2026-10-25
> Fuentes: pedido del dueño por Telegram (2026-10-25)

## Propósito del sistema

Llevar un registro sencillo de gastos e ingresos a partir de los documentos bancarios y facturas que el dueño le pasa al agente, ordenado por cuenta, mes y día, con el archivo de respaldo guardado.

## Roles de usuario

| Rol | Objetivo | Permisos o restricciones |
|-----|----------|-------------------------|
| Dueño | Pasar documentos y facturas, consultar cuánto entró y salió | Dueño de los datos; sus datos son privados |
| Pareja | Pasar documentos y consultar gastos desde el grupo compartido de Telegram | Solo dentro del grupo del proyecto; permisos por definir en la Fase 4 |
| Agente | Leer el documento y anotar los movimientos | Escribe solo en `datos/` y con el MCP `bancos` |

## Invariantes del negocio

* Cada movimiento es una línea jsonl en `datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl` según su fecha.
* Cada movimiento es `gasto` o `ingreso` y su `tipo` existe en el catálogo de su clase.
* Los datos financieros nunca salen de `datos/` ni entran a git.

## Requisitos funcionales

| ID | Requisito | Señal de aceptación | Estado |
|----|-----------|---------------------|--------|
| REQ-001 | Anotar los movimientos de un documento en un jsonl por día, dentro de una carpeta por mes | Tras pasar un documento existe la línea en `datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl`; repetir el documento no duplica | Pendiente |
| REQ-002 | Guardar el archivo de respaldo (imagen, PDF, Excel) en `datos/multimedia/AAAA-MM/` y referenciarlo en el movimiento | El campo `archivo` apunta a un archivo que existe | Pendiente |
| REQ-003 | Clasificar cada movimiento como gasto o ingreso con un tipo de `tipos-gasto.jsonl` o `tipos-ingreso.jsonl`, y poder agregar tipos nuevos | Un tipo desconocido se rechaza; uno nuevo queda en el catálogo | Pendiente |
| REQ-004 | Separar por cuenta: gastos del BCR en `bcr/`, de la tarjeta BAC en `bac/`; ingresos en la cuenta donde caen | Cada movimiento queda en la carpeta de su cuenta | Pendiente |
| REQ-005 | Consultar movimientos y el resumen de un mes (totales por clase, tipo y moneda) | El agente responde cuánto se gastó o ingresó en un mes sin leer los jsonl a mano | Pendiente |
| REQ-006 | Corregir un movimiento mal anotado | Se puede borrar una línea puntual y volver a anotarla | Pendiente |
| REQ-007 | Anotar y consultar movimientos desde un grupo de Telegram con el bot @MacEzequielBot, primero solo con el dueño y después con su pareja | Un documento mandado al grupo queda anotado con el MCP `bancos` y el bot responde en el grupo | Pendiente |

## Flujos principales

### Flujo: anotar un documento

Estado inicial: el dueño manda una foto, PDF o Excel por Telegram
Estado final: movimientos anotados y respaldo guardado

1. El agente lee el documento y deduce cuenta, fecha, clase, tipo, total, moneda y comercio.
2. Si algo no está claro, pregunta una sola cosa con opciones.
3. Anota con el MCP `bancos` (preview y `apply: true`), copiando el respaldo a `multimedia/`.
4. Le responde al dueño qué anotó (cantidad y totales).

## Fuera de alcance

* Base de datos, interfaz web o app.
* Conciliación automática con el banco y carga de estados de cuenta por API (lo viejo queda en la rama `legacy`).
