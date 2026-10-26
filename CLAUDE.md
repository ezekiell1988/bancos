# CLAUDE.md — Bancos

Seguir `AGENTS.md`. Usar MCP `iaWorkflow` para todo trabajo del proyecto (`ia_validate`, `ia_get_context`, `work_task`, `finish_task`); no recorrer `/ia` manualmente.

* Movimientos y respaldos van a `datos/` con el formato de `README.md`; `datos/` está fuera de git (repo público).
* No exponer datos financieros, cuentas, credenciales ni archivos fuente fuera de `datos/`.
* Escritos de workflow: preview primero, `apply: true` solo tras aprobación.
* Código anterior (MCP .NET + SQL): rama `legacy`, solo consulta.
