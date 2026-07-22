# Scalar Advanced Patterns & Examples

## Custom OpenAPI Transformers

### Multiple Security Schemes

Support both JWT and API Key authentication:

```csharp
internal sealed class MultiSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authProvider) 
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await authProvider.GetAllSchemesAsync();
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>();

        // JWT Bearer
        if (schemes.Any(s => s.Name == "Bearer"))
        {
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme"
            };
        }

        // API Key
        if (schemes.Any(s => s.Name == "ApiKey"))
        {
            document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key authentication"
            };
        }

        // OAuth2
        if (schemes.Any(s => s.Name == "OAuth"))
        {
            document.Components.SecuritySchemes["OAuth2"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("/oauth/authorize", UriKind.Relative),
                        TokenUrl = new Uri("/oauth/token", UriKind.Relative),
                        Scopes = new Dictionary<string, string>
                        {
                            ["read"] = "Read access",
                            ["write"] = "Write access",
                            ["admin"] = "Admin access"
                        }
                    }
                }
            };
        }
    }
}
```

### Add Server URLs

```csharp
internal sealed class ServerUrlTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Servers =
        [
            new OpenApiServer 
            { 
                Url = "https://api.example.com",
                Description = "Production server"
            },
            new OpenApiServer 
            { 
                Url = "https://staging.api.example.com",
                Description = "Staging server"
            },
            new OpenApiServer 
            { 
                Url = "https://localhost:7191",
                Description = "Local development"
            }
        ];

        return Task.CompletedTask;
    }
}
```

### Add Global Tags

```csharp
internal sealed class TagsTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Tags =
        [
            new OpenApiTag 
            { 
                Name = "Auth",
                Description = "Authentication and authorization endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Users",
                Description = "User management endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Products",
                Description = "Product catalog endpoints",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Product documentation",
                    Url = new Uri("https://docs.example.com/products")
                }
            }
        ];

        return Task.CompletedTask;
    }
}
```

---

## Endpoint Metadata Patterns

### Rich Endpoint Documentation

```csharp
app.MapPost("/api/v1/users", CreateUser)
   .WithName("CreateUser")
   .WithTags("Users")
   .WithSummary("Create a new user")
   .WithDescription("""
       Creates a new user account with the provided information.
       
       Requirements:
       - Email must be unique
       - Password must be at least 8 characters
       - Name is required
       
       Returns the created user with a 201 status code.
       """)
   .Accepts<CreateUserRequest>("application/json")
   .Produces<UserResponse>(StatusCodes.Status201Created)
   .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
   .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
   .ProducesValidationProblem()
   .RequireAuthorization("Admin");

static async Task<Results<Created<UserResponse>, ValidationProblem, Conflict<ProblemDetails>>>
CreateUser(
    CreateUserRequest request,
    IUserService userService,
    IValidator<CreateUserRequest> validator)
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        return TypedResults.ValidationProblem(validation.ToDictionary());

    var result = await userService.CreateAsync(request);
    
    return result.Match(
        user => TypedResults.Created($"/api/v1/users/{user.Id}", user),
        error => TypedResults.Conflict(new ProblemDetails 
        { 
            Title = "User already exists",
            Detail = error.Description
        })
    );
}
```

### File Upload Documentation

```csharp
app.MapPost("/api/v1/files/upload", UploadFile)
   .WithName("UploadFile")
   .WithTags("Files")
   .WithSummary("Upload a file")
   .Accepts<IFormFile>("multipart/form-data")
   .Produces<FileUploadResponse>(StatusCodes.Status200OK)
   .DisableAntiforgery(); // For API endpoints

static async Task<Ok<FileUploadResponse>> UploadFile(
    IFormFile file,
    IFileStorageService storageService)
{
    var fileId = await storageService.SaveAsync(file);
    return TypedResults.Ok(new FileUploadResponse(fileId, file.FileName));
}
```

### Streaming Responses

```csharp
app.MapGet("/api/v1/export/users", ExportUsers)
   .WithName("ExportUsers")
   .WithTags("Export")
   .WithSummary("Export users to CSV")
   .Produces<IAsyncEnumerable<string>>(StatusCodes.Status200OK, "text/csv");

static async IAsyncEnumerable<string> ExportUsers(
    IUserService userService,
    [EnumeratorCancellation] CancellationToken ct)
{
    yield return "Id,Name,Email\n";
    
    await foreach (var user in userService.GetAllAsync(ct))
    {
        yield return $"{user.Id},{user.Name},{user.Email}\n";
    }
}
```

---

## Response Type Patterns

### Custom Problem Details

```csharp
public record CustomProblemDetails : ProblemDetails
{
    public required string ErrorCode { get; init; }
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
    public string? TraceId { get; init; }
}

// Usage in endpoint
app.MapGet("/api/v1/users/{id}", GetUser)
   .Produces<UserDto>(StatusCodes.Status200OK)
   .Produces<CustomProblemDetails>(StatusCodes.Status404NotFound);

static async Task<Results<Ok<UserDto>, NotFound<CustomProblemDetails>>> GetUser(
    int id,
    IUserService userService,
    HttpContext context)
{
    var user = await userService.GetByIdAsync(id);
    
    return user is not null
        ? TypedResults.Ok(user)
        : TypedResults.NotFound(new CustomProblemDetails
        {
            Title = "User not found",
            Status = 404,
            ErrorCode = "USER_NOT_FOUND",
            TraceId = context.TraceIdentifier
        });
}
```

### Paginated Responses

```csharp
public record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

app.MapGet("/api/v1/users", GetUsers)
   .WithName("GetUsers")
   .Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK);

static async Task<Ok<PagedResponse<UserDto>>> GetUsers(
    [AsParameters] PaginationQuery query,
    IUserService userService)
{
    var result = await userService.GetPagedAsync(query.Page, query.PageSize);
    return TypedResults.Ok(result);
}

public record PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}
```

---

## Advanced Scalar Configuration

### Custom CSS Theming

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("My API Documentation")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithCustomCss("""
            :root {
                --scalar-color-1: #1a202c;
                --scalar-color-2: #2d3748;
                --scalar-color-3: #4a5568;
            }
            """)
        .ShowSidebar(true)
        .HideModels(false)
        .DarkMode(true);
});
```

### Multiple Document Versions

```csharp
// Configure multiple versions
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info.Version = "1.0";
        doc.Info.Title = "My API v1";
        return Task.CompletedTask;
    });
});

builder.Services.AddOpenApi("v2", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info.Version = "2.0";
        doc.Info.Title = "My API v2";
        doc.Info.Description = "Version 2.0 with breaking changes";
        return Task.CompletedTask;
    });
});

// Map both versions
app.MapOpenApi();

// Scalar for each version
app.MapScalarApiReference("/scalar/v1", options =>
{
    options.WithTitle("My API v1");
});

app.MapScalarApiReference("/scalar/v2", options =>
{
    options.WithTitle("My API v2");
});
```

### Environment-Specific Configuration

```csharp
app.MapScalarApiReference(options =>
{
    var config = options
        .WithTitle($"My API - {app.Environment.EnvironmentName}")
        .ShowSidebar(true);

    if (app.Environment.IsDevelopment())
    {
        config
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .DarkMode(true);
    }
    
    if (app.Environment.IsStaging())
    {
        config.WithApiKeyAuthentication(key =>
        {
            key.Token = builder.Configuration["Scalar:StagingApiKey"];
        });
    }
});
```

---

## Integration with FluentValidation

### Validation Error Responses in OpenAPI

```csharp
using FluentValidation;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain lowercase letter")
            .Matches(@"\d").WithMessage("Password must contain digit");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);
    }
}

// Endpoint with validation
app.MapPost("/api/v1/users", async (
    CreateUserRequest request,
    IValidator<CreateUserRequest> validator,
    IUserService userService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var user = await userService.CreateAsync(request);
    return TypedResults.Created($"/api/v1/users/{user.Id}", user);
})
.WithName("CreateUser")
.ProducesValidationProblem()
.Produces<UserResponse>(StatusCodes.Status201Created);
```

---

## Testing Scalar Configuration

### Integration Test Example

```csharp
public class ScalarConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScalarConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApi_Document_ShouldInclude_BearerSecurity()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");
        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        response.Should().BeSuccessful();
        document.GetProperty("components")
                .GetProperty("securitySchemes")
                .GetProperty("Bearer")
                .GetProperty("type")
                .GetString()
                .Should().Be("http");
    }

    [Fact]
    public async Task Scalar_Endpoint_ShouldBeAccessible_InDevelopment()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/scalar/v1");

        // Assert
        response.Should().BeSuccessful();
    }
}
```

---

## Performance Optimizations

### Caching OpenAPI Document

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Add response caching
builder.Services.AddResponseCaching();

var app = builder.Build();

app.UseResponseCaching();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
       .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    
    app.MapScalarApiReference();
}
```

### Lazy Loading for Large APIs

For APIs with hundreds of endpoints, consider splitting into multiple documents:

```csharp
builder.Services.AddOpenApi("auth");
builder.Services.AddOpenApi("users");
builder.Services.AddOpenApi("products");

// Only include relevant endpoints in each document
app.MapGroup("/api/v1/auth")
   .WithGroupName("auth")
   .MapAuthEndpoints();

app.MapGroup("/api/v1/users")
   .WithGroupName("users")
   .MapUserEndpoints();

app.MapGroup("/api/v1/products")
   .WithGroupName("products")
   .MapProductEndpoints();
```

---

## Common Issues & Solutions

### Problem: Endpoints not appearing in Scalar

**Cause**: Endpoints excluded or not properly registered

**Solution**:
```csharp
// Ensure endpoints are not excluded
app.MapGet("/api/users", GetUsers)
   .WithName("GetUsers")  // Ensure name is set
   .WithTags("Users");    // Add to a tag

// Check you're not excluding
// .ExcludeFromDescription(); // Remove this
```

### Problem: Response types not showing correctly

**Cause**: Missing `.Produces<T>()` metadata

**Solution**:
```csharp
app.MapGet("/api/users", GetUsers)
   .Produces<List<UserDto>>(StatusCodes.Status200OK)
   .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
```

### Problem: Authentication not working in Scalar

**Cause**: Transformer not registered or JWT service missing

**Solution**:
```csharp
// Ensure JWT is configured
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* config */ });

// Ensure transformer is added
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
```

---

## See Also

- [SKILL.md](../SKILL.md) - Main Scalar skill documentation
- [security.md](../../dotnet-10-csharp-14/security.md) - JWT authentication patterns
- [minimal-apis.md](../../dotnet-10-csharp-14/minimal-apis.md) - Endpoint patterns
