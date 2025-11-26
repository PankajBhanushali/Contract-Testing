# Specmatic Examples & Use Cases

## Example 1: Adding a New Field to the API

### Scenario
You want to add a `category` field to products.

### Step 1: Update the OpenAPI Spec

File: `specs/products-api.yaml`

```yaml
components:
  schemas:
    Product:
      type: object
      required:
        - id
        - name
        - price
        - inStock
        - category  # NEW
      properties:
        id:
          type: integer
        name:
          type: string
        category:  # NEW FIELD
          type: string
          description: Product category (e.g., "Tools", "Gadgets")
          enum: ["Tools", "Gadgets", "Widgets", "Services"]
        price:
          type: number
        inStock:
          type: boolean
```

### Step 2: Update Provider

File: `provider/src/index.js`

```javascript
// Add category to products
let products = [
  {
    id: 1,
    name: 'Widget',
    description: 'A useful widget',
    category: 'Widgets',  // NEW
    price: 19.99,
    inStock: true
  }
];

// Include category in POST
app.post('/api/products', validateCreateRequest, (req, res) => {
  const { name, description, price, category } = req.body;
  
  const newProduct = {
    id: nextId++,
    name: name.trim(),
    description: description || null,
    category: category || 'Uncategorized',  // NEW
    price: parseFloat(price),
    inStock: true
  };
  
  products.push(newProduct);
  res.status(201).json(newProduct);
});
```

### Step 3: Update Consumer

File: `consumer/src/contract.test.js`

```javascript
// Add test for new category field
it('should return product with category field', async () => {
  const product = await client.getProductById(1);
  expect(product).toHaveProperty('category');
  expect(typeof product.category).toBe('string');
});

// Test category validation
it('should accept valid category values', async () => {
  const newProduct = {
    name: 'New Tool',
    category: 'Tools',
    price: 29.99
  };
  
  const created = await client.createProduct(newProduct);
  expect(created.category).toBe('Tools');
});
```

### Step 4: Run Tests

```bash
# Provider still running
cd consumer
npm test

# All tests should pass, including new category tests
```

### What Happened?
✅ Spec updated first (source of truth)
✅ Provider implemented the change
✅ Consumer tests verify the change
✅ Contract tests ensure compatibility

---

## Example 2: API Versioning

### Scenario
Launch API v2 with breaking changes while supporting v1 for existing consumers.

### Structure
```
specs/
  ├── products-api-v1.yaml
  └── products-api-v2.yaml
```

### Version 1 Spec (Backward Compatible)
```yaml
# specs/products-api-v1.yaml
openapi: 3.0.0
info:
  title: Product Service API
  version: 1.0.0

paths:
  /api/v1/products:
    get:
      responses:
        '200':
          schema:
            type: array
            items:
              $ref: '#/components/schemas/ProductV1'

components:
  schemas:
    ProductV1:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
        price:
          type: number
```

### Version 2 Spec (New Features)
```yaml
# specs/products-api-v2.yaml
openapi: 3.0.0
info:
  title: Product Service API
  version: 2.0.0

paths:
  /api/v2/products:
    get:
      responses:
        '200':
          schema:
            type: array
            items:
              $ref: '#/components/schemas/ProductV2'

components:
  schemas:
    ProductV2:
      type: object
      required:
        - id
        - name
        - price
        - category        # NEW in v2
        - lastUpdated     # NEW in v2
      properties:
        id:
          type: integer
        name:
          type: string
        price:
          type: number
        category:
          type: string
        lastUpdated:
          type: string
          format: date-time
```

### Provider Supporting Both Versions
```javascript
// Support both v1 and v2 endpoints
app.get('/api/v1/products', (req, res) => {
  // Return v1 format (without new fields)
  const v1Products = products.map(p => ({
    id: p.id,
    name: p.name,
    price: p.price
  }));
  res.json(v1Products);
});

app.get('/api/v2/products', (req, res) => {
  // Return v2 format (with new fields)
  res.json(products);
});
```

### Consumer for v1
```javascript
async getProductsV1() {
  // Uses v1 endpoint and expects v1 schema
  const response = await fetch(`${this.baseUrl}/api/v1/products`);
  return response.json();
}
```

### Consumer for v2
```javascript
async getProductsV2() {
  // Uses v2 endpoint and expects v2 schema with new fields
  const response = await fetch(`${this.baseUrl}/api/v2/products`);
  return response.json();
}
```

### Testing Both Versions
```bash
# Test v1
specmatic test --spec specs/products-api-v1.yaml --baseurl http://localhost:8080

# Test v2
specmatic test --spec specs/products-api-v2.yaml --baseurl http://localhost:8080

# Both should pass!
```

---

## Example 3: Error Handling Contract

### Define Error Cases in Spec
```yaml
# specs/products-api.yaml
/api/products:
  post:
    responses:
      '201':
        description: Product created
        schema:
          $ref: '#/components/schemas/Product'
      '400':
        description: Invalid request
        schema:
          $ref: '#/components/schemas/ErrorResponse'
      '422':
        description: Validation error
        schema:
          $ref: '#/components/schemas/ValidationError'

components:
  schemas:
    ErrorResponse:
      type: object
      required:
        - code
        - message
      properties:
        code:
          type: string
        message:
          type: string
    
    ValidationError:
      type: object
      required:
        - code
        - message
        - errors
      properties:
        code:
          type: string
        message:
          type: string
        errors:
          type: array
          items:
            type: object
            properties:
              field:
                type: string
              reason:
                type: string
```

### Provider Error Handling
```javascript
app.post('/api/products', (req, res) => {
  // Validate name
  if (!req.body.name) {
    return res.status(400).json({
      code: 'MISSING_FIELD',
      message: 'Product name is required'
    });
  }

  // Validate price
  if (req.body.price < 0) {
    return res.status(422).json({
      code: 'VALIDATION_ERROR',
      message: 'Validation failed',
      errors: [
        {
          field: 'price',
          reason: 'Price must be non-negative'
        }
      ]
    });
  }

  // Create product
  const newProduct = { id: nextId++, ...req.body };
  products.push(newProduct);
  res.status(201).json(newProduct);
});
```

### Consumer Error Testing
```javascript
it('should handle 400 error when name missing', async () => {
  try {
    await client.createProduct({ price: 10 });
    fail('Should throw error');
  } catch (error) {
    expect(error.message).toContain('required');
  }
});

it('should handle 422 validation error', async () => {
  try {
    await client.createProduct({ name: 'Test', price: -10 });
    fail('Should throw error');
  } catch (error) {
    expect(error.message).toContain('Validation');
  }
});
```

---

## Example 4: Conditional Response Schema

### Scenario
Same endpoint returns different schema based on query parameter.

### Spec
```yaml
paths:
  /api/products:
    get:
      parameters:
        - name: detailed
          in: query
          schema:
            type: boolean
            default: false
      responses:
        '200':
          description: Products
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProductListResponse'

components:
  schemas:
    ProductListResponse:
      oneOf:
        - $ref: '#/components/schemas/SimpleProductList'
        - $ref: '#/components/schemas/DetailedProductList'
    
    SimpleProductList:
      type: array
      items:
        type: object
        properties:
          id:
            type: integer
          name:
            type: string
    
    DetailedProductList:
      type: array
      items:
        type: object
        properties:
          id:
            type: integer
          name:
            type: string
          description:
            type: string
          price:
            type: number
          inStock:
            type: boolean
```

### Provider
```javascript
app.get('/api/products', (req, res) => {
  const detailed = req.query.detailed === 'true';
  
  if (detailed) {
    // Return full product details
    res.json(products);
  } else {
    // Return simplified version
    const simple = products.map(p => ({
      id: p.id,
      name: p.name
    }));
    res.json(simple);
  }
});
```

### Consumer
```javascript
async getSimpleProducts() {
  const response = await fetch(`${this.baseUrl}/api/products`);
  return response.json();  // Returns: [{ id, name }]
}

async getDetailedProducts() {
  const response = await fetch(`${this.baseUrl}/api/products?detailed=true`);
  return response.json();  // Returns: [{ id, name, description, price, inStock }]
}
```

---

## Example 5: Authentication in Contract

### Spec with Security Scheme
```yaml
openapi: 3.0.0
info:
  title: Secure Product API
  version: 1.0.0

components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT

security:
  - BearerAuth: []

paths:
  /api/products:
    get:
      security:
        - BearerAuth: []
      responses:
        '200':
          description: Products list
          schema:
            type: array
        '401':
          description: Unauthorized
```

### Provider Auth Middleware
```javascript
const authenticateToken = (req, res, next) => {
  const authHeader = req.headers['authorization'];
  const token = authHeader && authHeader.split(' ')[1];

  if (!token) {
    return res.status(401).json({
      code: 'UNAUTHORIZED',
      message: 'Authorization token required'
    });
  }

  // Validate token format
  if (!token.startsWith('eyJ')) {
    return res.status(401).json({
      code: 'INVALID_TOKEN',
      message: 'Invalid token format'
    });
  }

  next();
};

app.get('/api/products', authenticateToken, (req, res) => {
  res.json(products);
});
```

### Consumer Auth Test
```javascript
it('should include authorization header', async () => {
  const token = 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';
  
  const response = await fetch(`${this.baseUrl}/api/products`, {
    headers: {
      'Authorization': token
    }
  });
  
  expect(response.status).toBe(200);
});

it('should reject request without token', async () => {
  const response = await fetch(`${this.baseUrl}/api/products`);
  expect(response.status).toBe(401);
});
```

---

## Example 6: Rate Limiting Contract

### Spec
```yaml
paths:
  /api/products:
    get:
      responses:
        '200':
          description: Products
          headers:
            X-RateLimit-Limit:
              schema:
                type: integer
            X-RateLimit-Remaining:
              schema:
                type: integer
            X-RateLimit-Reset:
              schema:
                type: integer
        '429':
          description: Too Many Requests
```

### Provider Rate Limiting
```javascript
import rateLimit from 'express-rate-limit';

const limiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100, // limit each IP to 100 requests per windowMs
  handler: (req, res) => {
    res.status(429).json({
      code: 'RATE_LIMIT_EXCEEDED',
      message: 'Too many requests'
    });
  }
});

app.get('/api/products', limiter, (req, res) => {
  res.set('X-RateLimit-Limit', '100');
  res.set('X-RateLimit-Remaining', '99');
  res.set('X-RateLimit-Reset', Date.now() + 15 * 60 * 1000);
  res.json(products);
});
```

---

## Running Examples

For each example:

1. **Update `specs/products-api.yaml`** with new schema/paths
2. **Update `provider/src/index.js`** to implement the contract
3. **Update `consumer/src/contract.test.js`** to test the contract
4. **Run tests**: `npm test` in consumer folder
5. **Verify**: All tests should pass

### Quick Test Loop

```bash
# Terminal 1: Start provider
cd provider && npm start

# Terminal 2: Run tests
cd consumer && npm test

# Make changes, watch tests update
# Tests rerun automatically with --watch flag
npm test -- --watch
```

---

## Next Steps

1. Try each example above
2. Modify the contract and see tests fail
3. Fix the implementation and see tests pass
4. Create your own API with Specmatic
5. Integrate into CI/CD pipeline

**Happy Testing! 🚀**
