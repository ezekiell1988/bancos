# 04 — VS Code: stdio local y HTTP remoto opcional

## Configuración local obligatoria: stdio

VS Code debe iniciar el proceso .NET, igual que inicia un MCP Node local. `.vscode/mcp.json`:

```json
{
  "servers": {
    "companyMcpLocal": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/CompanyMcp.Stdio/CompanyMcp.Stdio.csproj",
        "--no-launch-profile",
        "--tl:off",
        "--verbosity",
        "quiet"
      ],
      "cwd": "${workspaceFolder}",
      "dev": {
        "watch": [
          "src/CompanyMcp.Tools/**/*.cs",
          "src/CompanyMcp.Stdio/**/*.cs"
        ]
      }
    }
  }
}
```

No hay puerto, URL ni endpoint local. `command` y `args` hacen que VS Code administre el
lifecycle del child process. `dev.watch` reinicia `dotnet run` tras cambios y vuelve a
compilar. `--tl:off` evita secuencias del terminal logger en stdout; `--verbosity quiet`
reduce salida del build. El proyecto debe compilar sin warnings y el host debe enviar todos
los logs a stderr para no contaminar el protocolo.

Ejemplo listo para copiar:

- [../examples/config/vscode-mcp.local.json](../examples/config/vscode-mcp.local.json)

## Configuración HTTP remota — opcional

Solo agregar esta variante cuando ya exista un despliegue remoto. No usarla para el ciclo
normal de cambios y debugging:

```json
{
  "servers": {
    "companyMcp": {
      "type": "http",
      "url": "https://company-mcp.<env-id>.<region>.azurecontainerapps.io/mcp"
    }
  }
}
```

VS Code intenta Streamable HTTP y puede hacer fallback a SSE. El host opcional debe
implementar Streamable HTTP; no habilitar SSE solo por el fallback del cliente. Mantener la
entrada stdio como configuración local de desarrollo.

## Headers y secretos

Para una API key o bearer de desarrollo:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "mcp-token",
      "description": "MCP access token",
      "password": true
    }
  ],
  "servers": {
    "companyMcp": {
      "type": "http",
      "url": "https://company-mcp.<env-id>.<region>.azurecontainerapps.io/mcp",
      "headers": {
        "Authorization": "Bearer ${input:mcp-token}"
      }
    }
  }
}
```

Nunca escribir tokens reales en el archivo versionado.

## OAuth

Si el servidor implementa el flujo OAuth MCP, VS Code acepta:

```json
"oauth": { "clientId": "<CLIENT-ID>" }
```

VS Code abre el navegador en la primera conexión. Verificar discovery/protected resource
metadata y callback con el proveedor de identidad; no improvisar headers OAuth manuales.

## Ubicación del archivo

- `.vscode/mcp.json`: configuración del workspace administrada por VS Code.
- `.mcp.json`: formato portable leído directamente por Agent Host en escenarios compatibles.
- Config de usuario: servidores personales reutilizados entre workspaces.

No copiar mecánicamente entre formatos/clients: validar el schema que ofrece el editor.
Para un MCP project-owned, versionar `.vscode/mcp.json` sin secretos:

```gitignore
.vscode/*
!.vscode/mcp.json
```

## Ciclo obligatorio de desarrollo y debugging

1. Ejecutar `dotnet build` y resolver warnings.
2. Abrir `MCP: List Servers`; VS Code inicia `CompanyMcp.Stdio` automáticamente.
3. Revisar `Show Output`: los logs del host deben aparecer como stderr.
4. Tras agregar/renombrar tools, dejar que `dev.watch` reinicie el proceso.
5. Ejecutar `MCP: Reset Cached Tools` si el catálogo anterior persiste.
6. Verificar `tools/list` y llamar una tool con argumentos conocidos.
7. Para breakpoints, mantener el server stdio activo y usar `.NET: Attach to Process` sobre
   el proceso `CompanyMcp.Stdio`; no cambiar a HTTP para depurar.

El campo MCP `dev.debug` integrado admite principalmente Node/Python, no .NET. Para C# se
usa el debugger normal de .NET adjunto al child process stdio. El transporte observado por
VS Code y por los smoke tests sigue siendo `stdio`.

Ejemplos:

- [../examples/config/vscode-mcp.remote-oauth.json](../examples/config/vscode-mcp.remote-oauth.json)
- [../examples/config/vscode-mcp.remote-header.json](../examples/config/vscode-mcp.remote-header.json)

Fuente primaria:

- <https://code.visualstudio.com/docs/agents/reference/mcp-configuration>
