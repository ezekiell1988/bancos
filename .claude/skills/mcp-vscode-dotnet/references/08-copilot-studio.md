# 08 — Compatibilidad con Copilot Studio

Esta referencia es completamente opcional. El MCP se desarrolla y depura por stdio; solo
cuando Copilot Studio se convierte en consumidor se agrega el host HTTP y se valida que
cumpla las restricciones del cliente remoto.

## Requisitos base

- Streamable HTTP; no SSE legacy.
- URL HTTPS pública y certificado confiable.
- Ruta exacta registrada en el onboarding wizard, por ejemplo
  `https://company-mcp.<env-id>.<region>.azurecontainerapps.io/mcp`.
- Tools con nombres, descripciones y schemas estables.
- Autenticación `None`, API key u OAuth 2.0 según el riesgo y el soporte configurado.

Copilot Studio dejó de soportar SSE para MCP después de agosto de 2025. No habilitar SSE
como workaround.

## Regla de implementación

Conservar `CompanyMcp.Tools` como catálogo único y el SDK oficial como dueño del protocolo.
`CompanyMcp.Http` referencia ese assembly; no copia clases de tools ni reemplaza el host
stdio usado durante desarrollo. No copiar un router JSON-RPC, una lista fija de versiones
ni normalizadores de requests de implementaciones manuales.

Si una captura reproducible muestra una peculiaridad exclusiva de Copilot Studio:

1. Guardar request/response redactados como fixture de regresión.
2. Confirmar que persiste con la versión actual del SDK.
3. Implementar el adaptador mínimo como middleware delante de `MapMcp`.
4. Limitarlo por shape/ruta, registrar métrica y agregar test.
5. Retirarlo cuando SDK/cliente resuelvan la incompatibilidad.

## Identidad

- Con OAuth, validar criptográficamente issuer, audience, firma y lifetime.
- Inyectar `ClaimsPrincipal` en las tools; no confiar en JWT solo decodificado.
- No asumir que headers de identidad no firmados pertenecen al mismo tenant.
- Autorizar dentro de cada tool, además de proteger el endpoint.
- Para Graph/downstream, usar identidad/delegación explícita; no reenviar el bearer MCP.

## Pruebas cruzadas

Antes de publicar:

1. Build, contract tests y smoke stdio con el cliente C# oficial.
2. Comparar el catálogo del host HTTP con el catálogo stdio.
3. Conexión desde VS Code HTTP.
4. Onboarding y discovery desde Copilot Studio.
5. Tool pública/read-only.
6. Tool privada con usuario autorizado y no autorizado.
7. Write tool en preview y apply dentro de un entorno de prueba.

Fuente primaria:

- <https://learn.microsoft.com/microsoft-copilot-studio/mcp-add-existing-server-to-agent>
