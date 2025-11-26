# Practical Example: Adding OAuth to Product API

## Complete Step-by-Step Implementation

This guide shows how to add OAuth security to your existing Product API and test it with Specmatic.

---

## Current State (No OAuth)

**Before:**
```
Client → API (no authentication)
         All endpoints public
```

**After:**
```
Client → OAuth Server → Get Token → API (with [Authorize])
         1. Authenticate              Secured endpoints
         2. Get Bearer token
         3. Include in requests
```

---

## Step 1: Update OpenAPI Spec

### Add Security Definitions

**File: `specs/products-api-oauth.yaml`**

```yaml
openapi: 3.0.0
info:
  title: Secure Product Service API
  version: 2.0.0
  description: Product API with OAuth 2.0 security

servers:
  - url: http://localhost:5000
    description: Local development server

# IMPORTANT: Define OAuth security schemes
components:
  securitySchemes:
    # Define OAuth 2.0
    oauth2:
      type: oauth2
      description: "OAuth 2.0 Client Credentials flow for API security"
      flows:
        clientCredentials:
          tokenUrl: http://localhost:5001/oauth/token
          scopes:
            read:products: "Read products"
            write:products: "Create, update, delete products"
            admin: "Full administrator access"

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
        createdAt:
          type: string
          format: date-time
      required:
        - id
        - name
        - price

paths:
  # Health check - NO auth required
  /api/health:
    get:
      summary: Health check
      tags:
        - Health
      security: []  # Explicitly no auth required
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

  # Get all products - Requires read:products scope
  /api/products:
    get:
      summary: Get all products
      tags:
        - Products
      security:
        - oauth2:
            - read:products  # ← REQUIRES THIS SCOPE
      parameters:
        - name: skip
          in: query
          schema:
            type: integer
            default: 0
        - name: take
          in: query
          schema:
            type: integer
            default: 10
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
          description: Unauthorized - missing or invalid token
        '403':
          description: Forbidden - insufficient scopes

    # Create product - Requires write:products scope
    post:
      summary: Create a new product
      tags:
        - Products
      security:
        - oauth2:
            - write:products  # ← REQUIRES THIS SCOPE
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

  # Get by ID - Requires read:products scope
  /api/products/{id}:
    get:
      summary: Get product by ID
      tags:
        - Products
      security:
        - oauth2:
            - read:products
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      responses:
        '200':
          description: Product found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'
        '401':
          description: Unauthorized
        '404':
          description: Product not found

    # Update - Requires write:products scope
    put:
      summary: Update product
      tags:
        - Products
      security:
        - oauth2:
            - write:products
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
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
      responses:
        '200':
          description: Product updated
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'
        '401':
          description: Unauthorized
        '404':
          description: Product not found

    # Delete - Requires write:products scope
    delete:
      summary: Delete product
      tags:
        - Products
      security:
        - oauth2:
            - write:products
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      responses:
        '204':
          description: Product deleted
        '401':
          description: Unauthorized
        '404':
          description: Product not found
```

**Key Changes:**
- Added `securitySchemes` with OAuth 2.0 definition
- Each endpoint has `security` block specifying required scopes
- Health endpoint has `security: []` (no auth required)
- 401 responses for unauthorized
- 403 responses for insufficient scopes

---

## Step 2: Update Provider (.NET)

### 2A. Configure Authentication in Program.cs

**Add to `Provider/src/Program.cs`:**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// JWT Configuration
var jwtSecret = "your-super-secret-key-min-32-characters-for-testing-only";

// Configure JWT Bearer Authentication
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

// Configure Authorization Policies based on scopes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadProducts", policy =>
        policy.RequireClaim("scope", "read:products"));
    
    options.AddPolicy("WriteProducts", policy =>
        policy.RequireClaim("scope", "write:products"));
    
    options.AddPolicy("Admin", policy =>
        policy.RequireClaim("scope", "admin"));
});

var app = builder.Build();

// Use authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add OAuth token endpoint for testing
app.MapPost("/oauth/token", context =>
{
    var tokenGenerator = new TokenGenerator(jwtSecret);
    var token = tokenGenerator.GenerateToken(
        clientId: "test-client",
        scopes: new[] { "read:products", "write:products" }
    );

    return Results.Ok(new
    {
        access_token = token,
        token_type = "Bearer",
        expires_in = 3600,
        scope = "read:products write:products"
    });
});

app.Run("http://localhost:5000");
```

### 2B. Add Token Generator

**New File: `Provider/src/TokenGenerator.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SpecmaticProvider;

public class TokenGenerator
{
    private readonly string _jwtSecret;

    public TokenGenerator(string jwtSecret)
    {
        _jwtSecret = jwtSecret;
    }

    public string GenerateToken(string clientId, string[] scopes, int expirationMinutes = 60)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("scope", string.Join(" ", scopes)),
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost:5001",
            audience: "api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 2C. Add [Authorize] to Controller

**Update `Provider/src/Controllers/ProductController.cs`:**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpecmaticProvider.Controllers;

[ApiController]
[Route("api")]
public class ProductController : ControllerBase
{
    // Health - NO auth required
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    // GET all - Requires read:products scope
    [HttpGet("products")]
    [Authorize(Policy = "ReadProducts")]  // ← REQUIRES SCOPE
    public IActionResult GetProducts([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Mouse", Price = 29.99m, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "Keyboard", Price = 79.99m, CreatedAt = DateTime.UtcNow }
        };

        return Ok(products.Skip(skip).Take(take).ToList());
    }

    // POST - Requires write:products scope
    [HttpPost("products")]
    [Authorize(Policy = "WriteProducts")]  // ← REQUIRES SCOPE
    public IActionResult CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Id = new Random().Next(1000, 9999),
            Name = request.Name,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    [HttpGet("products/{id}")]
    [Authorize(Policy = "ReadProducts")]
    public IActionResult GetProductById(int id) => Ok(/* product */);

    [HttpPut("products/{id}")]
    [Authorize(Policy = "WriteProducts")]
    public IActionResult UpdateProduct(int id, [FromBody] UpdateProductRequest request) 
        => Ok(/* updated */);

    [HttpDelete("products/{id}")]
    [Authorize(Policy = "WriteProducts")]
    public IActionResult DeleteProduct(int id) => NoContent();
}

public class Product { /* ... */ }
public class CreateProductRequest { /* ... */ }
public class UpdateProductRequest { /* ... */ }
```

---

## Step 3: Run Specmatic Against OAuth API

### With Specmatic CLI

```bash
# Start the provider
cd Provider
dotnet run
# Runs on http://localhost:5000

# In another terminal, run Specmatic
specmatic test \
  --spec specs/products-api-oauth.yaml \
  --provider-port 5000
```

### Specmatic Workflow with Your API

```
1. Specmatic reads spec with OAuth definition
2. Identifies OAuth token URL: http://localhost:5001/oauth/token
3. Requests token: POST http://localhost:5001/oauth/token
4. Gets: {"access_token": "eyJ...", "expires_in": 3600}
5. For each endpoint:
   ✓ GET /api/health (no auth needed) → 200
   ✓ GET /api/products (with token) → 200
   ✓ GET /api/products (no token) → 401 (auto-tested!)
   ✓ POST /api/products (with token) → 201
   ✓ POST /api/products (no token) → 401 (auto-tested!)
   ... etc
```

### Expected Output

```
Testing Secure Product API with OAuth
=====================================

Token Server: http://localhost:5001/oauth/token
✓ Token acquired successfully
  - Scopes: read:products, write:products
  - Expires: 1 hour

Running Contract Tests:

GET /api/health (No Auth)
  ✓ GET /api/health without token ..................... PASS (200)

GET /api/products (Requires read:products)
  ✓ GET /api/products with valid token ............... PASS (200)
  ✓ GET /api/products without token .................. PASS (401)
  ✓ GET /api/products with wrong scope .............. PASS (403)

POST /api/products (Requires write:products)
  ✓ POST /api/products with valid token ............. PASS (201)
  ✓ POST /api/products without token ................ PASS (401)

PUT /api/products/{id} (Requires write:products)
  ✓ PUT /api/products/{id} with valid token ......... PASS (200)
  ✓ PUT /api/products/{id} without token ............ PASS (401)

DELETE /api/products/{id} (Requires write:products)
  ✓ DELETE /api/products/{id} with valid token ...... PASS (204)
  ✓ DELETE /api/products/{id} without token ......... PASS (401)

═════════════════════════════════════════════════════
✓ All 13 tests passed
Failed: 0
Time: 2.4 seconds
```

---

## Step 4: Update Consumer to Handle OAuth

### Consumer with OAuth Client

**File: `Consumer/src/ProductApiOAuthClient.cs`**

```csharp
using System.Net.Http.Headers;
using System.Text.Json;

namespace SpecmaticConsumer.Client;

public class ProductApiOAuthClient
{
    private readonly HttpClient _client;
    private readonly string _tokenUrl;
    private string _cachedToken;

    public ProductApiOAuthClient(
        string baseUrl = "http://localhost:5000",
        string tokenUrl = "http://localhost:5001/oauth/token")
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _tokenUrl = tokenUrl;
        _cachedToken = null;
    }

    private async Task EnsureTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken))
            return;

        // Request token from OAuth server
        var tokenClient = new HttpClient();
        var response = await tokenClient.PostAsync(_tokenUrl, 
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        _cachedToken = doc.GetProperty("access_token").GetString();

        // Add to default headers
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _cachedToken);
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        await EnsureTokenAsync();
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Product>>(json) ?? new();
    }

    // ... other methods
}
```

---

## What Specmatic Validates Automatically

### Happy Path Tests (WITH valid token)
✅ GET /api/products → 200 OK + correct schema  
✅ POST /api/products → 201 Created + product in response  
✅ GET /api/products/{id} → 200 OK  
✅ PUT /api/products/{id} → 200 OK  
✅ DELETE /api/products/{id} → 204 No Content  

### Security Tests (Auto-Generated)
✅ GET /api/products WITHOUT token → 401 Unauthorized  
✅ POST /api/products WITHOUT token → 401 Unauthorized  
✅ GET /api/products with wrong scope → 403 Forbidden  
✅ Missing Authorization header → 401  
✅ Invalid Bearer token → 401  

### No Manual Coding Required
- ✓ Specmatic auto-generates all OAuth tests
- ✓ Specmatic handles token acquisition
- ✓ Specmatic validates scopes
- ✓ You don't write test code for authentication!

---

## Summary: Before vs After

| Aspect | Before (No OAuth) | After (With OAuth) |
|--------|---|---|
| **Spec** | No security defined | Security schemes defined |
| **Provider** | No auth middleware | JWT Bearer authentication |
| **Endpoints** | Public | [Authorize] attributes |
| **Tests** | Basic contract tests | + OAuth security tests |
| **Specmatic Tests** | ~6 scenarios | ~13 scenarios (auth included) |
| **Token Management** | N/A | Automatic (Specmatic handles) |
| **401 Testing** | Must write manually | Auto-generated by Specmatic |

---

## Key Takeaway

**With Specmatic + OAuth:**
1. Define OAuth in OpenAPI spec
2. Implement [Authorize] in .NET controller
3. Create OAuth token endpoint
4. Run Specmatic
5. Specmatic auto-generates:
   - ✓ Happy path tests (with token)
   - ✓ Security tests (401/403)
   - ✓ Scope validation
   - ✓ Token management

**You don't write any OAuth test code!** Specmatic handles it all.

