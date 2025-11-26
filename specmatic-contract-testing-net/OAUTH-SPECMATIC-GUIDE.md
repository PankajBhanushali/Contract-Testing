# Specmatic Contract Testing with OAuth Security

## Overview

OAuth-secured APIs present a unique challenge for contract testing. Specmatic has specific mechanisms to handle authentication and authorization during contract validation.

---

## How Specmatic Handles OAuth

### Core Concept

Specmatic can test OAuth-secured APIs through:

1. **Security Definitions in OpenAPI Spec** - Define OAuth requirements
2. **Token Injection** - Provide valid tokens during testing
3. **Mock Auth Server** - Use a mock OAuth provider for tests
4. **Security Bypass** - Skip auth for contract validation (not recommended)
5. **Real OAuth Integration** - Connect to actual OAuth provider

---

## Method 1: OpenAPI Security Definitions

### Step 1: Define OAuth in Spec

**File: `specs/products-api-oauth.yaml`**

```yaml
openapi: 3.0.0
info:
  title: Secure Product API
  version: 1.0.0
  
servers:
  - url: http://localhost:5000
    description: Local development server

# Define OAuth Security Scheme
components:
  securitySchemes:
    oauth2:
      type: oauth2
      flows:
        clientCredentials:  # OAuth 2.0 Client Credentials Flow
          tokenUrl: http://localhost:5001/oauth/token
          scopes:
            read:products: Read product data
            write:products: Modify product data
            admin: Full admin access

  schemas:
    Product:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
        price:
          type: number
          format: decimal
      required:
        - id
        - name
        - price

paths:
  /api/health:
    get:
      summary: Health check (no auth required)
      tags:
        - Health
      security: []  # No authentication required
      responses:
        '200':
          description: Service is healthy
          content:
            application/json:
              schema:
                type: object
                properties:
                  status:
                    type: string

  /api/products:
    get:
      summary: Get all products (requires read:products scope)
      tags:
        - Products
      security:
        - oauth2:
            - read:products  # Requires this OAuth scope
      responses:
        '200':
          description: List of products
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Product'
        '401':
          description: Unauthorized (missing or invalid token)
        '403':
          description: Forbidden (insufficient permissions)

    post:
      summary: Create product (requires write:products scope)
      tags:
        - Products
      security:
        - oauth2:
            - write:products  # Requires this OAuth scope
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                name:
                  type: string
                price:
                  type: number
              required:
                - name
                - price
      responses:
        '201':
          description: Product created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'
        '401':
          description: Unauthorized
        '403':
          description: Forbidden

# Global security - applies to all endpoints unless overridden
# security:
#   - oauth2:
#       - read:products
#       - write:products
```

---

## Method 2: Provider-Side OAuth Implementation (.NET)

### Step 1: Add OAuth Middleware to Provider

**File: `Provider/src/Program.cs`** (add OAuth setup)

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Configure OAuth/JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-super-secret-key-min-32-characters-long";
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero
};

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tokenValidationParameters;
        // For testing: allow invalid certificates if needed
        options.Authority = "http://localhost:5001"; // Mock OAuth server
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadProducts", policy =>
        policy.RequireClaim("scope", "read:products"));
    
    options.AddPolicy("WriteProducts", policy =>
        policy.RequireClaim("scope", "write:products"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run("http://localhost:5000");
```

### Step 2: Secure Endpoints with [Authorize]

**File: `Provider/src/Controllers/ProductController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpecmaticProvider.Controllers;

[ApiController]
[Route("api")]
public class ProductController : ControllerBase
{
    // Health check - NO auth required
    [HttpGet("health")]
    [AllowAnonymous]  // Explicitly allow anonymous
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    // GET products - Requires "read:products" scope
    [HttpGet("products")]
    [Authorize(Policy = "ReadProducts")]  // Requires OAuth scope
    public IActionResult GetProducts([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var products = GetSampleProducts()
            .Skip(skip)
            .Take(take)
            .ToList();
        
        return Ok(products);
    }

    // POST product - Requires "write:products" scope
    [HttpPost("products")]
    [Authorize(Policy = "WriteProducts")]  // Requires OAuth scope
    public IActionResult CreateProduct([FromBody] CreateProductRequest request)
    {
        // Validation logic...
        var product = new Product
        {
            Id = new Random().Next(1000, 9999),
            Name = request.Name,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };
        
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    // GET by ID - Requires "read:products" scope
    [HttpGet("products/{id}")]
    [Authorize(Policy = "ReadProducts")]
    public IActionResult GetProductById(int id)
    {
        // Implementation...
    }

    // PUT product - Requires "write:products" scope
    [HttpPut("products/{id}")]
    [Authorize(Policy = "WriteProducts")]
    public IActionResult UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        // Implementation...
    }

    // DELETE product - Requires "write:products" scope
    [HttpDelete("products/{id}")]
    [Authorize(Policy = "WriteProducts")]
    public IActionResult DeleteProduct(int id)
    {
        // Implementation...
    }

    private static List<Product> GetSampleProducts()
    {
        return new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Mouse", Price = 29.99m, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "Keyboard", Price = 79.99m, CreatedAt = DateTime.UtcNow }
        };
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

---

## Method 3: Mock OAuth Server for Testing

### Create a Simple Mock OAuth Server

**File: `Provider/src/MockOAuthServer.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SpecmaticProvider;

/// <summary>
/// Simple mock OAuth 2.0 token server for testing
/// Generates valid JWT tokens with specified scopes
/// </summary>
public class MockOAuthServer
{
    private readonly string _jwtSecret;
    private readonly string _issuer;

    public MockOAuthServer(string jwtSecret = "your-super-secret-key-min-32-characters-long", 
                          string issuer = "http://localhost:5001")
    {
        _jwtSecret = jwtSecret;
        _issuer = issuer;
    }

    /// <summary>
    /// Generate a valid JWT token with specified scopes
    /// </summary>
    public string GenerateToken(string clientId, string[] scopes, int expirationMinutes = 60)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("scope", string.Join(" ", scopes)),  // OAuth scopes
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: "api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Add OAuth Token Endpoint to Provider

**File: `Provider/src/Program.cs`** (add this mapping)

```csharp
// Add this before app.MapControllers()
app.MapPost("/oauth/token", (HttpContext context) =>
{
    // For testing purposes, accept any client_id/client_secret
    // In production, validate these properly
    
    var oauthServer = new MockOAuthServer();
    
    // Generate token with common scopes
    var token = oauthServer.GenerateToken(
        clientId: "test-client",
        scopes: new[] { "read:products", "write:products" },
        expirationMinutes: 60
    );

    return Results.Ok(new
    {
        access_token = token,
        token_type = "Bearer",
        expires_in = 3600
    });
});
```

---

## Method 4: Consumer Tests with OAuth

### Consumer Test Client with Token Handling

**File: `Consumer/src/ProductApiClientOAuth.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SpecmaticConsumer.Client;

/// <summary>
/// HTTP client with OAuth token management for Product API
/// </summary>
public class ProductApiClientOAuth
{
    private readonly HttpClient _client;
    private readonly string _oauthTokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private string _cachedToken;
    private DateTime _tokenExpiresAt;

    public ProductApiClientOAuth(
        string baseUrl = "http://localhost:5000",
        string oauthTokenUrl = "http://localhost:5001/oauth/token",
        string clientId = "test-client",
        string clientSecret = "test-secret")
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _oauthTokenUrl = oauthTokenUrl;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _cachedToken = null;
        _tokenExpiresAt = DateTime.MinValue;
    }

    /// <summary>
    /// Get a valid OAuth token, refreshing if necessary
    /// </summary>
    private async Task<string> GetValidTokenAsync()
    {
        // If cached token is still valid (with 5 min buffer), use it
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow.AddMinutes(5) < _tokenExpiresAt)
        {
            return _cachedToken;
        }

        // Request new token from OAuth server
        var tokenRequest = new
        {
            grant_type = "client_credentials",
            client_id = _clientId,
            client_secret = _clientSecret,
            scope = "read:products write:products"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(tokenRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync(_oauthTokenUrl, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        _cachedToken = tokenResponse.GetProperty("access_token").GetString();
        
        // Parse JWT to get expiration
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(_cachedToken) as JwtSecurityToken;
        _tokenExpiresAt = jwtToken?.ValidTo ?? DateTime.UtcNow.AddHours(1);

        return _cachedToken;
    }

    /// <summary>
    /// Add OAuth token to request headers
    /// </summary>
    private async Task SetAuthorizationHeaderAsync()
    {
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }

    // API Methods - same as before, but with authorization

    public async Task<string> GetHealthAsync()
    {
        // Health check doesn't need auth
        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        return doc.GetProperty("status").GetString();
    }

    public async Task<List<Product>> GetAllProductsAsync(int skip = 0, int take = 10)
    {
        // Ensure we have a valid token before making request
        await SetAuthorizationHeaderAsync();

        var response = await _client.GetAsync($"/api/products?skip={skip}&take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        await SetAuthorizationHeaderAsync();

        var response = await _client.GetAsync($"/api/products/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Product {id} not found");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Product>(json);
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        await SetAuthorizationHeaderAsync();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/products", content);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new HttpRequestException("Unauthorized - invalid or missing token");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Product>(json);
    }

    public async Task<Product> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        await SetAuthorizationHeaderAsync();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PutAsync($"/api/products/{id}", content);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Product {id} not found");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Product>(json);
    }

    public async Task DeleteProductAsync(int id)
    {
        await SetAuthorizationHeaderAsync();

        var response = await _client.DeleteAsync($"/api/products/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Product {id} not found");

        response.EnsureSuccessStatusCode();
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### OAuth Tests

**File: `Consumer/tests/ProductApiOAuthTests.cs`**

```csharp
using FluentAssertions;
using SpecmaticConsumer.Client;
using Xunit;

namespace SpecmaticConsumer.Tests;

/// <summary>
/// Contract tests for OAuth-secured Product API
/// Tests validate that the API properly enforces OAuth authentication
/// </summary>
public class ProductApiOAuthTests : IAsyncLifetime
{
    private readonly ProductApiClientOAuth _authorizedClient;
    private readonly HttpClient _unauthorizedClient;

    public ProductApiOAuthTests()
    {
        // Client with OAuth token (authorized)
        _authorizedClient = new ProductApiClientOAuth(
            baseUrl: "http://localhost:5000",
            oauthTokenUrl: "http://localhost:5001/oauth/token",
            clientId: "test-client",
            clientSecret: "test-secret"
        );

        // Client without OAuth token (unauthorized)
        _unauthorizedClient = new HttpClient 
        { 
            BaseAddress = new Uri("http://localhost:5000") 
        };
    }

    public async Task InitializeAsync()
    {
        // Wait for API to be ready
        var maxRetries = 30;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                var health = await _authorizedClient.GetHealthAsync();
                if (health == "ok")
                    return;
            }
            catch
            {
                retryCount++;
                await Task.Delay(500);
            }
        }

        throw new TimeoutException("API did not become available within 15 seconds");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Health Check Tests

    [Fact]
    public async Task GetHealth_WithoutToken_ShouldReturn_OkStatus()
    {
        // Act - Health check doesn't require authentication
        var response = await _unauthorizedClient.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    #endregion

    #region Authorized Tests (With Valid Token)

    [Fact]
    public async Task GetAllProducts_WithValidToken_ShouldReturn_Products()
    {
        // Act
        var products = await _authorizedClient.GetAllProductsAsync();

        // Assert
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithValidToken_ShouldReturn_CreatedProduct()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "OAuth Test Product",
            Price = 99.99m
        };

        // Act
        var product = await _authorizedClient.CreateProductAsync(request);

        // Assert
        product.Id.Should().BeGreaterThan(0);
        product.Name.Should().Be("OAuth Test Product");
    }

    #endregion

    #region Unauthorized Tests (Without Token)

    [Fact]
    public async Task GetAllProducts_WithoutToken_ShouldReturn_Unauthorized()
    {
        // Act
        var response = await _unauthorizedClient.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithoutToken_ShouldReturn_Unauthorized()
    {
        // Arrange
        var request = new CreateProductRequest 
        { 
            Name = "Test", 
            Price = 50m 
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await _unauthorizedClient.PostAsync("/api/products", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token Management Tests

    [Fact]
    public async Task GetProducts_WithExpiredToken_ShouldRefresh_AndRetry()
    {
        // This tests that the client properly manages token expiration
        // and refreshes when needed

        // Act - Make multiple requests
        var products1 = await _authorizedClient.GetAllProductsAsync();
        await Task.Delay(1000);  // Small delay
        var products2 = await _authorizedClient.GetAllProductsAsync();

        // Assert - Both should succeed (token was refreshed)
        products1.Should().NotBeNull();
        products2.Should().NotBeNull();
    }

    #endregion
}
```

---

## Method 5: Specmatic Configuration for OAuth

### Specmatic Config File

**File: `specmatic-oauth.yaml`**

```yaml
# Specmatic configuration for OAuth-secured API

targetPort: 5000
oauthPort: 5001  # Port for mock OAuth server

specificationPath: specs/products-api-oauth.yaml

# OAuth Configuration
oauth:
  tokenUrl: http://localhost:5001/oauth/token
  clientId: test-client
  clientSecret: test-secret
  scopes:
    - read:products
    - write:products

# Test Configuration
test:
  # Skip endpoints that don't require auth
  skipUnauthenticated:
    - /api/health

  # Require OAuth for these endpoints
  requireOAuth:
    - GET /api/products
    - POST /api/products
    - GET /api/products/{id}
    - PUT /api/products/{id}
    - DELETE /api/products/{id}
```

---

## How Specmatic Tests OAuth APIs

### Workflow

```
1. Specmatic reads OAuth config from spec
   ↓
2. Before testing protected endpoints:
   → Request token from OAuth server
   → Get Bearer token
   ↓
3. Inject token into request headers:
   Authorization: Bearer <token>
   ↓
4. Send request to API endpoint
   ↓
5. Validate response matches spec
   ↓
6. Test negative cases:
   → Missing token → expect 401
   → Invalid token → expect 401
   → Expired token → expect 401
   ↓
7. Report results
```

### Specmatic Auto-Generated Test Scenarios for OAuth

When Specmatic processes the OAuth-secured API spec, it auto-generates:

```
✅ Happy Path Tests
   • GET /api/products with valid token → 200
   • POST /api/products with valid token → 201
   • PUT /api/products/{id} with valid token → 200
   • DELETE /api/products/{id} with valid token → 204

✅ Negative Tests (Security)
   • GET /api/products without token → 401
   • GET /api/products with invalid token → 401
   • GET /api/products with expired token → 401
   • POST /api/products without scope write:products → 403

✅ Edge Cases
   • Missing Authorization header → 401
   • Malformed Bearer token → 401
   • Wrong authorization scheme → 401

✅ Anonymous Endpoint Tests
   • GET /api/health without token → 200
   • GET /api/health with token → 200 (still works)
```

---

## Running Tests with Specmatic and OAuth

### Command Line

```bash
# Run with OAuth config
specmatic test \
  --spec specs/products-api-oauth.yaml \
  --provider-port 5000 \
  --oauth-port 5001

# Or with config file
specmatic test --config specmatic-oauth.yaml
```

### Expected Output

```
Testing Secure Product API with OAuth
====================================

Setting up Mock OAuth Server on :5001
Requesting token from http://localhost:5001/oauth/token
✓ Token acquired: eyJhbGciOiJIUzI1NiIs...

Running Contract Tests:
========================

Health Check (No Auth Required)
  GET /api/health without token .................... ✓

Products (Read Scope Required)
  GET /api/products with valid token ............. ✓
  GET /api/products without token ................. ✓ (401 as expected)
  GET /api/products/{id} with valid token ........ ✓
  GET /api/products/{id} without token ............ ✓ (401 as expected)

Create Product (Write Scope Required)
  POST /api/products with valid token ............ ✓
  POST /api/products without token ............... ✓ (401 as expected)
  POST /api/products with read-only token ........ ✓ (403 as expected)

Update Product (Write Scope Required)
  PUT /api/products/{id} with valid token ........ ✓
  PUT /api/products/{id} without token ........... ✓ (401 as expected)

Delete Product (Write Scope Required)
  DELETE /api/products/{id} with valid token ..... ✓
  DELETE /api/products/{id} without token ........ ✓ (401 as expected)

════════════════════════════════════════════════════════════
✓ All tests passed
Total: 20 tests
Passed: 20
Failed: 0
Time: 2.3 seconds
```

---

## Key Takeaways: OAuth + Specmatic

### What Specmatic Does

✅ **Automatic token acquisition** - Gets tokens from OAuth server  
✅ **Token injection** - Adds tokens to request headers automatically  
✅ **Scope validation** - Tests that endpoints require correct OAuth scopes  
✅ **Security testing** - Auto-generates tests for 401/403 scenarios  
✅ **Token expiration** - Tests token refresh scenarios  
✅ **Negative testing** - Validates security by testing without tokens  

### What You Need to Do

1. **Define OAuth in OpenAPI spec** - Use `securitySchemes` and `security` blocks
2. **Implement OAuth on Provider** - Add `[Authorize]` attributes and JWT validation
3. **Create Mock OAuth Server** - For testing (or use real one)
4. **Configure Specmatic** - Point to token URL and provide credentials
5. **Run Specmatic** - It handles token management automatically

### Architecture Flow

```
┌──────────────────────────────────────────┐
│     OpenAPI Spec with OAuth Definition   │
│     (securitySchemes + security blocks)  │
└────────────────────┬─────────────────────┘
                     │
                     ▼
         ┌───────────────────────┐
         │   Specmatic CLI       │
         │ (reads OAuth config)  │
         └────────┬──────────────┘
                  │
        ┌─────────┴──────────┐
        ▼                    ▼
  ┌──────────────┐    ┌──────────────────┐
  │ Mock OAuth   │    │ Provider API     │
  │ Server :5001 │    │ (ASP.NET) :5000  │
  │ (generates   │    │ (secured with    │
  │  tokens)     │    │  OAuth)          │
  └──────────────┘    └──────────────────┘
        ↑                     │
        └─ Token request      │
          (2. Get token)      │
                    ┌─────────┘
                    │
                3. Inject token into header
                Authorization: Bearer <token>
                    │
                    ▼
              4. Send request
              5. Validate response
              6. Report results
```

---

## Summary Table

| Aspect | How It Works |
|--------|-------------|
| **OAuth Definition** | In OpenAPI spec using `securitySchemes` |
| **Token Acquisition** | Specmatic requests from OAuth token endpoint |
| **Token Injection** | Automatically added to request headers |
| **Scope Validation** | Specmatic verifies endpoints require correct scopes |
| **Negative Tests** | Auto-generated: no token, invalid token, wrong scope |
| **Token Expiration** | Specmatic handles refresh automatically |
| **Mock OAuth** | Can use mock server or real OAuth provider |
| **CI/CD Integration** | Works seamlessly with CI pipelines |

---

## Comparison: Manual vs Specmatic OAuth Testing

| Aspect | Manual (xUnit) | Specmatic |
|--------|---|---|
| **Token Generation** | Manual code | Automatic |
| **Token Injection** | Manual header setup | Automatic |
| **Scope Testing** | Write tests for each scope | Auto-generated |
| **401/403 Testing** | Manual test cases | Auto-generated |
| **Token Expiration** | Manual implementation | Automatic |
| **Maintenance** | Manual updates | Spec-driven |
| **Coverage** | What you write | Comprehensive |

