# Scalar .NET Skill

Skill completo para implementar documentación de APIs con Scalar en .NET 10.

## Contenido

- **SKILL.md** - Guía principal con configuración, autenticación JWT, personalización y mejores prácticas
- **references/advanced-patterns.md** - Patrones avanzados, transformers personalizados, y ejemplos complejos

## Cuándo usar este skill

- Implementar documentación OpenAPI/Swagger en .NET 10
- Configurar autenticación JWT en documentación de API
- Reemplazar Swagger UI con Scalar
- Crear transformers de OpenAPI personalizados
- Documentar endpoints con metadata rica
- Configurar versionado de API

## Instalación de paquetes NuGet

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.2" />
<PackageReference Include="Microsoft.OpenApi" Version="2.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="2.12.32" />
```

## Inicio rápido

```csharp
// Program.cs
builder.Services.AddOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

Acceder a: `https://localhost:7191/scalar/v1`

## Características principales

✅ Configuración básica de Scalar  
✅ Autenticación JWT (Bearer tokens)  
✅ Document transformers personalizados  
✅ Múltiples esquemas de seguridad  
✅ Versionado de API  
✅ Personalización de temas  
✅ Metadata de endpoints  
✅ Respuestas tipadas  
✅ Integración con FluentValidation  
✅ Pruebas de configuración  

## Ventajas sobre Swagger UI

- **Nativo en .NET 10**: Recomendado por Microsoft
- **Mejor rendimiento**: Carga más rápida
- **UX superior**: Interfaz moderna e intuitiva
- **Mejor autenticación**: Setup más fácil para JWT/OAuth2
- **Ejemplos de código**: Múltiples lenguajes automáticamente
- **Sin Swashbuckle**: Usa OpenAPI nativo de .NET

## Ver también

- [dotnet-10-csharp-14](../dotnet-10-csharp-14/SKILL.md) - Mejores prácticas de .NET 10
- [Documentación oficial de Scalar](https://scalar.com)
- [Guía proyecto](../../docs/dotNet/scalar-documentacion-api.md) - Documentación completa del proyecto

---

**Versión**: 1.0  
**Última actualización**: 7 de febrero de 2026  
**Compatible con**: .NET 10.0.102, Scalar 2.12.32
