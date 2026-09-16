# 06 — Publicación opcional en Azure Container Apps

Esta referencia no forma parte del flujo cotidiano. Aplicarla únicamente cuando el usuario
o el proyecto requiera explícitamente un MCP remoto. El desarrollo, los cambios y el
debugging permanecen en `CompanyMcp.Stdio`.

## Gate antes de crear HTTP

Crear `CompanyMcp.Http` solo si al menos un consumidor no puede ejecutar el MCP local como
child process, por ejemplo Copilot Studio o un VS Code que consume un servidor central.
Si todos los consumidores son locales, terminar con stdio y omitir esta referencia.

## Por qué Azure Container Apps

Azure Container Apps (ACA) es el destino recomendado para este host opcional:

- **Escala a cero real.** Con `minReplicas: 0` el catálogo remoto no factura cómputo cuando
  nadie lo llama. Para un MCP consumido de forma esporádica por Copilot Studio, esto es la
  diferencia de costo relevante frente a mantener cómputo dedicado corriendo 24/7.
- **Build determinista.** El artefacto que corre en producción es exactamente la imagen que
  se construyó y probó en CI, sin depender de un buildpack que infiera cómo compilar el
  proyecto.
- **Escalado por HTTP/KEDA** administrado por la plataforma, sin gestionar un plan/SKU de
  cómputo por separado.
- **Ingress HTTPS administrado** con certificado automático en el dominio
  `*.azurecontainerapps.io`.

Si el tráfico es constante y alto, evaluar `minReplicas: 1` (o más) para evitar cold starts;
Container Apps sigue siendo válido, solo cambia la regla de escalado.

## Patrón recomendado

Usar **Bring your own MCP server**: empaquetar el host ASP.NET Core delgado que referencia
`CompanyMcp.Tools` y exponer `/mcp` en una imagen de contenedor. No duplicar tools dentro
del host HTTP.

## Dockerfile

```dockerfile
# examples/McpHttpServer/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/CompanyMcp.Http/CompanyMcp.Http.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CompanyMcp.Http.dll"]
```

Plantilla: [../examples/McpHttpServer/Dockerfile](../examples/McpHttpServer/Dockerfile).

Kestrel debe escuchar el puerto que Container Apps enruta como `targetPort` en su
configuración de ingress (`8080` en el ejemplo); no asumir un puerto fijo distinto sin
alinear ambos lados.

## Configuración del Container App

- Ingress externo, `targetPort` igual al puerto de `ASPNETCORE_URLS`, transporte HTTP
  (Streamable HTTP corre sobre HTTP/HTTPS normal, no requiere gRPC/h2c especial salvo que
  el SDK lo exija).
- `AllowedHosts` con el hostname público exacto de Container Apps.
- Health probe apuntando a `/health`.
- Secrets del Container App o Key Vault references para configuración sensible; nunca
  hardcodear en el Dockerfile ni en el YAML de despliegue.
- Managed identity (identidad asignada por el sistema o por el usuario) para acceder a
  Azure SQL, Storage, Key Vault u otros recursos downstream.
- Application Insights/OpenTelemetry con redacción de argumentos sensibles.
- Reglas de escalado (`scale.rules`) basadas en concurrencia HTTP; con HTTP stateless no
  hace falta session affinity.

## Escalado

Con HTTP stateless se puede escalar a varias réplicas sin session affinity:

- No guardar sesiones, locks ni resultados de negocio solo en memoria de una réplica.
- Usar base de datos, Redis, Storage o servicios externos para estado compartido.
- Mantener las tools breves y cancelables.
- Para trabajo largo, iniciar un job durable/cola y devolver un identificador consultable.
- `minReplicas: 0` es el default recomendado para MCP de uso esporádico; subir a `1+` solo
  si el cold start es inaceptable para el consumidor.

## Autenticación

Container Apps soporta autenticación integrada (Easy Auth) con Microsoft Entra ID delante
de la app. Aun así, aplicar autorización por tool dentro del proceso — la autenticación del
ingress no sustituye reglas por tool.

No reenviar el bearer del MCP a un downstream si el token no fue emitido para ese recurso.
Usar managed identity, client credentials o on-behalf-of según el escenario.

## GitHub Actions

Autenticar mediante OIDC, no un secreto de Docker Hub/ACR de larga vida:

1. Crear identidad/federated credential para el repositorio.
2. Asignar el rol mínimo requerido sobre el Container App y el Container Registry (ACR).
3. Configurar `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` y `AZURE_SUBSCRIPTION_ID` como variables
   o secrets apropiados.
4. Ejecutar build/tests y el smoke stdio antes de construir/publicar la imagen.
5. `docker build` + `docker push` a ACR, luego `az containerapp update --image ...`.

Plantilla: [../examples/ci/container-apps.yml](../examples/ci/container-apps.yml).

El workflow publica `CompanyMcp.Http` como imagen de contenedor; antes ejecuta tests y
smoke stdio del catálogo compartido. El smoke HTTP se agrega como verificación de paridad
del artefacto publicable.

## Revisiones

Container Apps versiona despliegues como **revisiones**, con split de tráfico:

```text
push main → build/test → build+push imagen → az containerapp update (nueva revisión)
  → smoke /mcp contra la revisión nueva (con label o 0% de tráfico) → mover tráfico 100%
```

Usar revisiones etiquetadas para hacer smoke antes de mover tráfico de producción, en vez
de desplegar directo al 100%.

## Checklist de entrega

- [ ] `/health` responde sin revelar configuración.
- [ ] `/mcp` solo acepta los hosts previstos (`AllowedHosts`).
- [ ] El catálogo HTTP coincide con el catálogo validado por stdio.
- [ ] La URL pública usa HTTPS (certificado administrado por Container Apps).
- [ ] Autenticación anónima solo si fue una decisión explícita.
- [ ] Reglas de escalado (`minReplicas`/`maxReplicas`) alineadas con el patrón de tráfico
      esperado.
- [ ] Escalado a varias réplicas probado con estado externo.
- [ ] Logs no contienen bodies ni tokens.
- [ ] Workflow usa OIDC, construye la imagen y falla si tests/smoke fallan.

Fuentes primarias:

- <https://learn.microsoft.com/azure/container-apps/overview>
- <https://learn.microsoft.com/azure/container-apps/scale-app>
- <https://learn.microsoft.com/azure/container-apps/github-actions>
- <https://learn.microsoft.com/azure/container-apps/authentication>
