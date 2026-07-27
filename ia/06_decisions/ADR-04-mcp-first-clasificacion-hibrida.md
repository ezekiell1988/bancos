# ADR-04 — MCP-first y clasificación híbrida aprendible

**Estado:** Aceptada
**Fecha:** 2026-07-26

## Contexto

`Bancos.Mcp` es la única interfaz activa. El LLM debe poder cargar archivos, ejecutar cierres, clasificar movimientos y obtener reportes sin API HTTP ni interfaz web. La clasificación debe reducir llamadas a IA y ser trazable.

## Decisión

El producto se opera exclusivamente mediante tools MCP. Cada movimiento se clasifica en este orden: reglas .NET deterministas, Azure AI solo como fallback, `No clasificado` cuando no exista resultado confiable y confirmación humana. La confirmación guarda auditoría y crea o actualiza una regla determinista reutilizable.

Las categorías contables tienen raíces Ingreso, Gasto, Activo, Pasivo y Capital. Los reportes se devuelven como HTML autocontenido mediante tools MCP.

## Consecuencias

* El LLM coordina operaciones mediante tools con respuestas explicables; no sustituye las reglas contables ni escribe directamente en la base de datos.
* Azure AI no recibe archivos, saldos, cuentas, identificadores ni secretos.
* Las herramientas de clasificación y reportes se implementan como features de `Bancos.Mcp` y se prueban allí.
* La integración Azure AI requiere una tarea de riesgo alto aprobada antes de implementarse.
