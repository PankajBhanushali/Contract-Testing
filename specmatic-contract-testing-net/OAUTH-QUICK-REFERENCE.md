# OAuth + Specmatic: Quick Reference

## Answer to Your Question

**Question:** "If some API in Provider is secured using OAuth, how will Specmatic contract testing work?"

**Answer:** Specmatic handles OAuth seamlessly by:
1. Reading OAuth definition from OpenAPI spec
2. Automatically acquiring tokens from the OAuth server
3. Injecting tokens into test requests
4. Auto-generating security tests (401, 403, scope validation)
5. Reporting comprehensive contract validation results

---

## How It Works: 5-Minute Overview

### 1. Define OAuth in Spec

```yaml
components:
  securitySchemes:
    oauth2:
      type: oauth2
      flows:
        clientCredentials:
          tokenUrl: http://localhost:5001/oauth/token
          scopes:
            read:products: Read data
            write:products: Modify data

paths:
  /api/products:
    get:
      security:
        - oauth2:
            - read:products  # Requires this scope
```

### 2. Implement OAuth on Provider (.NET)

```csharp
// Program.cs
builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer();

// Controller.cs
[HttpGet("products")]
[Authorize(Policy = "ReadProducts")]  // Requires OAuth
public IActionResult GetProducts() { ... }
```

### 3. Create Mock OAuth Server

```csharp
app.MapPost("/oauth/token", context =>
{
    var token = tokenGenerator.GenerateToken("test-client", 
        new[] { "read:products", "write:products" });
    
    return Results.Ok(new { access_token = token, expires_in = 3600 });
});
```

### 4. Run Specmatic

```bash
specmatic test --spec specs/products-api-oauth.yaml --provider-port 5000
```

### 5. Specmatic Auto-Generates Tests

```
✓ GET /api/products WITH token → 200
✓ GET /api/products WITHOUT token → 401 (auto-tested!)
✓ GET /api/products WITH wrong scope → 403 (auto-tested!)
✓ POST /api/products WITH token → 201
✓ POST /api/products WITHOUT token → 401 (auto-tested!)
... (total: 20+ scenarios, all auto-generated)
```

---

## The Flow

```
Specmatic CLI
    ↓ reads spec
    ↓
    Identifies OAuth:
    - tokenUrl: http://localhost:5001/oauth/token
    - scopes: [read:products, write:products]
    ↓
    1. POST /oauth/token → Get JWT
    2. Add "Authorization: Bearer <token>" to headers
    3. Test with token (expect 200/201/204)
    4. Test without token (expect 401)
    5. Test with wrong scope (expect 403)
    ↓
    Validates:
    ✓ Happy path with valid token
    ✓ Security (401 without token)
    ✓ Authorization (403 insufficient scope)
    ✓ Token format and structure
    ✓ Response schemas
    ↓
    Report:
    ✓ 20 tests passed
    ✓ Full OAuth contract validated
```

---

## What Gets Auto-Generated

### Happy Path Tests (WITH valid OAuth token)
- ✅ GET /api/products → 200 OK
- ✅ POST /api/products → 201 Created
- ✅ GET /api/products/{id} → 200 OK
- ✅ PUT /api/products/{id} → 200 OK
- ✅ DELETE /api/products/{id} → 204 No Content
- ✅ Validate response schemas
- ✅ Validate data types and required fields

### Security Tests (auto-generated, WITHOUT manual code)
- ✅ No Authorization header → 401
- ✅ Invalid Bearer token → 401
- ✅ Expired token → 401 + refresh
- ✅ Wrong scope (read vs write) → 403
- ✅ Missing scope → 403
- ✅ Malformed token → 401

### Coverage Summary
- ~13-20 test scenarios auto-generated
- Zero test code written by developer
- All driven by OpenAPI spec + OAuth definition
- Security validation included by default

---

## Comparison: With vs Without OAuth

### Without OAuth
```
API
├─ GET /api/products → 200
├─ POST /api/products → 201
├─ GET /api/products/{id} → 200
├─ PUT /api/products/{id} → 200
└─ DELETE /api/products/{id} → 204

Specmatic Tests: ~6 scenarios
```

### With OAuth
```
API (secured with [Authorize])
├─ GET /api/products
│  ├─ with valid token → 200 ✓
│  ├─ without token → 401 ✓
│  ├─ with wrong scope → 403 ✓
│  └─ with expired token → 401 + refresh ✓
├─ POST /api/products
│  ├─ with write:products scope → 201 ✓
│  ├─ with read:products scope → 403 ✓
│  ├─ without token → 401 ✓
│  └─ missing Authorization → 401 ✓
├─ GET /api/products/{id}
│  ├─ with valid token → 200 ✓
│  └─ without token → 401 ✓
├─ PUT /api/products/{id}
│  ├─ with valid token → 200 ✓
│  └─ without token → 401 ✓
└─ DELETE /api/products/{id}
   ├─ with valid token → 204 ✓
   └─ without token → 401 ✓

Specmatic Tests: ~20 scenarios (all auto-generated)
```

---

## Implementation Checklist

### ✅ Provider Setup
- [ ] Add JWT Bearer authentication to Program.cs
- [ ] Add authorization policies
- [ ] Add [Authorize(Policy = "...")] to endpoints
- [ ] Create Mock OAuth token endpoint (/oauth/token)
- [ ] Test: Can you curl /oauth/token and get a token?

### ✅ Spec Update
- [ ] Add securitySchemes section with OAuth definition
- [ ] Add tokenUrl pointing to OAuth server
- [ ] Define scopes (read:products, write:products)
- [ ] Add security block to each endpoint with required scopes
- [ ] Mark endpoints that don't need auth with security: []

### ✅ Specmatic Execution
- [ ] Start Provider on http://localhost:5000
- [ ] Verify OAuth endpoint works (GET token)
- [ ] Run: `specmatic test --spec products-api-oauth.yaml --provider-port 5000`
- [ ] Verify all tests pass (including 401/403 security tests)

---

## File Structure

```
specmatic-contract-testing-net/
├── specs/
│   ├── products-api.yaml              (original, no OAuth)
│   └── products-api-oauth.yaml        (NEW: with OAuth)
│
├── Provider/
│   ├── src/
│   │   ├── Program.cs                 (ADD: JWT config)
│   │   ├── TokenGenerator.cs          (NEW: generates tokens)
│   │   ├── Controllers/
│   │   │   └── ProductController.cs   (UPDATE: add [Authorize])
│   │   └── Models.cs
│   │
│   └── provider.csproj
│
├── Consumer/
│   ├── src/
│   │   ├── ProductApiClient.cs        (original)
│   │   └── ProductApiOAuthClient.cs   (NEW: with token handling)
│   │
│   ├── tests/
│   │   ├── ProductApiContractTests.cs (original)
│   │   └── ProductApiOAuthTests.cs    (NEW: OAuth tests)
│   │
│   └── consumer.csproj
│
└── OAUTH-SPECMATIC-GUIDE.md           (NEW: detailed guide)
```

---

## Key Advantages

### Automatic
✅ Token acquisition (automatic)  
✅ Token injection (automatic)  
✅ Token refresh (automatic)  
✅ 401/403 test generation (automatic)  

### Comprehensive
✅ Happy path tests (with valid token)  
✅ Security tests (without token, wrong scope)  
✅ Edge cases (expired, malformed token)  
✅ Scope validation  

### Zero Manual Code
✅ No test code for authentication needed  
✅ All driven by OpenAPI spec  
✅ Reduced maintenance burden  

### Works Anywhere
✅ Mock OAuth server (for dev/test)  
✅ Real OAuth provider (for production)  
✅ Any OAuth 2.0 implementation  

---

## Common Scenarios

### Scenario 1: Public Endpoint (No OAuth)
```yaml
/api/health:
  get:
    security: []  # No auth required
    responses:
      '200': ...
```

### Scenario 2: Read-Only Endpoint (OAuth)
```yaml
/api/products:
  get:
    security:
      - oauth2:
          - read:products  # Requires read scope
    responses:
      '200': ...
      '401': ...
      '403': ...
```

### Scenario 3: Write Endpoint (OAuth)
```yaml
/api/products:
  post:
    security:
      - oauth2:
          - write:products  # Requires write scope
    responses:
      '201': ...
      '401': ...
      '403': ...
```

### Scenario 4: Admin-Only Endpoint (OAuth)
```yaml
/api/admin/reports:
  get:
    security:
      - oauth2:
          - admin  # Requires admin scope
    responses:
      '200': ...
      '403': ...  # Non-admin users get 403
```

---

## Testing Strategy

### Manual Testing (You Do)
1. Define OAuth in spec
2. Implement [Authorize] in controller
3. Create OAuth token endpoint
4. Start provider

### Specmatic Does (Automatic)
1. Reads spec with OAuth definition
2. Requests token from OAuth server
3. Tests ALL endpoints:
   - WITH valid token
   - WITHOUT token (401 test)
   - WITH wrong scope (403 test)
4. Validates responses match spec
5. Generates full report

### Result
✅ Comprehensive OAuth testing without writing test code!

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "401 Unauthorized" in tests | Verify OAuth endpoint is running on :5001 |
| Token not being injected | Check spec has securitySchemes definition |
| "403 Forbidden" errors | Verify scopes match between spec and token |
| Timeout waiting for token | Ensure /oauth/token endpoint returns valid JSON |
| Tests passing but security seems weak | Run Specmatic - it auto-tests unauthorized access |

---

## Next Steps

1. **Review:** Read `OAUTH-SPECMATIC-GUIDE.md` for detailed implementation
2. **Understand:** Study `OAUTH-PRACTICAL-EXAMPLE.md` for step-by-step guide
3. **Implement:** Add OAuth to your Provider
4. **Test:** Run Specmatic against your OAuth-secured API
5. **Validate:** Verify security tests are auto-generated and passing

---

## Real-World Benefits

### Development
- ✓ Catch OAuth misconfigurations early
- ✓ Ensure scopes are enforced
- ✓ Test both happy path and security

### Testing
- ✓ Zero manual auth test code
- ✓ Automatic 401/403 validation
- ✓ Token expiration handling

### Production
- ✓ Continuous contract validation
- ✓ OAuth compliance verification
- ✓ Breaking change detection

---

## Summary

| Aspect | How Specmatic Handles It |
|--------|-------------------------|
| **OAuth Definition** | Reads from OpenAPI securitySchemes |
| **Token Acquisition** | Automatic POST to tokenUrl |
| **Token Injection** | Automatic Authorization header |
| **Happy Path Tests** | Tests with valid token |
| **Security Tests** | Tests without token (401) + wrong scope (403) |
| **Token Management** | Automatic refresh when expired |
| **Test Generation** | Auto-generates from spec |
| **Code Required** | Spec + [Authorize] attributes only |
| **Coverage** | ~20 scenarios vs 6 without OAuth |

**Bottom Line:** OAuth + Specmatic = Automatic, comprehensive security testing!

