# Specmatic Contract Testing Example

## What is Specmatic?

**Specmatic** is an API contract testing tool that validates that both consumer and provider applications satisfy the contract defined in an OpenAPI specification. It's based on the concept that the **specification is the contract**.

### Key Features
- **Specification-Driven**: Uses OpenAPI/Swagger as the single source of truth
- **Automatic Testing**: Generates test cases automatically from the spec
- **Mock Server**: Built-in mock server for testing consumers
- **Multi-Language Support**: Works with any language/framework
- **CI/CD Integration**: Easy to integrate into pipelines
- **Comprehensive Reporting**: Detailed test reports and coverage analysis

### How It Works

```
┌─────────────────────────────────────────────┐
│     OpenAPI Specification (Contract)        │
│         (products-api.yaml)                 │
└──────────────────┬──────────────────────────┘
                   │
       ┌───────────┴───────────┐
       ▼                       ▼
   ┌─────────┐          ┌──────────┐
   │ Consumer│          │ Provider │
   │ Tests   │          │ Tests    │
   │ Validate│          │ Validate │
   │ Requests│          │Responses │
   └─────────┘          └──────────┘
       │                       │
       ▼                       ▼
   [Mock Server]          [Real Server]
      :9000                  :8080
```

## Project Structure

```
specmatic-contract-testing/
├── specs/
│   └── products-api.yaml          # OpenAPI contract specification
├── provider/
│   ├── src/
│   │   └── index.js               # Express.js API server
│   └── package.json
├── consumer/
│   ├── src/
│   │   ├── api-client.js          # API client implementation
│   │   └── contract.test.js       # Contract tests
│   ├── package.json
│   └── jest.config.js
├── specmatic.yaml                 # Specmatic configuration
└── README.md                       # This file
```

## Getting Started

### Prerequisites
- Node.js 16+ installed
- npm or yarn package manager

### Step 1: Install Dependencies

**Provider:**
```bash
cd provider
npm install
```

**Consumer:**
```bash
cd consumer
npm install
```

### Step 2: Start the Provider

In one terminal:
```bash
cd provider
npm start
```

You should see:
```
Provider API running at http://localhost:8080
OpenAPI Spec available at http://localhost:8080/api-docs
```

### Step 3: Run Consumer Contract Tests

In another terminal:
```bash
cd consumer
npm test
```

Expected output:
```
PASS  src/contract.test.js
  Product API Consumer Contract Tests
    GET /api/products
      ✓ should return an array of products
      ✓ should return products with required fields
      ✓ should return products with correct data types
      ✓ should return products with valid price values
    GET /api/products/{id}
      ✓ should return a single product when given valid ID
      ✓ should return product with correct schema
      ✓ should throw error when product not found
    POST /api/products
      ✓ should create a new product with valid data
      ✓ should return product with all required fields
      ✓ should reject product without name
      ✓ should reject product with invalid price
    PUT /api/products/{id}
      ✓ should update product with valid data
      ✓ should return updated product with all fields
      ✓ should throw error when updating non-existent product
    DELETE /api/products/{id}
      ✓ should delete an existing product
      ✓ should throw error when deleting non-existent product

Tests: 16 passed, 16 total
```

## API Endpoints

### GET /api/products
Returns all products.

**Response (200):**
```json
[
  {
    "id": 1,
    "name": "Widget",
    "description": "A useful widget",
    "price": 19.99,
    "inStock": true
  }
]
```

### POST /api/products
Creates a new product.

**Request:**
```json
{
  "name": "New Widget",
  "description": "A brand new widget",
  "price": 24.99
}
```

**Response (201):**
```json
{
  "id": 3,
  "name": "New Widget",
  "description": "A brand new widget",
  "price": 24.99,
  "inStock": true
}
```

### GET /api/products/{id}
Retrieves a specific product by ID.

**Response (200):**
```json
{
  "id": 1,
  "name": "Widget",
  "description": "A useful widget",
  "price": 19.99,
  "inStock": true
}
```

**Response (404):**
```json
{
  "code": "NOT_FOUND",
  "message": "Product with id 999 not found"
}
```

### PUT /api/products/{id}
Updates a product.

**Request:**
```json
{
  "name": "Updated Widget",
  "price": 22.99
}
```

**Response (200):**
```json
{
  "id": 1,
  "name": "Updated Widget",
  "description": "A useful widget",
  "price": 22.99,
  "inStock": true
}
```

### DELETE /api/products/{id}
Deletes a product.

**Response (204):** No content

## Understanding the Contract

The contract is defined in `specs/products-api.yaml` using OpenAPI 3.0.0 specification.

Key contract elements:

1. **Paths**: Define available endpoints
2. **Operations**: Define HTTP methods (GET, POST, PUT, DELETE)
3. **Parameters**: Define query/path parameters with validation
4. **Request/Response Schemas**: Define data structure and types
5. **Status Codes**: Define expected HTTP status codes
6. **Error Responses**: Define error response format

Example from the spec:

```yaml
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
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'
        '404':
          description: Product not found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
```

## Testing Workflow

### Consumer-Side Testing

The consumer validates that:
1. ✅ Request format matches the contract
2. ✅ Response status codes are as expected
3. ✅ Response schemas match the contract
4. ✅ Required fields are present
5. ✅ Data types are correct
6. ✅ Error handling works

### Provider-Side Testing

The provider must ensure:
1. ✅ Accepts valid requests per the spec
2. ✅ Returns correct status codes
3. ✅ Response bodies match the schema
4. ✅ Validates input according to spec
5. ✅ Rejects invalid requests properly

## Running with Specmatic CLI

To use Specmatic directly for contract testing:

```bash
# Install Specmatic CLI
npm install -g specmatic

# Run tests against the provider
specmatic test --spec specs/products-api.yaml --baseurl http://localhost:8080

# Generate mock server
specmatic stub --spec specs/products-api.yaml

# Generate API documentation
specmatic docs --spec specs/products-api.yaml
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
name: Contract Tests

on: [push, pull_request]

jobs:
  contract-tests:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup Node.js
        uses: actions/setup-node@v2
        with:
          node-version: '18'
      
      - name: Install Provider Dependencies
        run: cd provider && npm install
      
      - name: Start Provider
        run: cd provider && npm start &
      
      - name: Install Consumer Dependencies
        run: cd consumer && npm install
      
      - name: Run Consumer Contract Tests
        run: cd consumer && npm test
      
      - name: Run Specmatic Tests
        run: |
          npm install -g specmatic
          specmatic test --spec specs/products-api.yaml --baseurl http://localhost:8080
```

## Contract Changes & Versioning

When the API contract changes:

1. **Update the OpenAPI spec** (specs/products-api.yaml)
2. **Update the provider** to implement the new contract
3. **Update the consumer** to use the new contract
4. **Run all tests** to ensure compatibility
5. **Update documentation** to reflect changes

### Breaking Changes

If a breaking change is introduced:

```yaml
# Old contract
paths:
  /api/products:
    get:
      responses:
        '200':
          schema:
            type: array
            items:
              $ref: '#/components/schemas/ProductV1'

# New contract (breaking)
paths:
  /api/products:
    get:
      responses:
        '200':
          schema:
            type: array
            items:
              $ref: '#/components/schemas/ProductV2'
```

**Solution**: 
- Create versioned endpoints (/api/v1/products, /api/v2/products)
- Support both versions during transition period
- Update contracts with version numbers
- Provide migration guide for consumers

## Debugging Failed Tests

### Issue: Consumer Test Fails

**Problem**: `Product with id X not found`

**Solution**:
1. Verify provider is running: `curl http://localhost:8080/health`
2. Check test data exists
3. Review API response: `curl http://localhost:8080/api/products`
4. Check error logs in provider

### Issue: Schema Validation Fails

**Problem**: Response doesn't match schema

**Solution**:
1. Check actual response: `curl http://localhost:8080/api/products/1`
2. Verify schema in `specs/products-api.yaml`
3. Ensure response includes all required fields
4. Check data types match schema definition

### Issue: Port Already in Use

**Problem**: `EADDRINUSE: address already in use :::8080`

**Solution**:
```bash
# Find process using port 8080
netstat -ano | findstr :8080  # Windows
lsof -i :8080                  # Mac/Linux

# Kill the process
taskkill /PID <PID> /F         # Windows
kill -9 <PID>                  # Mac/Linux
```

## Best Practices

### 1. Keep Specification Current
- Update spec before implementing changes
- Use spec as documentation
- Review spec changes in pull requests

### 2. Comprehensive Testing
- Test happy paths and error cases
- Validate all required fields
- Check data type conversions
- Test boundary values

### 3. Contract Evolution
- Avoid breaking changes when possible
- Use versioning for large changes
- Provide backward compatibility
- Document migration paths

### 4. CI/CD Integration
- Run contract tests on every commit
- Fail the build if tests fail
- Generate reports for visibility
- Track coverage over time

### 5. Documentation
- Maintain OpenAPI spec as documentation
- Keep README updated
- Document API behavior
- Provide examples

## Key Differences: Specmatic vs Pact vs Postman

| Feature | Specmatic | Pact | Postman |
|---------|-----------|------|---------|
| **Approach** | Spec-driven | Consumer-driven | Collection-based |
| **Source of Truth** | OpenAPI spec | Pact file | Postman collection |
| **Mock Server** | ✅ Built-in | ✅ Built-in | ⚠️ Postman cloud |
| **Auto Test Generation** | ✅ From spec | ❌ Manual | ❌ Manual |
| **Multi-consumer** | ✅ Single spec | ⚠️ Per consumer | ⚠️ Shared collection |
| **Documentation** | ✅ Spec IS docs | ❌ Separate | ⚠️ In collection |
| **Language Support** | ✅ Any language | ✅ Multi-language | ✅ Any HTTP API |
| **Learning Curve** | Medium | Steep | Low |

## When to Use Specmatic

✅ **Best For**:
- API-first development with OpenAPI specs
- Multi-consumer APIs with same contract
- Need comprehensive API documentation
- Want automatic test generation
- Prefer specification as source of truth
- Building REST APIs with strict contracts

❌ **Not Ideal For**:
- GraphQL APIs (limited support)
- Legacy APIs without specs
- Highly dynamic APIs
- Simple integrations

## Resources

- **Specmatic Documentation**: https://specmatic.io
- **OpenAPI Specification**: https://spec.openapis.org
- **OpenAPI Guide**: https://swagger.io/resources/articles/best-practices-in-api-design/
- **Express.js**: https://expressjs.com
- **Jest**: https://jestjs.io

## Next Steps

1. ✅ Run the example locally
2. ✅ Modify the API spec and see tests fail
3. ✅ Update provider to implement new spec
4. ✅ Re-run tests to verify
5. ✅ Integrate into your CI/CD pipeline
6. ✅ Scale to multiple services

## Support

For questions or issues:
1. Check Specmatic documentation: https://specmatic.io/documentation
2. Review OpenAPI best practices
3. Check test output for specific errors
4. Refer to troubleshooting section above

---

**Happy Contract Testing! 🎉**
