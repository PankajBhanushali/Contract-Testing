# Specmatic Setup & Installation Guide

## Installation Methods

### Method 1: Global Installation (Recommended)

#### Install Specmatic CLI
```bash
npm install -g specmatic
```

Verify installation:
```bash
specmatic --version
specmatic help
```

### Method 2: Local Project Installation

In your project directory:
```bash
npm install --save-dev specmatic
```

Then use:
```bash
npx specmatic --version
npx specmatic help
```

### Method 3: Docker

Pull the Specmatic Docker image:
```bash
docker pull znsio/specmatic:latest
```

Run container:
```bash
docker run -v $(pwd):/app znsio/specmatic:latest test --spec /app/specs/products-api.yaml
```

## Setup for This Project

### Prerequisites
- Node.js 16+ (download from https://nodejs.org)
- npm 8+ (comes with Node.js)
- Git (for version control)

### Step-by-Step Setup

#### 1. Clone or Copy the Example
```bash
git clone <repo-url>
cd specmatic-contract-testing
```

#### 2. Install Global Dependencies
```bash
npm install -g specmatic
npm install -g nodemon  # Optional: for auto-reload during development
```

#### 3. Install Provider Dependencies
```bash
cd provider
npm install
```

Output should show:
```
added XX packages
```

#### 4. Install Consumer Dependencies
```bash
cd ../consumer
npm install
```

#### 5. Start Provider (Terminal 1)
```bash
cd provider
npm start
```

Expected output:
```
Provider API running at http://localhost:8080
OpenAPI Spec available at http://localhost:8080/api-docs
```

#### 6. Run Consumer Tests (Terminal 2)
```bash
cd consumer
npm test
```

Expected: **16 tests pass**

## Project Structure Reference

```
specmatic-contract-testing/
│
├── specs/                          # Specifications
│   └── products-api.yaml          # OpenAPI 3.0 contract
│
├── provider/                       # API Provider
│   ├── src/
│   │   └── index.js               # Express.js server
│   ├── Dockerfile                 # Docker configuration
│   ├── package.json               # Dependencies
│   └── package-lock.json          # Lock file
│
├── consumer/                       # API Consumer
│   ├── src/
│   │   ├── api-client.js          # API client
│   │   └── contract.test.js       # Jest tests
│   ├── jest.config.js             # Jest configuration
│   ├── package.json               # Dependencies
│   └── package-lock.json          # Lock file
│
├── specmatic.yaml                 # Specmatic configuration
├── docker-compose.yml             # Docker Compose setup
├── README.md                       # Full documentation
├── QUICK-START.md                 # Quick start guide
├── ARCHITECTURE.md                # Architecture & concepts
└── SETUP.md                        # This file
```

## Configuration Files Explained

### specmatic.yaml
```yaml
specmaticConfig:
  specificationPath: "specs"     # Where specs are located
  baseURL: "http://localhost:8080"  # Provider URL
  testing:
    timeout: 10000               # Test timeout (ms)
    retries: 3                   # Number of retries
    verbose: true                # Detailed logging
  mockServer:
    enabled: true                # Enable mock server
    port: 9000                   # Mock server port
```

### package.json (Provider)
```json
{
  "name": "specmatic-example-provider",
  "version": "1.0.0",
  "type": "module",
  "main": "src/index.js",
  "scripts": {
    "start": "node src/index.js",
    "dev": "nodemon src/index.js"
  },
  "dependencies": {
    "express": "^4.18.2",
    "cors": "^2.8.5"
  }
}
```

### jest.config.js (Consumer)
```javascript
module.exports = {
  testEnvironment: 'node',
  testMatch: ['**/*.test.js'],
  collectCoverageFrom: ['src/**/*.js'],
  coveragePathIgnorePatterns: ['/node_modules/']
};
```

## Common Commands

### Provider Commands
```bash
# Start provider
cd provider && npm start

# Start with auto-reload
cd provider && npm run dev

# Check dependencies
cd provider && npm list
```

### Consumer Commands
```bash
# Run all tests
cd consumer && npm test

# Run tests in watch mode
cd consumer && npm test -- --watch

# Run specific test file
cd consumer && npm test contract.test.js

# Run with coverage report
cd consumer && npm test -- --coverage

# Run single test
cd consumer && npm test -- -t "should return an array"
```

### Specmatic Commands
```bash
# Test provider against spec
specmatic test --spec specs/products-api.yaml --baseurl http://localhost:8080

# Generate mock server
specmatic stub --spec specs/products-api.yaml

# Generate HTML documentation
specmatic docs --spec specs/products-api.yaml

# Validate spec
specmatic validate --spec specs/products-api.yaml
```

## Docker Setup

### Option 1: Docker Compose

Start everything with one command:
```bash
docker-compose up
```

Stops everything:
```bash
docker-compose down
```

### Option 2: Manual Docker

Build provider image:
```bash
cd provider
docker build -t specmatic-provider:1.0 .
```

Run provider container:
```bash
docker run -p 8080:8080 specmatic-provider:1.0
```

Run consumer tests against container:
```bash
npm test
```

## CI/CD Integration

### GitHub Actions

Create `.github/workflows/contract-tests.yml`:
```yaml
name: Contract Tests

on: [push, pull_request]

jobs:
  contract-tests:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
      
      - name: Install Specmatic
        run: npm install -g specmatic
      
      - name: Install Provider Dependencies
        run: cd provider && npm install
      
      - name: Start Provider
        run: cd provider && npm start &
      
      - name: Wait for Provider
        run: sleep 5
      
      - name: Install Consumer Dependencies
        run: cd consumer && npm install
      
      - name: Run Consumer Contract Tests
        run: cd consumer && npm test
      
      - name: Run Specmatic Tests
        run: |
          specmatic test \
            --spec specs/products-api.yaml \
            --baseurl http://localhost:8080
```

### GitLab CI

Create `.gitlab-ci.yml`:
```yaml
stages:
  - test

contract_tests:
  stage: test
  image: node:18
  script:
    - npm install -g specmatic
    - cd provider && npm install && npm start &
    - sleep 5
    - cd ../consumer && npm install && npm test
    - specmatic test --spec specs/products-api.yaml --baseurl http://localhost:8080
```

## Troubleshooting Installation

### Issue: "npm: command not found"
**Solution**: Install Node.js from https://nodejs.org

### Issue: "specmatic: command not found"
**Solution**:
```bash
npm install -g specmatic
# or if using local installation:
npx specmatic --version
```

### Issue: "EACCES: permission denied"
**Solution** (Mac/Linux):
```bash
sudo npm install -g specmatic
```

### Issue: "Port 8080 already in use"
**Solution**:
```bash
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# Mac/Linux
lsof -i :8080
kill -9 <PID>
```

### Issue: "Cannot find module 'express'"
**Solution**:
```bash
cd provider
npm install
```

### Issue: Tests timeout
**Solution**:
1. Verify provider is running: `curl http://localhost:8080/health`
2. Check logs for errors
3. Increase timeout in `jest.config.js`:
```javascript
module.exports = {
  testTimeout: 30000  // 30 seconds
};
```

## Verification Checklist

After setup, verify everything works:

- [ ] Node.js installed (`node --version`)
- [ ] npm installed (`npm --version`)
- [ ] Specmatic installed (`specmatic --version`)
- [ ] Provider starts (`npm start` in provider folder)
- [ ] Provider responds to requests (`curl http://localhost:8080/api/products`)
- [ ] Consumer tests run (`npm test` in consumer folder)
- [ ] All 16 tests pass
- [ ] Spec validates (`specmatic validate --spec specs/products-api.yaml`)

## Next Steps

1. ✅ Complete this setup
2. ✅ Read QUICK-START.md for 5-minute overview
3. ✅ Read README.md for comprehensive guide
4. ✅ Read ARCHITECTURE.md to understand concepts
5. ✅ Modify the spec and see tests fail/pass
6. ✅ Integrate into your CI/CD pipeline
7. ✅ Create your own Specmatic projects

## Support Resources

- **Specmatic Official**: https://specmatic.io
- **Specmatic GitHub**: https://github.com/znsio/specmatic
- **Specmatic Docs**: https://specmatic.io/documentation
- **OpenAPI Spec**: https://spec.openapis.org
- **Node.js Docs**: https://nodejs.org/docs
- **Express.js**: https://expressjs.com

## Uninstall / Cleanup

If you want to remove Specmatic:

```bash
# Remove global installation
npm uninstall -g specmatic

# Remove local dependencies
cd provider && npm prune
cd ../consumer && npm prune

# Remove node_modules
cd provider && rm -rf node_modules
cd ../consumer && rm -rf node_modules
```

## Performance Tips

### For Development
```bash
# Use nodemon for auto-reload
npm install -g nodemon
nodemon provider/src/index.js
```

### For Testing
```bash
# Run tests in parallel
npm test -- --maxWorkers=4

# Run only changed tests
npm test -- --onlyChanged
```

### For Production
```bash
# Use production mode
NODE_ENV=production npm start

# Use pm2 for process management
npm install -g pm2
pm2 start provider/src/index.js --name "specmatic-api"
```

---

**Setup Complete!** You're ready to work with Specmatic contract testing.
