# Quick Reference - OpenAPI Demo Setup

## One-Line Start (Windows PowerShell)

```powershell
cd openapi-contract-testing; docker-compose up -d; Start-Sleep -s 5; cd consumer; npm install; npm test
```

## One-Line Start (Bash)

```bash
cd openapi-contract-testing && docker-compose up -d && sleep 5 && cd consumer && npm install && npm test
```

## Directory Structure

```
openapi-contract-testing/
├── openapi.yaml              ← Single source of truth (API contract)
├── docker-compose.yml        ← Start provider in Docker
├── run-demo.bat              ← Windows startup script
├── run-demo.sh               ← Unix startup script
├── README.md                 ← Full documentation
├── SETUP.md                  ← Setup instructions
│
├── provider/                 ← VAIS Provider API
│   ├── index.js             ← Express server (responds to both v1 & v2)
│   ├── package.json
│   ├── Dockerfile
│   └── Dockerfile
│
└── consumer/                ← SF Consumer Tests
    ├── v1-client.js         ← SF 17.1 client (calls GET /users)
    ├── v2-client.js         ← SF 18.1 client (calls GET /users?apiVersion=2)
    ├── schema-validator.js  ← Validates responses
    ├── package.json
    └── tests/
        ├── v1.test.js       ← 7 tests for SF 17.1
        └── v2.test.js       ← 9 tests for SF 18.1
```

## What Each Component Does

| Component | Purpose | Technology |
|-----------|---------|------------|
| `openapi.yaml` | Defines API contract (both v1 & v2) | OpenAPI 3.0 |
| `provider/index.js` | Serves `/users` in v1 or v2 format | Node.js + Express |
| `consumer/v1-client.js` | Calls `GET /users` | JavaScript + Axios |
| `consumer/v2-client.js` | Calls `GET /users?apiVersion=2` | JavaScript + Axios |
| `consumer/tests/` | Validates responses against spec | Jest framework |

## Step-by-Step Demo

### Step 1: Review OpenAPI Specification (2 minutes)

```bash
cat openapi.yaml

# Key points:
# - Single /users endpoint
# - apiVersion query parameter controls format
# - V1 schema: id, name only
# - V2 schema: id, name, email, role
```

### Step 2: Start Provider (1 minute)

```bash
docker-compose up -d
curl http://localhost:5001/health
```

### Step 3: Understand Provider Implementation (2 minutes)

```bash
cat provider/index.js

# Key logic:
# if (apiVersion === '2') {
#   return v2 response (with email, role)
# } else {
#   return v1 response (just id, name)
# }
```

### Step 4: View Consumer Clients (2 minutes)

```bash
cat consumer/v1-client.js
cat consumer/v2-client.js

# V1: No query params
# V2: Sends ?apiVersion=2
```

### Step 5: Run Tests (3 minutes)

```bash
cd consumer
npm install
npm test

# Expected: 16 tests pass ✓
# - 7 tests for V1
# - 9 tests for V2
```

### Step 6: Manual API Testing (3 minutes)

```bash
# V1 endpoint (SF 17.1)
curl http://localhost:5001/users

# V2 endpoint (SF 18.1)
curl "http://localhost:5001/users?apiVersion=2"

# Note the difference:
# V1: Only id and name
# V2: id, name, email, role
```

## Key Files to Understand

### 1. openapi.yaml (THE CONTRACT)

```yaml
paths:
  /users:
    get:
      parameters:
        - name: apiVersion
          enum: ["1", "2"]
      responses:
        "200":
          # One response with oneOf for V1 or V2
          schema:
            oneOf:
              - $ref: "#/components/schemas/UsersResponseV1"
              - $ref: "#/components/schemas/UsersResponseV2"
```

### 2. provider/index.js (IMPLEMENTS CONTRACT)

```javascript
app.get('/users', (req, res) => {
  const apiVersion = req.query.apiVersion || '1';
  
  if (apiVersion === '2') {
    return res.json({ users: [v2 format] });
  }
  return res.json({ users: [v1 format] });
});
```

### 3. consumer/v1-client.js (CONSUMES V1)

```javascript
async getUsers(limit = 50) {
  // Call GET /users (NO apiVersion param)
  // Expect: { users: [{id, name}] }
}
```

### 4. consumer/v2-client.js (CONSUMES V2)

```javascript
async getUsers(limit = 50) {
  // Call GET /users?apiVersion=2
  // Expect: { users: [{id, name, email, role}] }
}
```

### 5. consumer/tests/v1.test.js (VALIDATES V1 CONTRACT)

```javascript
it('should return v1 schema: only id and name', async () => {
  const response = await client.getUsers();
  const validation = SchemaValidator.validateArray(users, userV1Schema);
  expect(validation.valid).toBe(true);
});
```

### 6. consumer/tests/v2.test.js (VALIDATES V2 CONTRACT)

```javascript
it('should return v2 schema: id, name, email, role', async () => {
  const response = await client.getUsers();
  const validation = SchemaValidator.validateArray(users, userV2Schema);
  expect(validation.valid).toBe(true);
});
```

## Test Results Explained

### V1 Tests (7 tests)

```
✓ should return 200 status code
✓ should return users array
✓ should return v1 schema: only id and name
✓ should NOT include email or role fields
✓ should respect limit parameter
✓ should have valid response headers
✓ should handle server errors gracefully
```

**What they validate**: SF 17.1 contract is satisfied

### V2 Tests (9 tests)

```
✓ should return 200 status code
✓ should return users array
✓ should return v2 schema: id, name, email, role
✓ should include all required v2 fields
✓ should have valid email format in v2 response
✓ should have valid role values in v2 response
✓ should respect limit parameter in v2
✓ should have valid response headers
✓ v2 response should include v1 fields (backward compatible)
```

**What they validate**: SF 18.1 contract is satisfied + backward compatibility

## How This Demonstrates the Scenario

```
Timeline:

SF 17.1 (Old)
├─ Calls GET /users
├─ Expects: {users: [{id, name}]}
└─ Provider VAIS 1.0
   └─ Returns v1 response ✓

Time passes... SF 18.1 released

SF 18.1 (New)
├─ Calls GET /users?apiVersion=2
├─ Expects: {users: [{id, name, email, role}]}
└─ Provider VAIS 2.0
   └─ Supports both v1 AND v2 ✓

Result:
✓ SF 17.1 still works with VAIS 2.0
✓ SF 18.1 works with VAIS 2.0
✓ No breaking changes
✓ Both contracts validated
```

## What's Being Demonstrated

1. **Single Source of Truth**: OpenAPI spec defines both v1 and v2
2. **Consumer-Driven**: Clients define what they need
3. **Schema Validation**: Responses must match schema
4. **Backward Compatibility**: v1 clients still work
5. **Version Support**: Provider handles multiple versions
6. **Automated Testing**: All contracts verified automatically

## Extending the Demo

### Add POST /users

1. Add to `openapi.yaml`:
```yaml
post:
  requestBody:
    schema: { $ref: "#/components/schemas/CreateUserRequest" }
  responses:
    "201": { description: "Created" }
```

2. Implement in `provider/index.js`
3. Add tests in `consumer/tests/`

### Add Database

Replace mock data with real database (MongoDB, PostgreSQL, etc.)

### Add CI/CD

Create GitHub Actions workflow to run tests on every commit

---

**Created**: November 26, 2025
