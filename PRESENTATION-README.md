# Contract Testing Presentation Materials

## Overview
This directory contains complete presentation materials comparing Pact and Postman approaches to contract testing, with working examples of both.

## Contents

### 📊 Presentation
- **`PRESENTATION-CONTRACT-TESTING.md`**: Complete slide deck (28 slides + backup)
  - Introduction to contract testing
  - Detailed comparison: Pact vs Postman
  - Real-world examples and use cases
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
Consumer/
├── src/ApiClient.cs           # HTTP client implementation
└── tests/ApiTest.cs           # Pact consumer tests (generates contracts)

Provider/
├── src/
│   ├── ProductsController.cs  # API implementation
│   └── Startup.cs             # Middleware configuration
└── tests/
    ├── ProductTest.cs         # Pact provider verification
    └── Middleware/
        ├── ProviderStateMiddleware.cs      # Test data setup
        └── AuthTokenRequestFilter.cs       # Auth token handling

pacts/
└── ApiClient-ProductService.json  # Generated contract
```

#### Postman Example
```
postman-contract-testing/
├── Provider/src/              # Same API, simplified version
├── Consumer/src/              # Same client, simplified version
└── postman-collections/
    ├── Product-API-Contract-Tests.postman_collection.json
    └── Product-API-Environment.postman_environment.json
```

### 📖 Documentation
- **Consumer tests fix steps**: `Consumer/tests/consumer-tests-fix-steps.md`
- **Postman setup guide**: `postman-contract-testing/README.md`
- **Workshop learning objectives**: `LEARNING.md`

## How to Use This Presentation

### For Presenters

1. **Review the Materials** (30 minutes)
   - Read through `PRESENTATION-CONTRACT-TESTING.md`
   - Familiarize yourself with both examples
   - Review the decision framework (Slide 22)

2. **Customize for Your Audience** (1 hour)
   - Add company-specific context
   - Adjust technical depth
   - Select relevant slides (core deck is 26 slides)
   - Prepare demos (see below)

3. **Prepare Demos** (1 hour)
   - Set up both Pact and Postman examples
   - Practice running tests
   - Prepare breakage scenarios to demonstrate failures

### For Decision Makers

**Quick Path** (15 minutes):
1. Read Slides 1-4: Introduction & context
2. Read Slide 10: Side-by-side comparison
3. Read Slide 22: Decision framework
4. Read `CONTRACT-TESTING-QUICK-REFERENCE.md`: ROI section

**Deep Dive** (2 hours):
1. Read full presentation
2. Run both demos
3. Review cost-benefit analysis (Slide 17)
4. Consider implementation roadmap (Slide 23)

### For Engineers

**Implementation Path** (4-6 hours):
1. Read Slides 5-9: Technical details
2. Set up and run Pact example
3. Set up and run Postman example
4. Review pitfalls (Slide 14)
5. Study code patterns in examples
6. Experiment with modifications

## Running the Examples

### Pact Example

**Prerequisites**:
- .NET 8 SDK
- Port 9123 available (or modify in `ApiTest.cs`)

**Steps**:
```powershell
# Terminal 1: Run Provider
cd Provider\src
dotnet restore
dotnet run

# Terminal 2: Run Consumer Tests (generates pact)
cd Consumer\tests
dotnet test

# Terminal 3: Verify Provider
cd Provider\tests
dotnet test
```

**Expected Output**:
- Consumer tests: ✅ 5 passed, generates pact file
- Provider tests: ✅ 1 passed (5 interactions verified)

**Troubleshooting**: See `Consumer/tests/consumer-tests-fix-steps.md`

### Postman Example

**Prerequisites**:
- .NET 8 SDK
- Postman Desktop OR Newman CLI (`npm install -g newman`)

**Steps**:
```powershell
# Start Provider
cd postman-contract-testing\Provider\src
dotnet restore
dotnet run

# Option A: Postman GUI
# 1. Import collection from postman-collections/
# 2. Import environment
# 3. Run collection

# Option B: Newman CLI
cd postman-contract-testing
newman run postman-collections/Product-API-Contract-Tests.postman_collection.json `
  -e postman-collections/Product-API-Environment.postman_environment.json
```

**Expected Output**:
- 3 test scenarios, all passing
- Response time metrics
- HTML report (if using `--reporter-html-export`)

## Presentation Delivery Guide

### Recommended Format (60 minutes)

**Section 1: Introduction** (10 min)
- Slides 1-4: Problem statement & contract testing overview
- Engage audience: "Who has integration test pain?"

**Section 2: Solutions** (15 min)
- Slides 5-9: Pact deep dive
- Slides 8-9: Postman deep dive
- Show code examples from slides

**Section 3: Comparison** (10 min)
- Slide 10: Feature comparison
- Slides 11-12: When to use each
- Slide 22: Decision framework

**Section 4: Demo** (15 min)
- Live demo: Pact workflow (10 min)
- Live demo: Postman workflow (5 min)
- Show breaking changes and failures

**Section 5: Wrap-up** (10 min)
- Slide 23: Implementation roadmap
- Slide 24: Key takeaways
- Q&A

### Alternative Formats

**Workshop Format** (3-4 hours):
- Include hands-on exercises
- Have attendees run examples
- Group discussion on team's specific needs
- Create action plan

**Executive Briefing** (30 minutes):
- Slides 1-4, 10, 17, 22, 24 only
- Focus on ROI and decision criteria
- Skip technical details

**Technical Deep Dive** (90 minutes):
- Focus on Slides 5-16
- Extended demo time
- Live coding session
- Architecture patterns discussion

## Customization Tips

### Adding Company Context
1. Replace generic examples with your services
2. Add your service architecture diagram
3. Include actual pain points from your team
4. Reference your CI/CD setup

### Adjusting Technical Depth
- **Less technical**: Focus on "What" and "Why", skip code details
- **More technical**: Add backup slides, detailed code walkthrough
- **Mixed audience**: Create separate tracks or appendices

### Time Constraints
- **15 min**: Slides 1-4, 10, 22, 24
- **30 min**: Add 17, 23, brief demo
- **45 min**: Full core deck, short demo
- **60 min**: Full deck with demos
- **90+ min**: Workshop format with hands-on

## FAQ for Presenters

**Q: Which tool should I recommend?**
A: Use the decision framework (Slide 22). For most teams starting out: Postman. For mature microservices: Pact.

**Q: What if my organization already uses [other tool]?**
A: Acknowledge it, compare to Pact/Postman, focus on contract testing principles that apply universally.

**Q: How do I handle pushback on learning curve?**
A: Emphasize ROI timeline (Slide 17), start small approach (Slide 23), and hybrid option (Slide 22).

**Q: Should I demo live or use screenshots?**
A: Live for credibility, but have backup recordings/screenshots in case of technical issues.

**Q: What if someone asks about GraphQL/gRPC?**
A: Both Pact and Postman support these (mention plugins/setup differences), but keep focus on HTTP for clarity.

## Post-Presentation Resources

**For Follow-up**:
- Share this repository link
- Provide quick reference guide
- Offer office hours or Slack channel
- Create pilot project team

**Metrics to Track** (if implementing):
- Time to first contract test
- Contract coverage over time
- Bugs caught by contract tests
- Integration test reduction
- Developer satisfaction

## Contributing

Found an issue or have suggestions?
- Update presentation materials
- Add more examples
- Share success stories
- Improve documentation

## Version History

- **v1.0** (November 2025): Initial presentation materials
  - 28-slide deck with comparisons
  - Working Pact and Postman examples
  - Quick reference guide
  - Comprehensive documentation

---

**Presentation Author**: Generated for pact-workshop-dotnet  
**Last Updated**: November 24, 2025  
**License**: Same as main repository
