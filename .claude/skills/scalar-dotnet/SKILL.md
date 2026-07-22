---
name: scalar-dotnet
description: Use when implementing Scalar API documentation in .NET 10 applications; when setting up OpenAPI/Swagger documentation; when configuring JWT authentication in API docs; when users ask about API documentation, Scalar, OpenAPI transformers, or replacing Swagger UI
---

# Scalar API Documentation for .NET 10

Scalar is Microsoft's recommended modern alternative to Swagger UI for .NET 10. Provides superior performance, better authentication UX, and multi-language code examples.

**Official docs:** [Scalar](https://scalar.com/products/api-references) | [ASP.NET Core Integration](https://scalar.com/products/api-references/integrations/aspnetcore/integration) | [Microsoft OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents)

## Why Scalar over Swagger UI?

- **Native in .NET 10**: Microsoft recommends Scalar as primary option
- **Modern UI**: Clean, contemporary design
- **Better performance**: Faster than traditional Swagger UI
- **Superior auth UX**: Intuitive JWT, OAuth2, API Keys configuration
- **Code generation**: Examples in cURL, JavaScript, Python, C#, etc.
- **Native support**: No need for Swashbuckle

---

## Quick Setup

### 1. Install Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.2" />
<PackageReference Include="Microsoft.OpenApi" Version="2.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="2.12.32" />
```

### 2. Basic Configuration (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add native .NET 10 OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Development only
if (app.Environment.IsDevelopment())
{
    // Expose OpenAPI document
    app.MapOpenApi();
    
    // Add Scalar UI
    app.MapScalarApiReference();
}

app.Run();
```

**Access:**
- Scalar UI: `https://localhost:7191/scalar/v1`
- OpenAPI JSON: `https://localhost:7191/openapi/v1.json`

---

## JWT Authentication Setup

### Document Transformer

Create a transformer to add JWT Bearer authentication to OpenAPI schema:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) 
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document, 
        OpenApiDocumentTransformerContext context, 
        CancellationToken cancellationToken)
    {
        var authSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authSchemes.Any(s => s.Name == "Bearer")) return;

        // Add security scheme
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token",
                Description = "Enter JWT token obtained from login endpoint"
            }
        };

        // Apply to all operations
        foreach (var operation in document.Paths.Values
            .SelectMany(path => path.Operations))
        {
            operation.Value.Security ??= [];
            operation.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        }
    }
}
```

### Register Transformer

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
```

---

## Customization

### Theme and Title

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("My API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .ShowSidebar(true);
});
```

### Pre-fill Authentication (Development Only)

```csharp
// ⚠️ DEVELOPMENT ONLY - Never use real tokens in production
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes("Bearer")
        .AddHttpAuthentication("Bearer", auth =>
        {
            auth.Token = "your_dev_token_here";
        }));
}
```

---

## Using Authentication in Scalar

### Step 1: Get JWT Token

Call your login endpoint:

```bash
POST /api/v1/Auth/login
Content-Type: application/json

{
  "emailLogin": "user@example.com",
  "password": "Password123!"
}
```

Response:
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-08T12:00:00Z"
}
```

### Step 2: Configure in Scalar

1. In Scalar UI, find the authentication button or "Bearer Token" field
2. Paste the JWT token (WITHOUT "Bearer" prefix)
3. Scalar automatically adds `Authorization: Bearer {token}` header

### Step 3: Test Protected Endpoints

Now `[Authorize]` endpoints work:
```csharp
app.MapGet("/api/v1/protected", [Authorize] () => "Secret data")
   .WithName("GetProtectedData");
```

---

## API Versioning

Support multiple API versions:

```csharp
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options.WithTitle("My API v1");
});
```

Access:
- v1: `https://localhost:7191/scalar/v1`
- v2: `https://localhost:7191/scalar/v2`

---

## Common Patterns

### Add Metadata to Endpoints

```csharp
app.MapGet("/api/users/{id}", GetUserById)
   .WithName("GetUser")
   .WithTags("Users")
   .WithSummary("Get user by ID")
   .WithDescription("Returns detailed user information")
   .Produces<UserDto>(StatusCodes.Status200OK)
   .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

static async Task<Results<Ok<UserDto>, NotFound<ProblemDetails>>> GetUserById(
    int id, 
    IUserService userService)
{
    var user = await userService.GetByIdAsync(id);
    return user is not null 
        ? TypedResults.Ok(user) 
        : TypedResults.NotFound(new ProblemDetails 
        { 
            Title = "User not found", 
            Status = 404 
        });
}
```

### Document Request/Response Models

Use attributes for better OpenAPI documentation:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public record LoginRequest
{
    [Required, EmailAddress]
    [Description("User email address")]
    public required string EmailLogin { get; init; }

    [Required, MinLength(8)]
    [Description("User password (min 8 characters)")]
    public required string Password { get; init; }
}
```

### Exclude Endpoints from OpenAPI

```csharp
app.MapGet("/internal/health", () => "OK")
   .ExcludeFromDescription(); // Won't appear in Scalar
```

---

## Troubleshooting

### Authentication button not visible

1. Verify `BearerSecuritySchemeTransformer` is registered
2. Check JWT authentication service is configured
3. Inspect `/openapi/v1.json` for `components.securitySchemes`

### Token not accepted

1. Ensure token hasn't expired
2. Don't include "Bearer" prefix when pasting in Scalar
3. Verify JWT configuration in `appsettings.json`

### Scalar doesn't load

1. Check `ASPNETCORE_ENVIRONMENT=Development`
2. Confirm `Scalar.AspNetCore` package is installed
3. Ensure `app.MapScalarApiReference()` is called AFTER `app.MapOpenApi()`

---

## Best Practices

✅ **DO:**
- Use `IOpenApiDocumentTransformer` for security schemes
- Add metadata with `.WithName()`, `.WithTags()`, `.WithSummary()`
- Use `TypedResults<T>` for type-safe responses
- Document all public endpoints
- Use versioning for breaking changes

❌ **DON'T:**
- Hard-code authentication tokens in production
- Expose Scalar in production (development only)
- Forget to add `.Produces<T>()` for response types
- Use `[ApiExplorerSettings(IgnoreApi = true)]` without reason

---

## See Also

- [minimal-apis.md](../dotnet-10-csharp-14/minimal-apis.md) - Minimal API patterns
- [security.md](../dotnet-10-csharp-14/security.md) - JWT authentication setup
- Official Scalar docs: https://scalar.com
- Microsoft OpenAPI docs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi

---

**Last updated:** February 7, 2026  
**Scalar version:** 2.12.32  
**.NET version:** 10.0.102
