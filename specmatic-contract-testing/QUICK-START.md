# Specmatic Contract Testing - Quick Start Guide

## 5-Minute Setup

### Prerequisites
```bash
# Check Node.js version
node --version  # Should be 16+
npm --version   # Should be 8+
```

### Step 1: Install & Start Provider (Terminal 1)
```bash
cd specmatic-contract-testing/provider
npm install
npm start
```

Wait for message:
```
Provider API running at http://localhost:8080
```

### Step 2: Run Consumer Tests (Terminal 2)
```bash
cd specmatic-contract-testing/consumer
npm install
npm test
```

Expected: **All 16 tests pass ✅**

## What Just Happened?

1. **Provider** started an Express.js API server at port 8080
2. **Consumer** ran 16 Jest tests validating the API contract
3. **Tests** checked:
   - Response status codes (200, 201, 204, 404)
   - Response schemas and required fields
   - Data types (string, number, boolean)
   - Error handling (invalid requests, missing resources)

## Testing the API Manually

### List all products:
```bash
curl http://localhost:8080/api/products
```

### Get a specific product:
```bash
curl http://localhost:8080/api/products/1
```

### Create a new product:
```bash
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"New Widget","price":29.99}'
```

### Update a product:
```bash
curl -X PUT http://localhost:8080/api/products/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Widget","price":24.99}'
```

### Delete a product:
```bash
curl -X DELETE http://localhost:8080/api/products/1
```

## Understanding the Contract

The **contract** is defined in `specs/products-api.yaml`

Key elements:
- **Paths**: `/api/products`, `/api/products/{id}`
- **Operations**: GET, POST, PUT, DELETE
- **Schemas**: Product, CreateProductRequest, ErrorResponse
- **Validations**: Required fields, data types, value ranges

## Key Files

| File | Purpose |
|------|---------|
| `specs/products-api.yaml` | OpenAPI contract specification |
| `provider/src/index.js` | Express.js API server |
| `consumer/src/api-client.js` | API client implementing the contract |
| `consumer/src/contract.test.js` | 16 contract validation tests |

## Modifying the Contract

### Example: Add a new field to Product

1. **Update the spec** (`specs/products-api.yaml`):
```yaml
components:
  schemas:
    Product:
      properties:
        category:  # NEW
          type: string
          description: Product category
```

2. **Update the provider** (`provider/src/index.js`):
```javascript
let products = [
  {
    id: 1,
    name: 'Widget',
    category: 'Tools',  // NEW
    // ...
  }
];
```

3. **Update the consumer** (`consumer/src/contract.test.js`):
```javascript
it('should return product with category field', async () => {
  const product = await client.getProductById(1);
  expect(product).toHaveProperty('category');
});
```

4. **Re-run tests**:
```bash
npm test
```

Tests should still pass ✅

## Advanced Topics

### Mock Server
Generate a mock server that conforms to the spec:
```bash
npm install -g specmatic
specmatic stub --spec specs/products-api.yaml
```

The mock will run on http://localhost:9000

### Continuous Integration
Add to your CI/CD pipeline:
```yaml
- name: Run Contract Tests
  run: |
    cd consumer
    npm install
    npm test
```

### Contract Versioning
Support multiple API versions:
```
specs/
  ├── products-api-v1.yaml
  └── products-api-v2.yaml

provider/src/
  ├── v1-routes.js
  └── v2-routes.js
```

## Troubleshooting

**Q: "Cannot find module" error**
```bash
npm install  # Re-run in the folder
```

**Q: "Port 8080 already in use"**
```bash
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# Mac/Linux
lsof -i :8080
kill -9 <PID>
```

**Q: Tests timeout**
```bash
# Verify provider is running
curl http://localhost:8080/health

# Check the error message for details
npm test -- --verbose
```

## Next Steps

1. ✅ Review `specs/products-api.yaml` to understand the contract
2. ✅ Modify the spec and update provider/consumer
3. ✅ Run tests to verify changes
4. ✅ Add new endpoints to the API
5. ✅ Write contract tests for new endpoints
6. ✅ Integrate into your project

## Resources

- OpenAPI Spec: `specs/products-api.yaml`
- Full Documentation: `README.md`
- Specmatic Docs: https://specmatic.io
- OpenAPI Docs: https://spec.openapis.org

---

**Need help?** Check the full README.md for detailed explanations.
