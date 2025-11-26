# OpenAPI Contract Testing Demo - Setup Guide

## Prerequisites

### Required

- **Node.js 18+**: [Download](https://nodejs.org/)
- **Docker & Docker Compose**: [Install](https://www.docker.com/products/docker-desktop)
- **Git**: [Download](https://git-scm.com/)

### Verify Installation

```powershell
# PowerShell
node --version
# v18.x.x

docker --version
# Docker version 24.x.x

docker-compose --version
# Docker Compose version 2.x.x

git --version
# git version 2.x.x
```

## Setup Steps

### Step 1: Navigate to Demo Directory

```powershell
cd "c:\Users\crf8625\OneDrive - Siemens Healthineers\Documents\Work\Learning\Contract Testing\openapi-contract-testing"
```

### Step 2: Start Provider API

#### Option A: Docker Compose (Recommended)

```powershell
# Start provider in background
docker-compose up -d

# Verify it's running
docker-compose ps

# Output:
# NAME              COMMAND         SERVICE         STATUS        PORTS
# provider-api      "node index.js" provider-api    Up 30 seconds  0.0.0.0:5001->5001/tcp

# Check health
curl http://localhost:5001/health
```

#### Option B: Local Node.js

```powershell
cd provider
npm install
node index.js

# Output:
# ✓ Provider API running on http://localhost:5001
```

### Step 3: Install Consumer Dependencies

```powershell
cd consumer
npm install

# Output:
# npm warn ...
# added XXX packages in X.XXs
```

### Step 4: Run Tests

```powershell
# Run all tests
npm test

# Or specific versions:
npm run test:v1  # SF 17.1 tests
npm run test:v2  # SF 18.1 tests
```

## Expected Output

### Successful Test Run

```
PASS  tests/v1.test.js (5.123 s)
  SF 17.1 Consumer - V1 Contracts
    GET /users (v1 format)
      ✓ should return 200 status code (45 ms)
      ✓ should return users array (12 ms)
      ✓ should return v1 schema: only id and name (8 ms)
      ✓ should NOT include email or role fields (6 ms)
      ✓ should respect limit parameter (9 ms)
      ✓ should have valid response headers (5 ms)
    Error Handling
      ✓ should handle server errors gracefully (3 ms)

PASS  tests/v2.test.js (3.456 s)
  SF 18.1 Consumer - V2 Contracts
    GET /users?apiVersion=2 (v2 format)
      ✓ should return 200 status code (42 ms)
      ✓ should return users array (10 ms)
      ✓ should return v2 schema: id, name, email, role (12 ms)
      ✓ should include all required v2 fields (8 ms)
      ✓ should have valid email format in v2 response (7 ms)
      ✓ should have valid role values in v2 response (5 ms)
      ✓ should respect limit parameter in v2 (6 ms)
      ✓ should have valid response headers (4 ms)
    Backward Compatibility Check
      ✓ v2 response should include v1 fields (5 ms)

Tests:       16 passed, 16 total
Snapshots:   0 total
Time:        8.579 s
```

## Manual Testing

### Test V1 Endpoint

```powershell
# V1 Response (SF 17.1)
curl http://localhost:5001/users

# Response:
#{
#  "users": [
#    { "id": 1, "name": "John Doe" },
#    { "id": 2, "name": "Jane Smith" },
#    { "id": 3, "name": "Bob Johnson" }
#  ]
#}
```

### Test V2 Endpoint

```powershell
# V2 Response (SF 18.1)
curl "http://localhost:5001/users?apiVersion=2"

# Response:
#{
#  "users": [
#    {
#      "id": 1,
#      "name": "John Doe",
#      "email": "john@company.com",
#      "role": "admin"
#    },
#    {
#      "id": 2,
#      "name": "Jane Smith",
#      "email": "jane@company.com",
#      "role": "user"
#    },
#    {
#      "id": 3,
#      "name": "Bob Johnson",
#      "email": "bob@company.com",
#      "role": "guest"
#    }
#  ]
#}
```

### Test Limit Parameter

```powershell
# Get only 2 users
curl "http://localhost:5001/users?limit=2"

# Get v2 with limit
curl "http://localhost:5001/users?apiVersion=2&limit=1"
```

### Test Error Handling

```powershell
# Invalid API version
curl "http://localhost:5001/users?apiVersion=3"

# Response:
#{
#  "code": "INVALID_PARAMETER",
#  "message": "Invalid apiVersion parameter. Use \"1\" or \"2\""
#}
```

## Demonstration Flow (For Presentations)

### 5-Minute Demo

```powershell
# 1. Show OpenAPI Spec (1 min)
code openapi.yaml

# 2. Start Provider (1 min)
docker-compose up -d
curl http://localhost:5001/health

# 3. Show Consumer Code (1 min)
code consumer/v1-client.js
code consumer/v2-client.js

# 4. Run Tests (2 min)
cd consumer
npm test
```

### 15-Minute Deep Dive

```powershell
# 1. Explain Architecture (3 min)
# - Show OpenAPI spec
# - Explain v1 vs v2 differences
# - Show provider implementation

# 2. Start Services (2 min)
docker-compose up -d
sleep 5

# 3. Manual API Testing (4 min)
curl http://localhost:5001/users                    # V1
curl "http://localhost:5001/users?apiVersion=2"    # V2
curl "http://localhost:5001/users?limit=2"         # Limit

# 4. Show Consumer Code (2 min)
code consumer/tests/v1.test.js
code consumer/tests/v2.test.js

# 5. Run Automated Tests (4 min)
cd consumer
npm run test:v1
npm run test:v2
```

## Troubleshooting

### Issue: Port 5001 Already in Use

```powershell
# Find process using port 5001
Get-NetTCPConnection -LocalPort 5001 | Select-Object OwningProcess
taskkill /PID <PID> /F

# Or use different port
$env:PORT=5002
docker-compose up -d
```

### Issue: Docker Not Starting

```powershell
# Check Docker daemon
docker ps

# Start Docker Desktop if needed
# Then retry docker-compose up
docker-compose up -d
```

### Issue: Tests Fail with Connection Error

```powershell
# Ensure provider is healthy
curl http://localhost:5001/health

# Wait for startup if just started
Start-Sleep -Seconds 5

# Retry tests
npm test
```

### Issue: npm install Fails

```powershell
# Clear npm cache
npm cache clean --force

# Retry install
npm install

# Or use different registry
npm install --registry https://registry.npmjs.org/
```

## Cleanup

### Stop Provider

```powershell
# Stop Docker containers
docker-compose down

# Remove volumes
docker-compose down -v

# Remove images
docker-compose down -v --rmi all
```

### Clean Local Files

```powershell
# Remove node_modules
rmdir -r consumer\node_modules
rmdir -r provider\node_modules

# Remove lock files
del consumer\package-lock.json
del provider\package-lock.json
```

## Next Steps After Setup

### 1. Customize the Demo

- Add more user data in `provider/index.js`
- Add new endpoints to `openapi.yaml`
- Create additional test scenarios

### 2. Extend with Postman

```bash
# Export OpenAPI to Postman collection
# Use Postman CLI to run tests
```

### 3. Add CI/CD Integration

```bash
# GitHub Actions workflow
# Azure Pipelines configuration
# GitLab CI pipeline
```

### 4. Add Advanced Testing

```bash
# Dredd contract testing
# Schemathesis property-based testing
# Prism mock server validation
```

## Files Created

| File | Size | Purpose |
|------|------|---------|
| `openapi.yaml` | 5 KB | API contract specification |
| `provider/index.js` | 2 KB | Express.js provider implementation |
| `provider/package.json` | 200 B | Provider dependencies |
| `provider/Dockerfile` | 200 B | Provider Docker image |
| `consumer/v1-client.js` | 1 KB | V1 consumer client |
| `consumer/v2-client.js` | 1 KB | V2 consumer client |
| `consumer/schema-validator.js` | 3 KB | JSON schema validator |
| `consumer/tests/v1.test.js` | 2 KB | V1 contract tests |
| `consumer/tests/v2.test.js` | 2 KB | V2 contract tests |
| `consumer/package.json` | 300 B | Consumer dependencies |
| `docker-compose.yml` | 400 B | Docker Compose configuration |
| `run-demo.bat` | 1 KB | Windows startup script |
| `run-demo.sh` | 1 KB | Unix startup script |
| `README.md` | 8 KB | Project documentation |
| `SETUP.md` | 10 KB | This setup guide |

## Support

### Check Provider Logs

```powershell
docker-compose logs -f provider-api

# Sample output:
# [2025-11-26T10:30:45.123Z] GET /users?apiVersion=2
# [2025-11-26T10:30:45.234Z] GET /health
```

### Check Test Logs

```powershell
cd consumer
npm test -- --verbose

# More detailed output from Jest
npm test -- --no-coverage --verbose
```

### Debug Mode

```powershell
# Run with detailed logging
$env:DEBUG=*
npm test
```

---

**Setup Guide Created**: November 26, 2025
**Last Updated**: November 26, 2025
