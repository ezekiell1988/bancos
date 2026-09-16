# Build context: raíz del repo (necesita CompanyMcp.Tools y CompanyMcp.Http).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/CompanyMcp.Http/CompanyMcp.Http.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Debe coincidir con el targetPort configurado en el ingress del Container App.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CompanyMcp.Http.dll"]
