# Getting Started - Specmatic .NET Contract Testing

## What You're About to Learn

This is a **complete, working example** of API contract testing using Specmatic principles with .NET Core. By the end, you'll understand:

✅ What contract testing is  
✅ Why specs matter  
✅ How to test APIs with xUnit  
✅ How to use OpenAPI specifications  
✅ How to ensure provider-consumer alignment  

## The Story

Imagine you have:
- **Provider Team**: Building the REST API (ASP.NET Core)
- **Consumer Team**: Using the REST API (.NET Client)
- **Problem**: How do we ensure they stay aligned?

**Solution**: Contract Testing with Specifications

```
Before Contract Testing          With Contract Testing
├─ Provider changes API          ├─ Spec defines contract
├─ Consumer breaks              ├─ Provider implements spec
├─ Integration fails            ├─ Consumer uses spec
├─ Debug in production          ├─ Tests validate both
└─ Unhappy teams!               └─ Happy teams! ✓
```

## Quick Start (5 minutes)

### 1. Start the Provider API

```bash
cd Provider
dotnet restore
dotnet run
```

You'll see:
```
Now listening on http://localhost:5000
```

### 2. Run the Tests

```bash
cd Consumer
dotnet restore
dotnet test
```

Expected result:
```
19 test(s) were executed successfully!
All test suites passed!
```

**Success!** Your contract is validated. ✅

## What Just Happened?

```
1. Provider Started
   ├─ ASP.NET Core listening on http://localhost:5000
   ├─ Implements all endpoints from spec
   └─ Ready to serve requests

2. Tests Ran
   ├─ Connected to http://localhost:5000
   ├─ Made HTTP requests per spec
   ├─ Validated responses
   └─ 19 tests all passed ✓

3. Contract Validated
   ├─ Provider follows spec ✓
   ├─ Consumer can use API ✓
   ├─ No surprises ✓
   └─ Ready for production ✓
```

## Key Files Explained

### 1. **specs/products-api.yaml** - The Contract

This is the **specification** - the single source of truth:

```yaml
paths:
  /api/products:
    get:
      summary: Get all products
      responses:
        '200':
          schema:
            type: array
            items:
              $ref: '#/components/schemas/Product'
```

**Says**: "When you GET /api/products, you get a 200 with an array of Products"

### 2. **Provider/src/Program.cs** - The API

This is the **provider** - implements the spec:

```csharp
app.MapGet("/api/products", async (IProductRepository repo) =>
{
    var products = await repo.GetAllAsync();
    return Results.Ok(products);
});
```

**Does**: "When someone calls GET /api/products, return all products"

### 3. **Consumer/src/ProductApiClient.cs** - The Client

This is the **consumer** - uses the API per spec:

```csharp
public async Task<List<Product>> GetAllProductsAsync()
{
    var response = await _httpClient.GetAsync($"{_baseUrl}/api/products");
    var products = JsonSerializer.Deserialize<List<Product>>(await response.Content.ReadAsStringAsync());
    return products;
}
```

**Does**: "Call GET /api/products and return the list"

### 4. **Consumer/tests/ProductApiContractTests.cs** - The Tests

These are the **contract tests** - validate the contract:

```csharp
[Fact]
public async Task GetAllProducts_ShouldReturn_ArrayOfProducts()
{
    var products = await _client.GetAllProductsAsync();
    products.Should().BeOfType<List<Product>>();
}
```

**Does**: "Ensure the API returns an array of products per spec"

## Understanding the Flow

### Request → Provider → Response Flow

```
┌─ Consumer Makes Request
│  GET /api/products
│
├─ Provider Receives
│  app.MapGet("/api/products", ...)
│
├─ Provider Validates (per spec)
│  - Is the path valid? ✓
│  - Are parameters valid? ✓
│
├─ Provider Executes
│  - Get products from database
│  - Build response
│
├─ Provider Responds
│  - Status: 200 OK ✓
│  - Body: Array of Products ✓
│  - Format: JSON ✓
│
├─ Consumer Receives
│  - Parse JSON
│  - Map to List<Product>
│
└─ Test Validates
   - Did we get array? ✓
   - Are fields present? ✓
   - Are types correct? ✓
   - Spec satisfied! ✓
```

## The Three Layers

### Layer 1: Specification (Top)
```
specs/products-api.yaml
↓
"Here's what the API should be"
```

### Layer 2: Implementation (Middle)
```
Provider (API) ← Implements spec
Consumer (Client) ← Uses spec
↓
"Here's what we built"
```

### Layer 3: Validation (Bottom)
```
Tests
↓
"Does it match the spec?"
```

## Making Changes (Learning Exercise)

### Change 1: Add a Required Field

**Scenario**: Your team decides products need a "color" field.

**Step 1: Update Spec**
```yaml
# specs/products-api.yaml
schemas:
  Product:
    required: [id, name, price, color]  # ← Add color here
    properties:
      color:
        type: string
        enum: [Red, Blue, Green]
```

**Step 2: Update Provider**
```csharp
// Provider/src/Models.cs
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }  // ← Add this
}
```

**Step 3: Update Consumer**
```csharp
// Consumer/src/Models.cs
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }  // ← Add this
}
```

**Step 4: Run Tests**
```bash
cd Consumer
dotnet test
```

**Result**: ✓ Tests still pass (or tell you what's wrong)

### Change 2: Add Validation Rule

**Scenario**: Prices must be at least $9.99

**Step 1: Update Spec**
```yaml
properties:
  price:
    type: number
    minimum: 9.99  # ← Add minimum
```

**Step 2: Update Provider**
```csharp
if (request.Price < 9.99m)
    errors.Add(new { Field = "price", Issue = "Minimum is $9.99" });
```

**Step 3: Update Tests**
```csharp
[Fact]
public async Task CreateProduct_WithTooLowPrice_ShouldFail()
{
    var request = new CreateProductRequest { Price = 5m };
    await Assert.ThrowsAsync<HttpRequestException>(
        () => _client.CreateProductAsync(request));
}
```

**Step 4: Run Tests**
```bash
dotnet test
```

**Result**: ✓ Validation enforced end-to-end

## Common Questions

### Q: Why a separate spec file?
**A**: Single source of truth. Both teams refer to one place. If it's only in code, it drifts.

### Q: Can't we just test the code?
**A**: Yes, but you lose:
- Documentation (what's the contract?)
- Consumer alignment (what should they expect?)
- Automatic test generation (faster development)

### Q: When should I update the spec?
**A**: Before implementation. Spec first, code second.

### Q: What if provider and consumer disagree?
**A**: The spec decides. Both must follow it.

### Q: Do I need Docker?
**A**: No. You can run locally with `dotnet run`. Docker is optional for deployment.

## Next Steps

### 🎯 Phase 1: Understand (You are here)
- ✅ Run the example
- ✅ Read this file
- ✓ Explore QUICK-START.md

### 📚 Phase 2: Learn
- ✓ Read README.md (detailed overview)
- ✓ Read ARCHITECTURE.md (design patterns)
- ✓ Explore the code

### ✏️ Phase 3: Experiment
- ✓ Make a small change to the spec
- ✓ Update the provider
- ✓ Run tests to verify
- ✓ See it work end-to-end

### 🚀 Phase 4: Deploy
- ✓ Use Docker to containerize
- ✓ Add to your CI/CD pipeline
- ✓ Share with your team

## File Navigation

| File | Purpose | Read When |
|------|---------|-----------|
| **00-START-HERE.md** | Overview | First |
| **QUICK-START.md** | Setup guide | Second |
| **README.md** | Complete guide | Want details |
| **ARCHITECTURE.md** | Design patterns | Understanding design |
| **INDEX.md** | File reference | Need to find something |

## Real-World Scenario

**Your Situation:**
```
SF 17.1 (consumer) needs product data
         ↓
VAIS API (provider) provides it
         ↓
How do we ensure compatibility?
```

**With Contract Testing:**
```
1. SF 17.1 team: "We need: id, name, price"
   ↓
2. VAIS team: "OK, we'll provide exactly that"
   ↓
   (Spec agreement)
   ↓
3. VAIS implements: GET /api/products
   ↓
4. SF 17.1 implements: client.getProducts()
   ↓
5. Tests run: ✓ SF gets what it expects ✓ VAIS provides correctly
   ↓
6. Integration: No surprises ✓
```

## Tools You're Using

| Tool | Purpose | Why |
|------|---------|-----|
| **OpenAPI** | Spec format | Industry standard |
| **ASP.NET Core** | Provider framework | .NET standard |
| **xUnit** | Test framework | .NET standard |
| **FluentAssertions** | Assertions | Readable tests |
| **Docker** | Containerization | Reproducible environments |

## Success Criteria

By the end of this, you should:

- ✓ Understand why contract testing matters
- ✓ Know what specifications do
- ✓ Run a complete example end-to-end
- ✓ Understand test coverage
- ✓ See how provider & consumer align
- ✓ Be ready to implement in your own projects

## Summary

**Contract testing** = making sure provider and consumer agree on the API contract

**Specification** = the agreement (OpenAPI)

**Tests** = proof they follow it

**Result** = no surprises in production ✓

## Need Help?

### Provider won't start
```bash
# Make sure port 5000 is free
netstat -ano | findstr :5000
```

### Tests fail to connect
```bash
# Make sure provider is running
curl http://localhost:5000/api/health
```

### Tests give weird errors
```bash
# Rebuild everything
cd Provider && dotnet clean && dotnet build
cd Consumer && dotnet clean && dotnet build
```

## Ready?

```bash
# Start provider
cd Provider && dotnet run

# In another terminal: run tests
cd Consumer && dotnet test
```

**You got this!** ✓

---

**Next**: Read [QUICK-START.md](QUICK-START.md) for detailed setup instructions.
