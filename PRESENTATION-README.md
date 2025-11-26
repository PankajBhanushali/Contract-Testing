# Contract Testing Presentation Materials

## Overview
This directory contains complete presentation materials covering three approaches to contract testing: Pact, Postman, and OpenAPI - with working examples of all three.

## Contents

### 📊 Presentation
- **`PRESENTATION-CONTRACT-TESTING.md`**: Complete slide deck (28+ slides + backup)
  - Introduction to contract testing
  - Three approaches: Pact vs Postman vs OpenAPI
  - Detailed comparisons and use cases
  - Real-world examples
  - Best practices and pitfalls
  - Implementation roadmap
  - Decision framework

### 📚 Quick Reference
- **`CONTRACT-TESTING-QUICK-REFERENCE.md`**: One-page comparison guide
  - Feature matrix
  - Command reference
  - Common pitfalls & solutions
  - Setup checklists
  - ROI comparison

### 💻 Working Examples

#### Pact Example (Full Workshop)
```
pact-contract-testing/
├── Consumer/
│   ├── src/ApiClient.cs           # HTTP client implementation
│   └── tests/ApiTest.cs           # Pact consumer tests (generates contracts)
├── Provider/
│   ├── src/ProductsController.cs  # API implementation
│   └── tests/
│       ├── ProductTest.cs         # Pact provider verification
│       └── Middleware/
│           ├── ProviderStateMiddleware.cs
│           └── AuthTokenRequestFilter.cs
└── pacts/
    └── ApiClient-ProductService.json
```

#### Postman Example
```
postman-contract-testing/
├── Provider/src/              # ASP.NET Core API
├── Consumer/src/              # Consumer client
├── Consumer/tests/            # xUnit consumer tests
└── postman-collections/
    ├── Product-API-Contract-Tests.postman_collection.json
    └── Product-API-Environment.postman_environment.json
```

#### OpenAPI Example (NEW!)
```
openapi-contract-testing/
├── openapi.yaml              # OpenAPI 3.0 spec (single source of truth)
├── docker-compose.yml        # Docker Compose setup
├── provider/                 # Node.js Provider API (v1 & v2 support)
└── consumer/                 # Jest Consumer Tests
    ├── v1-client.js         # SF 17.1 client
    ├── v2-client.js         # SF 18.1 client
    └── tests/
        ├── v1.test.js       # 7 v1 tests
        └── v2.test.js       # 9 v2 tests
```

### 📖 Documentation
- **Consumer tests fix steps**: `pact-contract-testing/Consumer/tests/consumer-tests-fix-steps.md`
- **Postman setup guide**: `postman-contract-testing/README.md`
- **OpenAPI quick start**: `openapi-contract-testing/QUICK-START.md`
- **OpenAPI detailed guide**: `openapi-contract-testing/OPENAPI-CONTRACT-TESTING.md`
- **Workshop learning objectives**: `pact-contract-testing/LEARNING.md`

## How to Use This Presentation

### For Presenters

1. **Review the Materials** (45 minutes)
   - Read through `PRESENTATION-CONTRACT-TESTING.md`
   - Familiarize yourself with all three examples
   - Review the decision framework

2. **Customize for Your Audience** (1-2 hours)
   - Add company-specific context
   - Adjust technical depth
   - Select relevant slides (core deck covers 3 approaches)
   - Prepare demos

3. **Prepare Demos** (1.5-2 hours)
   - Set up all three examples
   - Practice running tests for each
   - Prepare failure scenarios
   - Time each demo

### For Decision Makers

**Quick Path** (20 minutes):
1. Read Slides 1-4: Introduction & solutions landscape
2. Read Slide 11: Side-by-side comparison (updated with OpenAPI)
3. Read Slide 22: Decision framework (updated)
4. Read `CONTRACT-TESTING-QUICK-REFERENCE.md`

**Deep Dive** (3 hours):
1. Read full presentation
2. Run all three demos
3. Review cost-benefit analysis
4. Study the three approaches
5. Consider hybrid strategy

### For Engineers

**Implementation Path** (6-8 hours):
1. Read Slides 5-16: Technical details
2. Set up and run all three examples
3. Review pitfalls and best practices
4. Study code patterns
5. Experiment with modifications
6. Choose approach for your project

## Running the Examples

### Pact Example

**Prerequisites**:
- .NET 8 SDK
- Port 9123 available

**Steps**:
```powershell
cd pact-contract-testing

# Terminal 1: Run Provider
cd Provider\src
dotnet restore
dotnet run

# Terminal 2: Run Consumer Tests
cd Consumer\tests
dotnet test

# Terminal 3: Verify Provider
cd Provider\tests
dotnet test
```

**Expected**: Consumer tests pass (5/5), generates pact file → Provider verification passes

### Postman Example

**Prerequisites**:
- .NET 8 SDK
- Newman CLI or Postman Desktop

**Steps**:
```powershell
cd postman-contract-testing

# Terminal 1: Run Provider
cd Provider\src
dotnet run

# Terminal 2: Run Consumer Tests
cd Consumer\tests
dotnet test

# Terminal 3: Run Postman Tests
cd ..
newman run postman-collections/Product-API-Contract-Tests.postman_collection.json `
  -e postman-collections/Product-API-Environment.postman_environment.json
```

**Expected**: All 3 endpoints tested, 10/11 assertions pass ✅

### OpenAPI Example (NEW!)

**Prerequisites**:
- Node.js 18+
- Docker & Docker Compose (or local Node.js)

**Steps**:
```powershell
cd openapi-contract-testing

# Option A: Docker Compose
docker-compose up -d
sleep 5

# Option B: Manual
cd provider
npm install
npm start

# Terminal 2: Run Consumer Tests
cd consumer
npm install
npm test
```

**Expected**: All 16 tests pass ✅ (7 v1 + 9 v2)

**Features**:
- Single OpenAPI spec for both v1 & v2
- SF 17.1 and SF 18.1 compatibility
- Schema validation
- Real-world versioning scenario

## Presentation Delivery Guide

### Recommended Format (75 minutes)

**Section 1: Introduction** (10 min)
- Slides 1-4: Problem statement & solutions landscape
- Engage: "Which approach fits your team?"

**Section 2: Solution Deep Dives** (20 min)
- Slides 5-10: Pact (consumer-driven)
- Slides 10-12: Postman (collection-based)
- Slides 10-13: OpenAPI (spec-driven)

**Section 3: Comparison & Decision** (15 min)
- Slide 11: Feature comparison matrix
- Slides 12-14: When to use each
- Slide 22: Decision framework

**Section 4: Demos** (20 min)
- Demo 1: Pact workflow (8 min)
- Demo 2: Postman workflow (6 min)
- Demo 3: OpenAPI workflow (6 min)

**Section 5: Wrap-up** (10 min)
- Slide 23: Implementation roadmap
- Slide 24: Key takeaways
- Q&A

### Alternative Formats

**Executive Briefing** (30 minutes):
- Slides 1-4, 11, 17, 22, 24
- Skip technical details
- Focus on ROI and decision criteria

**Technical Deep Dive** (120 minutes):
- Detailed exploration of all three
- Extended demo time (2-3 min each)
- Live coding/modification
- Architecture discussion

**Workshop Format** (4-5 hours):
- Include hands-on exercises
- Have attendees run examples
- Group decision-making
- Create action plan for team

### Three Approaches Summary

| Aspect | Pact | Postman | OpenAPI |
|--------|------|---------|---------|
| **Type** | Consumer-Driven | Collection-Based | Spec-Driven |
| **Best For** | Microservices | Quick Start | API Documentation |
| **Setup Time** | 1-2 weeks | 1-2 days | 3-5 days |
| **Learning Curve** | Steep | Gentle | Medium |
| **Mock Server** | Built-in | ❌ | Prism (OSS) |
| **Documentation** | No | Limited | Built-in ✅ |
| **Versioning** | Pact Broker | Manual | In spec |

## Customization Tips

### Adding Company Context
1. Replace generic examples with your services
2. Add your architecture diagram
3. Reference your actual pain points
4. Show your CI/CD setup

### Adjusting Technical Depth
- **Less technical**: Focus on principles, skip code
- **More technical**: Extended code walkthrough
- **Mixed audience**: Separate tracks or appendices

### Time Constraints
- **20 min**: Slides 1-4, 11, 22
- **45 min**: Add demos (quick overview each)
- **75 min**: Full core deck with complete demos
- **120+ min**: Deep dive with hands-on workshop

## FAQ for Presenters

**Q: Which tool should I recommend?**
A: Use Decision Framework (Slide 22):
- Start with **Postman** for quick wins
- Graduate to **OpenAPI** for governance
- Add **Pact** for critical service pairs

**Q: Should we use all three?**
A: **Hybrid approach recommended**:
- OpenAPI as specification source
- Pact for strategic contracts
- Postman for functional/exploratory testing

**Q: How do I handle mixed technical audiences?**
A: Three-track approach:
- Executive track: ROI, decision criteria
- Architecture track: Patterns, governance
- Implementation track: Hands-on coding

**Q: Live demos or backup recordings?**
A: Live for credibility, but have backups for:
- Network issues
- Port conflicts
- Setup problems
- Time overruns

## Post-Presentation Resources

**For Follow-up**:
- Share this repository
- Provide quick reference guide
- Offer office hours
- Create pilot project

**Metrics to Track** (if implementing):
- Time to first contract test
- Contract coverage %
- Bugs caught by tests
- Integration test reduction
- Developer satisfaction

## Version History

- **v2.0** (November 2025): Added OpenAPI approach
  - 28+ slide deck covering three approaches
  - New OpenAPI example (consumer + provider + tests)
  - Updated decision framework
  - Hybrid strategy guidance
  
- **v1.0** (November 2025): Initial materials
  - Pact and Postman examples
  - 26-slide comparison deck
  - Quick reference guide

---

**Last Updated**: November 26, 2025  
**Presentation Author**: Generated for Contract Testing Repository  
**License**: Same as main repository
