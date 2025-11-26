# Specmatic Architecture & Concepts

## What is Specmatic?

Specmatic is an **Open Source API contract testing tool** that validates APIs against their specifications. The core philosophy is:

> **The Specification IS the Contract**

Unlike Pact (consumer-driven) or manual testing, Specmatic:
- Uses OpenAPI specs as the authoritative contract
- Automatically generates test scenarios from the spec
- Tests both provider and consumer against the same spec
- Provides mock servers that conform to the spec
- Generates comprehensive documentation

## Architecture Overview

```
┌───────────────────────────────────────────────────────────────┐
│                   OpenAPI Specification                       │
│                   (products-api.yaml)                         │
│         - Paths, Methods, Parameters                         │
│         - Request/Response Schemas                           │
│         - Status Codes, Error Definitions                    │
└─────────────────────┬───────────────────────────────────────┘
                      │
        ┌─────────────┴─────────────┐
        │                           │
        ▼                           ▼
┌──────────────────┐       ┌──────────────────┐
│  Specmatic Test  │       │ Mock Server      │
│  Generator       │       │ (Prism/Stub)     │
│                  │       │                  │
│ Creates:         │       │ Serves:          │
│ • Request tests  │       │ • Valid responses│
│ • Response tests │       │ • Error responses│
│ • Edge cases     │       │ • Dynamic data   │
└────────┬─────────┘       └────────┬─────────┘
         │                         │
         ▼                         ▼
┌─────────────────────────────────────────────┐
│         Contract Validation Tests           │
│                                             │
│  Provider Tests:                           │
│  • Does API match the spec?                │
│  • Are responses correct?                  │
│  • Is error handling correct?              │
│                                             │
│  Consumer Tests:                           │
│  • Do requests follow the spec?            │
│  • Can we parse responses?                 │
│  • Do we handle errors correctly?          │
└─────────────────────────────────────────────┘
```

## Key Concepts

### 1. Contract
The **contract** is the OpenAPI specification file. It defines:
- What endpoints exist
- What parameters they accept
- What responses they return
- What errors they can produce

### 2. Provider Verification
The provider must prove it implements the contract:
```
Provider reads spec → Generates test scenarios → 
Runs against provider → Validates responses match spec
```

### 3. Consumer Validation
The consumer must prove it uses the contract correctly:
```
Consumer reads spec → Generates request scenarios → 
Sends to mock/provider → Validates responses
```

### 4. Mock Server
A mock server implements the spec without the real business logic:
```
Mock Server reads spec → Creates endpoints matching spec → 
Returns sample responses per spec → Consumers can develop independently
```

## Comparison with Other Approaches

### Pact (Consumer-Driven)
```
Consumer writes test → 
Pact generates contract → 
Provider verifies contract
```

✅ Consumer controls the contract  
❌ Contract not documented separately  
❌ Hard to see full API surface  

### OpenAPI/Specmatic (Spec-Driven)
```
Team defines OpenAPI spec → 
Both consumer and provider implement → 
Both validate against spec
```

✅ Contract is documented  
✅ Clear API definition  
✅ Can generate code and docs  
❌ Spec can become outdated  

### Postman (Collection-Based)
```
Create collection → 
Add test scripts → 
Run with Newman
```

✅ Easy to get started  
✅ Visual interface  
❌ Not true contract testing  

## File Structure Explained

### OpenAPI Specification (`specs/products-api.yaml`)

```yaml
openapi: 3.0.0           # Spec version
info:                    # Metadata
  title: Product Service API
  version: 1.0.0

paths:                   # API endpoints
  /api/products:
    get:                 # HTTP method
      responses:
        '200':          # HTTP status code
          description: List of products
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'

components:
  schemas:               # Data structures
    Product:
      type: object
      required:
        - id
        - name
      properties:
        id:
          type: integer
        name:
          type: string
```

### Provider Implementation (`provider/src/index.js`)

The provider must:
1. Implement all paths defined in the spec
2. Accept parameters as specified
3. Return responses that match the schemas
4. Return correct status codes

```javascript
// Matches spec: GET /api/products → returns array of Products
app.get('/api/products', (req, res) => {
  res.json(products);  // Array[Product]
});

// Matches spec: POST /api/products → returns Product
app.post('/api/products', validateCreateRequest, (req, res) => {
  const newProduct = { id: nextId++, ...req.body, inStock: true };
  products.push(newProduct);
  res.status(201).json(newProduct);  // 201 status + Product schema
});
```

### Consumer Implementation (`consumer/src/api-client.js`)

The consumer must:
1. Format requests per the spec
2. Handle responses per the spec
3. Handle error statuses

```javascript
async getAllProducts() {
  // Matches spec: GET /api/products
  const response = await fetch(`${this.baseUrl}/api/products`);
  return response.json();  // Expect Array[Product]
}

async getProductById(id) {
  // Matches spec: GET /api/products/{id}
  const response = await fetch(`${this.baseUrl}/api/products/${id}`);
  if (response.status === 404) {
    throw new Error(`Product not found`);  // Handle 404 per spec
  }
  return response.json();
}
```

### Tests (`consumer/src/contract.test.js`)

Tests validate the contract is implemented:

```javascript
// Test validates contract: GET /api/products returns Array[Product]
it('should return an array of products', async () => {
  const products = await client.getAllProducts();
  expect(Array.isArray(products)).toBe(true);
});

// Test validates contract: POST rejects invalid price
it('should reject product with invalid price', async () => {
  const invalidProduct = { name: 'Test', price: -10 };
  await expect(client.createProduct(invalidProduct)).rejects.toThrow();
});
```

## How Specmatic Works

### Test Scenario Generation

Specmatic analyzes the spec and generates test scenarios:

```yaml
# From spec:
parameters:
  - name: id
    in: path
    schema:
      type: integer
      minimum: 1

# Specmatic generates tests for:
✓ Valid ID: GET /api/products/1
✓ Invalid ID type: GET /api/products/abc
✓ Boundary: GET /api/products/0 (below minimum)
✓ Large value: GET /api/products/999999999
```

### Response Validation

For each response, Specmatic validates:

```
1. Status Code: Is it in the spec? (200, 201, 404, etc.)
2. Content-Type: Is it correct? (application/json)
3. Schema: Do fields match types? (id: number, name: string)
4. Required Fields: Are all required fields present?
5. Data Values: Are values valid? (price >= 0, string length, etc.)
```

## Testing Workflow

### Step 1: Define Contract
```yaml
# specs/products-api.yaml
paths:
  /api/products/{id}:
    get:
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      responses:
        '200':
          description: Product found
          schema:
            $ref: '#/components/schemas/Product'
        '404':
          description: Product not found
```

### Step 2: Provider Implements
```javascript
app.get('/api/products/:id', (req, res) => {
  const product = products.find(p => p.id === parseInt(req.params.id));
  if (!product) {
    return res.status(404).json({ error: 'Not found' });
  }
  res.json(product);
});
```

### Step 3: Specmatic Tests Provider
```
Test 1: GET /api/products/1
  → Expects status 200
  → Expects Product schema
  → ✓ PASS

Test 2: GET /api/products/9999
  → Expects status 404
  → ✓ PASS
```

### Step 4: Consumer Uses Contract
```javascript
const product = await client.getProductById(1);
// Expects: { id: 1, name: '...', price: ..., inStock: ... }
```

### Step 5: Specmatic Tests Consumer
```
Test: Does consumer send valid request?
  → GET /api/products/1 (valid ID)
  → ✓ PASS

Test: Does consumer handle 404?
  → GET /api/products/9999
  → Expects error handling
  → ✓ PASS
```

## Benefits of Spec-Driven Approach

| Benefit | Description |
|---------|-------------|
| **Single Source of Truth** | One spec, all teams refer to it |
| **Self-Documenting** | Spec IS the documentation |
| **Code Generation** | Generate SDKs, stubs, docs from spec |
| **Automatic Tests** | No manual test writing needed |
| **Multi-Consumer** | One spec serves many consumers |
| **Mock Servers** | Instant mock servers from spec |
| **Version Management** | Track spec versions over time |
| **API Governance** | Enforce consistency across APIs |

## Common Workflows

### Workflow 1: Spec-First Development
```
1. Design OpenAPI spec with team
2. Generate mock server
3. Consumer develops against mock
4. Provider implements spec
5. Run contract tests
6. Both teams deploy
```

### Workflow 2: Spec-as-Documentation
```
1. Implement API first
2. Write/generate OpenAPI spec from code
3. Use spec as documentation
4. Generate SDKs for consumers
5. Run contract tests for validation
```

### Workflow 3: Breaking Changes
```
1. Design v2 endpoint in spec
2. Support both v1 and v2
3. Run both contract tests
4. Migrate consumers gradually
5. Deprecate v1
6. Remove v1
```

## Real-World Scenario

### Salesforce Connector Example

**Problem**: 
- Salesforce releases SF 17.1, 18.1
- Each version has slightly different API
- Connector needs to support both

**Solution with Specmatic**:

```yaml
# specs/salesforce-api-v17.1.yaml
paths:
  /api/contacts:
    get:
      responses:
        '200':
          schema:
            $ref: '#/components/schemas/ContactList'

# specs/salesforce-api-v18.1.yaml
paths:
  /api/contacts:
    get:
      responses:
        '200':
          schema:
            $ref: '#/components/schemas/ContactListV2'
```

**Implementation**:

```javascript
// Provider supports both versions
app.get('/api/contacts', (req, res) => {
  const version = req.query.apiVersion || '1';
  if (version === '2') {
    res.json(getContactsV2());  // Matches SF 18.1 spec
  } else {
    res.json(getContactsV1());  // Matches SF 17.1 spec
  }
});
```

**Testing**:

```bash
# Test against v17.1 spec
specmatic test --spec specs/salesforce-api-v17.1.yaml

# Test against v18.1 spec
specmatic test --spec specs/salesforce-api-v18.1.yaml
```

## Advanced Features

### 1. Request/Response Matchers
```yaml
# Match ID pattern
parameters:
  - name: productId
    schema:
      type: string
      pattern: '^[0-9]{4}$'  # Only 4 digits
```

### 2. Conditional Schemas
```yaml
# Different schema based on type
properties:
  data:
    oneOf:
      - $ref: '#/components/schemas/Product'
      - $ref: '#/components/schemas/Service'
```

### 3. Provider States
```bash
# Test with different data states
specmatic test --spec products-api.yaml \
  --provider-state "product exists"
```

## Best Practices

1. **Keep Spec Updated**: Always update spec first
2. **Use Semantic Versioning**: v1.0.0, v1.1.0, v2.0.0
3. **Document Changes**: Explain breaking changes
4. **Test Systematically**: Cover all paths and methods
5. **Version Specs**: Keep old specs for compatibility
6. **Automate**: Integrate into CI/CD
7. **Review**: Have team review spec changes
8. **Communicate**: Notify consumers of changes

## Resources

- Specmatic Documentation: https://specmatic.io
- OpenAPI Specification: https://spec.openapis.org
- Swagger Documentation: https://swagger.io
- JSON Schema: https://json-schema.org

---

**Key Takeaway**: With Specmatic, the specification becomes the contract that both providers and consumers validate against, creating a single source of truth for API interactions.
