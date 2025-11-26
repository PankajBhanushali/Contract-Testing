# Specmatic Contract Testing - Complete Index

## Quick Navigation

### Getting Started (Start Here!)
- **[QUICK-START.md](QUICK-START.md)** - 5-minute setup and first test
- **[SETUP.md](SETUP.md)** - Detailed installation and configuration

### Understanding Specmatic
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Concepts, workflows, and design patterns
- **[README.md](README.md)** - Comprehensive guide with all details
- **[EXAMPLES.md](EXAMPLES.md)** - Practical code examples and use cases

### Project Files
- **[specs/products-api.yaml](specs/products-api.yaml)** - OpenAPI contract specification
- **[provider/src/index.js](provider/src/index.js)** - Express.js API provider
- **[consumer/src/api-client.js](consumer/src/api-client.js)** - API client implementation
- **[consumer/src/contract.test.js](consumer/src/contract.test.js)** - Contract tests
- **[specmatic.yaml](specmatic.yaml)** - Specmatic configuration

---

## Learning Path

### Beginner
1. Read [QUICK-START.md](QUICK-START.md) (5 minutes)
2. Follow setup steps
3. Run: `npm start` in provider
4. Run: `npm test` in consumer
5. See all 16 tests pass ✅

### Intermediate
1. Read [ARCHITECTURE.md](ARCHITECTURE.md) (15 minutes)
2. Review [specs/products-api.yaml](specs/products-api.yaml)
3. Study [provider/src/index.js](provider/src/index.js)
4. Examine [consumer/src/contract.test.js](consumer/src/contract.test.js)
5. Try examples from [EXAMPLES.md](EXAMPLES.md)

### Advanced
1. Read [README.md](README.md) completely (30 minutes)
2. Study CI/CD integration section
3. Create your own API with Specmatic
4. Integrate into your pipeline
5. Scale to multiple services

---

## Key Concepts

### What is Specmatic?
**Specification-Driven API Contract Testing**
- Uses OpenAPI as the contract
- Automatically generates tests
- Validates both provider and consumer
- Generates mock servers

### Why Use Specmatic?
✅ Single source of truth (the spec)  
✅ Automatic test generation  
✅ Built-in mock servers  
✅ Multi-consumer support  
✅ Documentation generation  
✅ Easy CI/CD integration  

### How Does It Work?
```
OpenAPI Spec
    ↓
Tests Generated
    ↓
Provider Validated → Consumer Validated
    ↓              ↓
 Response OK   Request OK
    ↓              ↓
  PASS        PASS
```

---

## File Structure

```
specmatic-contract-testing/
│
├── 📋 Documentation
│   ├── README.md               # Complete guide
│   ├── QUICK-START.md          # 5-minute setup
│   ├── SETUP.md                # Detailed setup
│   ├── ARCHITECTURE.md         # Concepts & design
│   ├── EXAMPLES.md             # Code examples
│   └── INDEX.md                # This file
│
├── 📄 Configuration
│   ├── specmatic.yaml          # Specmatic config
│   ├── docker-compose.yml      # Docker setup
│   └── .gitignore              # Git ignore
│
├── 🔧 Provider (API Server)
│   ├── src/
│   │   └── index.js            # Express server
│   ├── Dockerfile              # Docker image
│   ├── package.json            # Dependencies
│   └── package-lock.json
│
├── 🧪 Consumer (Tests)
│   ├── src/
│   │   ├── api-client.js       # API client
│   │   └── contract.test.js    # Tests
│   ├── jest.config.js          # Jest config
│   ├── package.json            # Dependencies
│   └── package-lock.json
│
└── 📋 Contract
    └── specs/
        └── products-api.yaml   # OpenAPI spec
```

---

## Common Tasks

### Task: Run the Example
```bash
# Terminal 1: Start provider
cd provider && npm install && npm start

# Terminal 2: Run tests
cd consumer && npm install && npm test
```

**Expected Result**: 16 tests pass ✅

### Task: Modify the API
1. Update `specs/products-api.yaml`
2. Update `provider/src/index.js`
3. Update `consumer/src/contract.test.js`
4. Run `npm test` in consumer folder
5. Verify tests pass

### Task: Add Authentication
See [EXAMPLES.md](EXAMPLES.md) → Example 5: Authentication

### Task: Support Multiple API Versions
See [EXAMPLES.md](EXAMPLES.md) → Example 2: API Versioning

### Task: Setup CI/CD
See [SETUP.md](SETUP.md) → CI/CD Integration

### Task: Use Docker
```bash
docker-compose up
```

---

## Testing Commands

### Consumer Tests
```bash
cd consumer

# Run all tests
npm test

# Watch mode (rerun on changes)
npm test -- --watch

# Show coverage
npm test -- --coverage

# Run specific test
npm test -- -t "should return"
```

### Provider Validation (Specmatic CLI)
```bash
# Test provider against spec
specmatic test --spec specs/products-api.yaml

# Generate mock server
specmatic stub --spec specs/products-api.yaml

# Validate spec
specmatic validate --spec specs/products-api.yaml
```

---

## Key Differences Between Approaches

### Specmatic vs Pact vs Postman

| Feature | Specmatic | Pact | Postman |
|---------|-----------|------|---------|
| **Approach** | Spec-Driven | Consumer-Driven | Manual |
| **Source of Truth** | OpenAPI Spec | Pact File | Collection |
| **Auto Test Generation** | ✅ Yes | ❌ No | ❌ No |
| **Multi-Consumer** | ✅ One spec | ⚠️ Per consumer | ⚠️ Shared |
| **Documentation** | ✅ In spec | ❌ Separate | ⚠️ Limited |
| **Learning Curve** | Medium | Steep | Low |
| **Mock Server** | ✅ Built-in | ✅ Built-in | ⚠️ Cloud |

### When to Use Specmatic
✅ API-first development  
✅ Multiple consumers  
✅ REST APIs with OpenAPI  
✅ Need documentation  
✅ Want auto test generation  

### When NOT to Use Specmatic
❌ GraphQL APIs (limited support)  
❌ No OpenAPI spec  
❌ Highly dynamic APIs  

---

## Resources

### Official Documentation
- Specmatic: https://specmatic.io
- OpenAPI: https://spec.openapis.org
- Swagger: https://swagger.io

### Tools & Dependencies
- Node.js: https://nodejs.org
- Express.js: https://expressjs.com
- Jest: https://jestjs.io
- Docker: https://docker.com

### Related Examples
- Pact Example: `../pact-contract-testing/`
- Postman Example: `../postman-contract-testing/`
- OpenAPI Example: `../openapi-contract-testing/`

---

## Troubleshooting

### Tests Won't Run
1. Check provider is running: `curl http://localhost:8080/health`
2. Check Node.js version: `node --version` (needs 16+)
3. Reinstall dependencies: `npm install`

### Port Already in Use
```bash
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# Mac/Linux
lsof -i :8080 | awk 'NR!=1 {print $2}' | xargs kill -9
```

### Package Not Found
```bash
# In provider folder
cd provider && npm install

# In consumer folder
cd consumer && npm install
```

### Spec Validation Failed
```bash
# Install Specmatic
npm install -g specmatic

# Validate spec
specmatic validate --spec specs/products-api.yaml
```

---

## Next Steps

1. ✅ Run QUICK-START.md
2. ✅ Read ARCHITECTURE.md
3. ✅ Try examples from EXAMPLES.md
4. ✅ Create your own API
5. ✅ Integrate into CI/CD
6. ✅ Scale to your services

---

## Questions?

### For Specmatic Help
- Official Docs: https://specmatic.io
- GitHub: https://github.com/znsio/specmatic
- Community: Discussions and issues

### For This Example
- Review README.md for comprehensive guide
- Check EXAMPLES.md for code samples
- See ARCHITECTURE.md for concepts

---

**Ready to get started?** Begin with [QUICK-START.md](QUICK-START.md) 🚀
