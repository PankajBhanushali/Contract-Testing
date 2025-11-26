---
marp: true
theme: default
class: lead
paginate: true
backgroundColor: #fff
backgroundImage: url('data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><rect fill="%23f0f0f0" width="100" height="100"/></svg>')
---

<!-- _class: lead -->
# Contract Testing
## Pact vs Postman vs OpenAPI

### A Comprehensive Comparison of Three Approaches

---

## What is Contract Testing?

- **Definition**: Testing the interface (contract) between services to ensure they can communicate correctly
- **Purpose**: Catch integration issues early without running full integration tests
- **Analogy**: Like a legal contract - both parties agree on terms before doing business

### The Problem We're Solving

| Traditional Approach | Contract Testing |
|---|---|
| Slow and brittle | Fast and focused |
| Require all services running | Isolated validation |
| Hard to maintain | Version controlled |
| Test too much | Test what matters |

---

## Testing Pyramid & Contract Testing

```
        /\
       /  \      E2E Tests (Slow, Brittle)
      /    \
     /------\    Integration Tests
    /        \   ← CONTRACT TESTS ← Fast, Focused
   /----------\  Unit Tests (Fast, Many)
  /____________\
```

**Contract Testing Sweet Spot**
- ✅ Faster than integration tests
- ✅ More confidence than unit tests
- ✅ Validates real API interactions
- ✅ Runs in isolation

---

## Contract Testing Solutions Landscape

### Consumer-Driven Contracts
- **Pact** (Most popular, multi-language)
- Spring Cloud Contract
- Specmatic

### API-First / Specification-Based
- **OpenAPI/Swagger validation**
- JSON Schema validators
- Dredd, Prism, Schemathesis

### Collection-Based Testing
- **Postman Contract Testing**
- Manual test scripts

---

## Pact: Consumer-Driven Contracts

### Core Philosophy
> "The consumer defines what they need from the provider"

### How It Works
1. Consumer writes tests describing expected interactions
2. Pact file generated (JSON contract)
3. Provider verifies they can satisfy the contract
4. Pact Broker manages contract versions and verification

### Key Benefits
- ✅ Consumer and provider develop in parallel
- ✅ Contracts generated from real code
- ✅ Built-in mock server for consumer testing
- ✅ Version compatibility checking
- ✅ Provider states for different scenarios

---

## Pact Workflow - Visual

```
┌──────────────┐                  ┌──────────────┐
│  CONSUMER    │                  │  PROVIDER    │
└──────────────┘                  └──────────────┘
       │                                 │
       │ 1. Write Pact test              │
       │    (expectations)               │
       ▼                                 │
  ┌─────────┐                           │
  │ Pact    │ 2. Generate contract      │
  │ File    │─────────────────────────▶ │
  │ (JSON)  │                           │
  └─────────┘                           │
       │                        3. Verify contract
       │                        4. Run tests
       │                                 │
       │                        ┌────────▼──────┐
       │                        │ ✓ PASS / ✗ FAIL
       │◀───────────────────────│
       │    5. Results          └────────────────┘
       │
  ┌────▼──────────┐
  │  Pact Broker  │
  │ (Management)  │
  └───────────────┘
```

---

## Pact Example - Consumer Test

```csharp
[Fact]
public async Task GetProduct()
{
    pact.UponReceiving("A valid request for a product")
        .Given("product with ID 10 exists")
        .WithRequest(HttpMethod.Get, "/api/products/10")
        .WithHeader("Authorization", Match.Regex("Bearer ..."))
    .WillRespond()
        .WithStatus(HttpStatusCode.OK)
        .WithJsonBody(new TypeMatcher(expectedProduct));

    await pact.VerifyAsync(async ctx => {
        var response = await ApiClient.GetProduct(10);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    });
}
```

**Output**: Pact contract file with specifications

---

## Pact Example - Provider Verification

```csharp
[Fact]
public void EnsureProviderHonoursPact()
{
    using (var webHost = WebHost.CreateDefaultBuilder()
        .UseStartup<TestStartup>()
        .Build())
    {
        webHost.Start();

        var verifier = new PactVerifier();
        verifier
            .WithHttpEndpoint(new Uri(serviceUri))
            .WithFileSource(pactFile)
            .WithProviderStateUrl(stateUri)
            .Verify();
    }
}
```

**Key Features**:
- ✅ Provider states (data setup)
- ✅ Request filters (auth tokens)
- ✅ Automatic verification

---

## Postman Contract Testing

### Approach
> "Define and validate API contracts through comprehensive test scripts"

### How It Works
1. Create Postman collection with contract expectations
2. Write test scripts to validate response structure
3. Share collection with team/CI pipeline
4. Run with Newman CLI in automation
5. Validate against running API

### Key Benefits
- ✅ No special frameworks needed
- ✅ Visual interface for debugging
- ✅ Team already knows Postman
- ✅ Can test running services
- ✅ Includes API documentation

---

## OpenAPI: Specification-Driven Validation

### Approach
> "Define contract as OpenAPI spec, validate both consumer and provider against it"

### How It Works
1. Create OpenAPI specification (openapi.yaml)
2. Consumer validates requests/responses against spec
3. Provider generates responses per spec
4. Automated testing tools (Dredd, Prism, Schemathesis)
5. Single source of truth for entire contract

### Key Benefits
- ✅ Single source of truth (OpenAPI spec)
- ✅ Built-in API documentation
- ✅ Multi-consumer support
- ✅ Schema validation built-in
- ✅ Mock servers (Prism)
- ✅ API versioning support

---

## Postman Contract Test Example

```javascript
// Get Product by ID - Contract Test
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Product has required fields", function () {
    const product = pm.response.json();
    pm.expect(product).to.have.property('id').that.is.a('number');
    pm.expect(product).to.have.property('name').that.is.a('string');
    pm.expect(product.id).to.equal(10);
});
```

**Run with Newman**:
```bash
newman run contract-tests.json -e environment.json
```

---

## OpenAPI Contract Test Example

```yaml
# openapi.yaml - Single source of truth
openapi: 3.0.0
info:
  title: Users API
  version: 1.0.0

paths:
  /users:
    get:
      parameters:
        - name: apiVersion
          in: query
          schema:
            type: string
            enum: ["1", "2"]
      responses:
        "200":
          description: Users list
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsersResponse"
```

**Validate with Dredd**:
```bash
dredd openapi.yaml http://localhost:5001
# Tests: 16 passing ✅
```

---

## Side-by-Side Comparison

| Feature | Pact | Postman | OpenAPI |
|---------|------|---------|---------|
| Setup Complexity | Medium-High | Low | Medium |
| Learning Curve | Steep | Gentle | Medium |
| Mock Server | ✅ Built-in | ❌ No | ✅ Prism |
| Provider States | ✅ Built-in | ❌ Manual | ❌ Manual |
| Contract Auto-Gen | ✅ Yes | ❌ No | ❌ No |
| Pact Broker | ✅ Yes | ⚠️  Manual | ✅ In spec |
| CI/CD Integration | ✅ Excellent | ✅ Excellent | ✅ Excellent |
| Multi-language | ✅ Yes | ✅ Any HTTP | ✅ Any HTTP |
| API Documentation | ❌ No | ⚠️  Limited | ✅ Built-in |
| Debugging | ⚠️  CLI/Logs | ✅ Visual UI | ⚠️  CLI/Visual |
| Team Familiarity | ❌ Specialized | ✅ Common | ⚠️  Dev-friendly |
| Open Source | ✅ Fully OSS | ⚠️  Freemium | ✅ Fully OSS |

---

## When to Use Pact

### ✅ Ideal Scenarios
- Microservices architecture with many dependencies
- Multiple consumer teams consuming same provider
- Parallel development needed
- Frequent API changes requiring version compatibility
- Strong contract governance across teams
- Mature DevOps with CI/CD automation

### 🎯 Real-World Use Cases
- E-commerce platforms (20+ microservices)
- Mobile app + backend API development
- Third-party API integrations
- Platform-as-a-Service offerings

---

## When to Use Postman

### ✅ Ideal Scenarios
- Simpler architectures with fewer dependencies
- API-first development with OpenAPI specs
- Quick start needed - fast results required
- Limited technical resources for setup
- Existing Postman adoption in organization
- Combined testing needs (functional + contract + performance)

### 🎯 Real-World Use Cases
- Monolith transitioning to microservices
- External partner API validation
- QA team-led testing initiatives
- Smaller teams (2-5 services)

---

## When to Use OpenAPI

### ✅ Ideal Scenarios
- API-first development with existing specs
- Multiple consumer teams using same API
- Need comprehensive API documentation
- Want built-in mock servers (Prism)
- Legacy REST API systems
- API versioning is important

### 🎯 Real-World Use Cases
- Public APIs with multiple consumers
- Microservices platforms with REST APIs
- API versioning (v1/v2) strategies
- Organizations with API governance
- DevOps automating API validation

---

## Decision Framework

### Choose Pact If:
- ✅ You have >5 microservices
- ✅ Multiple teams consume same APIs
- ✅ Frequent API changes cause issues
- ✅ Need parallel development
- ✅ Strong DevOps/CI culture
- ✅ Budget for learning curve

### Choose Postman If:
- ✅ <5 services or simpler architecture
- ✅ Quick start is priority
- ✅ Team already uses Postman
- ✅ Combined testing approach desired
- ✅ Limited technical resources
- ✅ Provider-driven development

### Choose OpenAPI If:
- ✅ API-first development
- ✅ Need built-in documentation
- ✅ Want mock servers (Prism)
- ✅ Multiple consumer versions
- ✅ Need spec-driven governance
- ✅ Prefer specification over code

---

## Hybrid Approach (Use All Three!)

### Recommended Strategy
- **OpenAPI**: Specification (source of truth)
- **Pact**: Core service contracts (critical paths)
- **Postman**: Exploratory & functional testing

### Benefits
- ✅ Flexibility - each tool where it excels
- ✅ Governance - spec-driven compliance
- ✅ Team choice - different preferences
- ✅ Progressive - migrate gradually

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1-2)
- [ ] Choose tool based on decision framework
- [ ] Set up infrastructure
- [ ] Train pilot team
- [ ] Select 1-2 service pairs for POC

### Phase 2: Pilot (Week 3-6)
- [ ] Implement contracts for pilot services
- [ ] Integrate with CI/CD
- [ ] Document patterns
- [ ] Measure impact

### Phase 3: Scale (Month 2-3)
- [ ] Roll out to additional teams
- [ ] Establish governance
- [ ] Create templates

### Phase 4: Optimize (Month 3+)
- [ ] Refine based on feedback
- [ ] Track metrics
- [ ] Continuous improvement

---

## Key Takeaways

### Contract Testing is Essential
1. Bridges gap between unit and integration tests
2. Enables parallel development across teams
3. Catches issues early before expensive failures
4. Reduces test brittleness vs E2E tests

### Tool Selection Matters
- **Pact**: Best for mature microservices
- **Postman**: Best for simpler setups or quick wins
- **Context is key**: No one-size-fits-all solution

### Success Factors
- ✅ Team buy-in and training
- ✅ CI/CD integration
- ✅ Clear governance
- ✅ Start small, scale gradually

---

## Resources & Next Steps

### Pact Resources
- **Docs**: https://docs.pact.io
- **Workshop**: This repository
- **Community**: Slack, GitHub
- **PactFlow**: https://pactflow.io

### Postman Resources
- **Docs**: https://learning.postman.com
- **Newman**: https://www.npmjs.com/package/newman
- **Example**: Repository examples
- **Community**: Postman forums

### OpenAPI Resources
- **Docs**: https://spec.openapis.org
- **Prism**: https://stoplight.io/prism/
- **Dredd**: https://dredd.org/
- **Example**: Repository examples

---

## Getting Started

### 1. Explore Examples
- Review Pact implementation in repository
- Review Postman implementation
- Run demos locally

### 2. Choose Your Approach
- Evaluate your architecture
- Match against decision framework
- Get team buy-in

### 3. Start Small
- Pick one service pair
- Implement contracts
- Measure results

### 4. Scale & Iterate
- Expand to more services
- Refine processes
- Share learnings

---

## Q&A Discussion Topics

**Common Questions**:

1. **"Do we need Pact Broker or can we use Git?"**
   - Git works for small teams; Broker enables scale

2. **"Can Pact test performance?"**
   - No - validates structure only, not performance

3. **"What about GraphQL/gRPC?"**
   - Both support with plugins/setup

4. **"How do we version contracts?"**
   - Pact Broker handles this; Postman needs Git/tagging

5. **"Testing third-party APIs?"**
   - Postman better suited; Pact for internal APIs

---

## Demo Time!

### Live Demonstrations

**Demo 1: Pact Workflow** (10 min)
1. Consumer test writing
2. Generate pact file
3. Provider verification
4. Breaking changes & failures

**Demo 2: Postman Workflow** (5 min)
1. Import collection
2. Run tests in UI
3. Newman CLI execution
4. View results

**Demo 3: Debugging** (5 min)
- Troubleshoot common issues
- Port conflicts, missing headers

---

<!-- _class: lead -->

# Questions?

### Contact & Feedback
**Ready to try it?**
Clone the repository and follow the README

**Need help?**
Reach out for implementation guidance

---

<!-- _class: lead -->

# Thank You!

### Repository Contents
- ✅ Working Pact example (Consumer + Provider)
- ✅ Working Postman example (Consumer + Provider)
- ✅ Working OpenAPI example (Consumer + Provider + Spec)
- ✅ Troubleshooting documentation
- ✅ Comparison matrices
- ✅ This presentation

**Clone it**:
```bash
git clone https://github.com/PankajBhanushali/Contract-Testing.git
cd "Contract Testing"
```