using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SpecmaticProvider.Models;
using SpecmaticProvider.Services;
using SpecmaticProvider;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// JWT Configuration
var jwtSecret = "your-super-secret-key-min-32-characters-for-testing-only-change-in-production";

// Add Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Authority = "http://localhost:5001";
        options.RequireHttpsMetadata = false;
    });

// Add Authorization Policies with custom scope validation
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadProducts", policy =>
        policy.Requirements.Add(new ScopeRequirement("read:products")));
    
    options.AddPolicy("WriteProducts", policy =>
        policy.Requirements.Add(new ScopeRequirement("write:products")));
});

// Add scope authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

// Add services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// OAuth token endpoint for testing
app.MapPost("/oauth/token", () =>
{
    var tokenGenerator = new TokenGenerator(jwtSecret);
    var token = tokenGenerator.GenerateToken(
        clientId: "test-client",
        scopes: new[] { "read:products", "write:products" },
        expirationMinutes: 60
    );

    return Results.Ok(new
    {
        access_token = token,
        token_type = "Bearer",
        expires_in = 3600,
        scope = "read:products write:products"
    });
}).WithName("GetOAuthToken").WithOpenApi();

// Health check endpoint - NO auth required
app.MapGet("/api/health", () =>
{
    return Results.Ok(new { status = "ok" });
})
.WithName("GetHealth")
.WithOpenApi();

// Get all products - Requires read:products scope
app.MapGet("/api/products", async (IProductRepository repo, int skip = 0, int take = 10) =>
{
    if (skip < 0 || take < 1 || take > 100)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Code = "INVALID_PARAMETERS",
            Message = "Skip must be >= 0, and take must be between 1 and 100"
        });
    }

    var products = await repo.GetAllAsync(skip, take);
    return Results.Ok(products);
})
.RequireAuthorization("ReadProducts")
.WithName("GetProducts")
.WithOpenApi();

// Get product by ID
app.MapGet("/api/products/{id}", async (int id, IProductRepository repo) =>
{
    if (id < 1)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Code = "INVALID_ID",
            Message = "Product ID must be greater than 0"
        });
    }

    var product = await repo.GetByIdAsync(id);
    if (product == null)
    {
        return Results.NotFound(new ErrorResponse
        {
            Code = "NOT_FOUND",
            Message = $"Product with ID {id} not found"
        });
    }

    return Results.Ok(product);
})
.RequireAuthorization("ReadProducts")
.WithName("GetProductById")
.WithOpenApi();

// Create product - Requires write:products scope
app.MapPost("/api/products", async (CreateProductRequest request, IProductRepository repo) =>
{
    var errors = new List<ValidationError>();

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        errors.Add(new ValidationError { Field = "name", Issue = "Name is required" });
    }
    else if (request.Name.Length > 200)
    {
        errors.Add(new ValidationError { Field = "name", Issue = "Name must not exceed 200 characters" });
    }

    if (request.Price <= 0)
    {
        errors.Add(new ValidationError { Field = "price", Issue = "Price must be greater than 0" });
    }

    if (request.Discount.HasValue && (request.Discount < 0 || request.Discount > 100))
    {
        errors.Add(new ValidationError { Field = "discount", Issue = "Discount must be between 0 and 100" });
    }

    if (!IsValidCategory(request.Category))
    {
        errors.Add(new ValidationError { Field = "category", Issue = "Invalid category" });
    }

    if (errors.Count > 0)
    {
        return Results.UnprocessableEntity(new ValidationErrorResponse
        {
            Code = "VALIDATION_ERROR",
            Message = "Validation failed",
            Errors = errors
        });
    }

    var product = await repo.CreateAsync(request);
    return Results.Created($"/api/products/{product.Id}", product);
})
.RequireAuthorization("WriteProducts")
.WithName("CreateProduct")
.WithOpenApi();

// Update product - Requires write:products scope
app.MapPut("/api/products/{id}", async (int id, UpdateProductRequest request, IProductRepository repo) =>
{
    if (id < 1)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Code = "INVALID_ID",
            Message = "Product ID must be greater than 0"
        });
    }

    var errors = new List<ValidationError>();

    if (request.Name != null && request.Name.Length > 200)
    {
        errors.Add(new ValidationError { Field = "name", Issue = "Name must not exceed 200 characters" });
    }

    if (request.Price.HasValue && request.Price <= 0)
    {
        errors.Add(new ValidationError { Field = "price", Issue = "Price must be greater than 0" });
    }

    if (request.Discount.HasValue && (request.Discount < 0 || request.Discount > 100))
    {
        errors.Add(new ValidationError { Field = "discount", Issue = "Discount must be between 0 and 100" });
    }

    if (request.Category != null && !IsValidCategory(request.Category))
    {
        errors.Add(new ValidationError { Field = "category", Issue = "Invalid category" });
    }

    if (errors.Count > 0)
    {
        return Results.UnprocessableEntity(new ValidationErrorResponse
        {
            Code = "VALIDATION_ERROR",
            Message = "Validation failed",
            Errors = errors
        });
    }

    var existingProduct = await repo.GetByIdAsync(id);
    if (existingProduct == null)
    {
        return Results.NotFound(new ErrorResponse
        {
            Code = "NOT_FOUND",
            Message = $"Product with ID {id} not found"
        });
    }

    var updatedProduct = await repo.UpdateAsync(id, request);
    return Results.Ok(updatedProduct);
})
.RequireAuthorization("WriteProducts")
.WithName("UpdateProduct")
.WithOpenApi();

// Delete product - Requires write:products scope
app.MapDelete("/api/products/{id}", async (int id, IProductRepository repo) =>
{
    if (id < 1)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Code = "INVALID_ID",
            Message = "Product ID must be greater than 0"
        });
    }

    var deleted = await repo.DeleteAsync(id);
    if (!deleted)
    {
        return Results.NotFound(new ErrorResponse
        {
            Code = "NOT_FOUND",
            Message = $"Product with ID {id} not found"
        });
    }

    return Results.NoContent();
})
.RequireAuthorization("WriteProducts")
.WithName("DeleteProduct")
.WithOpenApi();

app.Run("http://localhost:5000");

static bool IsValidCategory(string category)
{
    var validCategories = new[] { "Electronics", "Clothing", "Books", "Home", "Other" };
    return validCategories.Contains(category);
}

// Custom scope authorization requirement and handler
class ScopeRequirement : IAuthorizationRequirement
{
    public string Scope { get; }

    public ScopeRequirement(string scope)
    {
        Scope = scope;
    }
}

class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        // Check if user has scope claim
        var scopeClaim = context.User.FindFirst("scope");
        if (scopeClaim != null)
        {
            // Parse space-separated scopes
            var userScopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (userScopes.Contains(requirement.Scope))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
