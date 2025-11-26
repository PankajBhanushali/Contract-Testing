# OpenAPI Contract Testing Demo

Complete working example demonstrating contract testing with OpenAPI/Swagger validation.

## Scenario

- **SF 17.1 Consumer**: Calls `GET /users` (v1 format)
- **SF 18.1 Consumer**: Calls `GET /users?apiVersion=2` (v2 format)
- **VAIS Provider**: Supports both endpoints simultaneously

## Project Structure

```
openapi-contract-testing/
├── openapi.yaml              # OpenAPI 3.0 specification (single source of truth)
├── docker-compose.yml        # Docker Compose setup
├── README.md                 # This file
│
├── provider/                 # VAIS Provider API
│   ├── package.json
│   ├── index.js             # Express.js server
│   ├── Dockerfile           # Provider Docker image
│   └── .gitignore
│
└── consumer/                # SF Consumer Tests
    ├── package.json
    ├── v1-client.js         # SF 17.1 API client
    ├── v2-client.js         # SF 18.1 API client
    ├── schema-validator.js  # JSON Schema validator
    └── tests/
        ├── v1.test.js       # SF 17.1 contract tests
        └── v2.test.js       # SF 18.1 contract tests
```

## Quick Start

### 1. Prerequisites

- Node.js 18+ (for consumer tests)
- Docker & Docker Compose (for provider)
- Git

### 2. Start Provider (Option A: Docker)

```bash
# From root directory
docker-compose up -d

# Check if running
docker-compose ps

# View logs
docker-compose logs -f provider-api

# Stop
docker-compose down
```

### 3. Start Provider (Option B: Local Node.js)

```bash
cd provider
npm install
node index.js

# Output:
# ✓ Provider API running on http://localhost:5001
```

### 4. Run Consumer Tests

```bash
cd consumer
npm install

# Run all tests
npm test

# Run v1 tests only
npm run test:v1

# Run v2 tests only
npm run test:v2

# Watch mode
npm run test:watch
```

## API Endpoints

### GET /users (v1 format - SF 17.1)

Request:
```bash
curl http://localhost:5001/users
```

Response:
```json
{
  "users": [
    { "id": 1, "name": "John Doe" },
    { "id": 2, "name": "Jane Smith" }
  ]
}
```

**Contract Expectations**:
- ✅ Status: 200
- ✅ Fields: id, name only
- ❌ No email field
- ❌ No role field

### GET /users?apiVersion=2 (v2 format - SF 18.1)

Request:
```bash
curl "http://localhost:5001/users?apiVersion=2"
```

Response:
```json
{
  "users": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john@company.com",
      "role": "admin"
    },
    {
      "id": 2,
      "name": "Jane Smith",
      "email": "jane@company.com",
      "role": "user"
    }
  ]
}
```

**Contract Expectations**:
- ✅ Status: 200
- ✅ Fields: id, name, email, role
- ✅ Email valid format
- ✅ Role enum: admin, user, guest

## Test Results

### V1 Tests (SF 17.1)

```
PASS  tests/v1.test.js
  SF 17.1 Consumer - V1 Contracts
    GET /users (v1 format)
      ✓ should return 200 status code
      ✓ should return users array
      ✓ should return v1 schema: only id and name
      ✓ should NOT include email or role fields
      ✓ should respect limit parameter
      ✓ should have valid response headers
    Error Handling
      ✓ should handle server errors gracefully

Test Suites: 1 passed, 1 total
Tests:       7 passed, 7 total
```

### V2 Tests (SF 18.1)

```
PASS  tests/v2.test.js
  SF 18.1 Consumer - V2 Contracts
    GET /users?apiVersion=2 (v2 format)
      ✓ should return 200 status code
      ✓ should return users array
      ✓ should return v2 schema: id, name, email, role
      ✓ should include all required v2 fields
      ✓ should have valid email format in v2 response
      ✓ should have valid role values in v2 response
      ✓ should respect limit parameter in v2
      ✓ should have valid response headers
    Backward Compatibility Check
      ✓ v2 response should include v1 fields

Test Suites: 1 passed, 1 total
Tests:       9 passed, 9 total
```

## Demonstration Flow

### Step 1: Check OpenAPI Spec

```bash
cat openapi.yaml

# Verify:
# - /users endpoint defined
# - V1 and V2 schemas
# - Query parameter apiVersion
```

### Step 2: Start Provider

```bash
docker-compose up -d
sleep 5
curl http://localhost:5001/health
```

### Step 3: Test V1 Contract

```bash
cd consumer
npm run test:v1

# Expected: All 7 tests pass ✓
```

### Step 4: Test V2 Contract

```bash
npm run test:v2

# Expected: All 9 tests pass ✓
```

### Step 5: Manual Testing

```bash
# Test v1 endpoint
curl http://localhost:5001/users | jq

# Test v2 endpoint
curl "http://localhost:5001/users?apiVersion=2" | jq

# Compare responses
# V1: Only id and name
# V2: id, name, email, role
```

## Key Concepts Demonstrated

### 1. Single Source of Truth
- OpenAPI spec defines contract
- Both consumer and provider validate against it
- No ambiguity about expectations

### 2. Multiple Versions
- Same endpoint serves different formats
- Query parameter controls version
- Backward compatibility maintained

### 3. Schema Validation
- Consumer validates response against schema
- Provider generates schema-compliant responses
- Errors caught early

### 4. Backward Compatibility
- V1 clients work with new provider
- V2 clients get extended fields
- No breaking changes

## Extending the Demo

### Add New Endpoint

1. Update `openapi.yaml`:
```yaml
paths:
  /users/{id}:
    get:
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      responses:
        "200":
          description: User detail
```

2. Implement in `provider/index.js`:
```javascript
app.get('/users/:id', (req, res) => {
  // Implementation
});
```

3. Add tests in `consumer/tests/`:
```javascript
// New test file
```

### Add Database

Replace mock data in `provider/index.js` with:
- MongoDB
- PostgreSQL
- Firebase
- etc.

### Add Authentication

Add middleware to provider:
```javascript
app.use(authMiddleware);
```

### Add API Gateway

Use:
- Kong
- AWS API Gateway
- Azure API Management

## Troubleshooting

### Provider won't start

```bash
# Check if port 5001 is in use
lsof -i :5001

# Use different port
PORT=5002 npm run start
```

### Tests fail with connection error

```bash
# Ensure provider is running
curl http://localhost:5001/health

# Wait for startup
sleep 5
npm test
```

### Docker issues

```bash
# Rebuild images
docker-compose build --no-cache

# Check logs
docker-compose logs

# Clean up
docker-compose down -v
```

## Files Reference

| File | Purpose |
|------|---------|
| `openapi.yaml` | API contract specification |
| `provider/index.js` | Provider implementation |
| `consumer/v1-client.js` | V1 consumer client |
| `consumer/v2-client.js` | V2 consumer client |
| `consumer/tests/v1.test.js` | V1 contract tests |
| `consumer/tests/v2.test.js` | V2 contract tests |

## Next Steps

1. **Add Postman Collection**
   - Export from OpenAPI spec
   - Run via Newman
   - CI/CD integration

2. **Add Dredd Testing**
   - npm install -g dredd
   - Create hooks.js
   - Automate testing

3. **Add Prism Mock Server**
   - npm install -g @stoplight/prism-cli
   - prism mock openapi.yaml
   - Consumer testing without real provider

4. **CI/CD Pipeline**
   - GitHub Actions
   - Azure Pipelines
   - GitLab CI

---

**Created**: November 26, 2025
**Updated**: November 26, 2025
