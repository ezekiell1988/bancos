---
name: mcp-chat-gpt
description: >
  Build an MCP (Model Context Protocol) server compatible with ChatGPT / OpenAI using .NET 10 / C# 14.
  Covers Streamable HTTP transport, spec 2025-06-18 compliance (Mcp-Session-Id, MCP-Protocol-Version,
  Origin validation, DELETE), authentication (API key + OAuth), output_schema, structuredContent,
  and ChatGPT-specific quirks. Triggers: mcp chatgpt, mcp openai, chatgpt mcp server, openai mcp.
---

# MCP para ChatGPT / OpenAI — Skill de Construcción (.NET 10 / C# 14)

**WORKFLOW SKILL** — Guía completa para construir un servidor MCP que funcione correctamente con **ChatGPT (OpenAI)** usando **.NET 10 y C# 14**. Transporte: Streamable HTTP exclusivamente.

> Requiere el skill [dotnet-10-csharp-14](../dotnet-10-csharp-14/SKILL.md) para patrones de C# 14, Options, TypedResults y resiliencia HTTP.

## Usa Este Skill Cuando:

- ✅ El usuario quiere crear o mejorar un servidor MCP para conectar con ChatGPT
- ✅ Hay dudas sobre compliance con la spec MCP 2025-06-18 para OpenAI
- ✅ Se necesita implementar autenticación, headers de protocolo o output_schema para ChatGPT
- ✅ ChatGPT no conecta o rechaza el servidor MCP

## NO Uses Este Skill Para:

- ❌ MCP para Copilot Studio → usar skill `mcp-copilot-studio`
- ❌ MCP para Claude Desktop / Claude Code (SSE transport) → ver referencia SSE separada
- ❌ Creación de GPTs o configuración dentro de ChatGPT (eso es lado cliente)

---

## Índice de Referencia

| Archivo | Contenido |
|---------|-----------|
| [01-protocol-spec.md](references/01-protocol-spec.md) | Spec MCP 2025-06-18 completa: JSON-RPC 2.0, flujo, headers obligatorios, sesiones, DELETE |
| [02-chatgpt-requirements.md](references/02-chatgpt-requirements.md) | Requisitos específicos de ChatGPT/OpenAI: auth, output_schema, structuredContent, quirks |
| [03-tool-design.md](references/03-tool-design.md) | Diseño de tools: inputSchema, outputSchema, optimización de tokens, structuredContent |
| [04-implementation.md](references/04-implementation.md) | Implementación completa ASP.NET Core Minimal APIs con todos los headers y auth |
| [05-checklist.md](references/05-checklist.md) | Checklist de validación, errores comunes, comandos curl de prueba |

---

## Puntos Críticos — Resumen Rápido

1. **Transport: Streamable HTTP** — ChatGPT usa POST exclusivamente. Un único endpoint (`/mcp`) acepta POST para JSON-RPC y GET para SSE (spec compliance). No usa SSE como transporte primario.
2. **`Mcp-Session-Id` header** — El servidor DEBE devolver este header en la respuesta de `initialize`. El cliente lo envía en todas las requests posteriores. Sin esto, ChatGPT puede no mantener estado entre calls.
3. **`MCP-Protocol-Version` header** — El cliente lo envía en todas las requests post-initialize. El servidor DEBE validarlo y retornar HTTP 400 si la versión no es soportada.
4. **Validación de `Origin`** — Obligatoria por spec para prevenir DNS rebinding. Configurar whitelist de orígenes permitidos.
5. **Autenticación** — ChatGPT soporta OAuth 2.0 y API key. Para servidores propios, API key en header `Authorization: Bearer <key>` es lo más simple.
6. **`output_schema`** en tool definitions — ChatGPT lo usa para validar respuestas y generar citaciones. Agregar JSON Schema de salida a cada tool.
7. **`structuredContent`** en respuestas — Además de `content[0].text`, incluir `structuredContent` con el objeto tipado para que ChatGPT pueda generar citaciones y UI rica.
8. **`content[0].text`** — Sigue siendo obligatorio como fallback. Siempre incluir ambos: `content` y `structuredContent`.
9. **DELETE `/mcp`** — El cliente envía DELETE para terminar sesiones. El servidor debe limpiar el estado de la sesión.
10. **HTTPS obligatorio** — ChatGPT rechaza URLs HTTP.
11. **`notifications/*` → 202** — Igual que otros clientes MCP: notificaciones se responden con HTTP 202 sin body JSON-RPC.
12. **No batch** — A diferencia de Copilot Studio, ChatGPT envía requests individuales bien formadas (no arrays malformados). No necesitar `NormalizeBatch`.
