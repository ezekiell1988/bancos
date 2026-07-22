# ADR-03 — Surrogate PK en tablas de unión (junction tables)

**Estado:** Aprobado  
**Fecha:** 2026-07-21  
**Área:** DB  

---

## Contexto

Al diseñar `tbCardStatementLines` (tabla de unión entre `tbCardStatements` y `tbTransactions`) surgió la pregunta: ¿usar PK compuesta `(idCardStatements, idTransactions)` o una surrogate PK propia?

## Decisión

Usar **surrogate PK** (`uniqueidentifier`) en todas las tablas de unión del MCP, más un **UNIQUE constraint** sobre las columnas que forman la relación lógica.

```sql
-- Ejemplo: tbCardStatementLines
idCardStatementLines  uniqueidentifier  PK
idCardStatements      uniqueidentifier  FK (no duplicado por UNIQUE)
idTransactions        uniqueidentifier  FK

UNIQUE (idCardStatements, idTransactions)
```

## Razones

1. **Extensibilidad:** si en el futuro se agregan columnas a la línea (ej. `sortOrder`, `addedAt`, `note`), la PK propia ya está disponible como ancla sin rediseñar la tabla.
2. **Consistencia:** todas las tablas del MCP tienen su propio `idNombreTabla` — las tablas de unión no son la excepción.
3. **Integridad igual de fuerte:** el UNIQUE constraint garantiza que no se duplique la asociación, igual que una PK compuesta.
4. **FKs externas más simples:** si otra tabla necesita referenciar una línea específica, lo hace con un solo GUID en lugar de dos columnas.

## Consecuencias

- Todas las junction tables del proyecto siguen este patrón: surrogate PK + UNIQUE constraint.
- El UNIQUE constraint debe estar presente siempre — sin él, la surrogate PK no previene duplicados lógicos.
- Aplica a: `tbCardStatementLines` y cualquier tabla de unión futura.

## Alternativa descartada

PK compuesta `(idA, idB)` — más común en literatura clásica, pero limita la extensibilidad y rompe la consistencia de nomenclatura del proyecto.
