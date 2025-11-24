# Contract Testing: Quick Reference Guide

## Tool Comparison Matrix

| Aspect | Pact | Postman |
|--------|------|---------|
| **Philosophy** | Consumer-driven | API-first validation |
| **Primary Use** | Service-to-service contracts | API validation & testing |
| **Mock Server** | ✅ Built-in | ❌ Requires paid plan or manual setup |
| **Setup Time** | 2-4 hours | 15-30 minutes |
| **Learning Curve** | 2-4 weeks | 1-3 days |
| **Maintenance Effort** | Medium | Low-Medium |
| **Team Size Sweet Spot** | 10+ engineers | 2-10 engineers |
| **Service Count** | 5+ services | 1-5 services |
| **Cost** | Free (OSS), PactFlow $$ | Free-$$$ (team features) |
| **CI/CD Ready** | ✅ Excellent | ✅ Excellent |
| **Version Management** | ✅ Pact Broker | Manual/Git |
| **Provider States** | ✅ Native | ⚠️ Manual implementation |
| **Documentation** | Excellent | Excellent |
| **Community** | Strong (specialized) | Very strong (general) |

## When to Choose What

### Choose Pact When:
- ✅ Multiple microservices (5+)
- ✅ Multiple consumer teams per provider
- ✅ Parallel development needed
- ✅ Frequent API changes
- ✅ Strong CI/CD culture
- ✅ Need mock servers for consumer testing

### Choose Postman When:
- ✅ Simpler architecture (<5 services)
- ✅ Quick start needed
- ✅ Team already uses Postman
- ✅ Provider-driven development
- ✅ Testing external/third-party APIs
- ✅ Combined functional + contract testing

### Use Both When:
- 🤔 Core services use Pact
- 🤔 Exploratory/manual testing uses Postman
- 🤔 External API validation uses Postman
- 🤔 Different teams have different needs

## Feature Comparison

### Contract Generation
| Feature | Pact | Postman |
|---------|------|---------|
| Auto-generation from code | ✅ | ❌ |
| Manual definition | ⚠️ Possible | ✅ |
| Schema-based | ⚠️ Via plugins | ✅ |
| Example-based | ✅ | ✅ |

### Testing Capabilities
| Capability | Pact | Postman |
|------------|------|---------|
| Contract validation | ✅ ✅ ✅ | ✅ ✅ |
| Consumer isolation | ✅ ✅ ✅ | ❌ |
| Provider states | ✅ ✅ ✅ | ⚠️ |
| Dynamic data (auth) | ✅ ✅ | ⚠️ |
| Version compatibility | ✅ ✅ ✅ | ❌ |
| Functional testing | ⚠️ | ✅ ✅ ✅ |
| Performance testing | ❌ | ✅ ✅ |
| Load testing | ❌ | ✅ (via monitors) |

### Integration & Automation
| Feature | Pact | Postman |
|---------|------|---------|
| CLI support | ✅ | ✅ (Newman) |
| CI/CD integration | ✅ ✅ ✅ | ✅ ✅ |
| Git integration | ✅ | ✅ |
| Webhook triggers | ✅ (Broker) | ✅ (Monitors) |
| Scheduled runs | ⚠️ Via CI | ✅ |
| Result publishing | ✅ (Broker) | ✅ (Cloud) |

## Common Pitfalls

### Pact Pitfalls
1. **Port conflicts** → Check with `Get-NetTCPConnection`, change ports
2. **Native DLL missing** → Add `CopyLocalLockFileAssemblies`
3. **401 errors** → Implement request filters for auth
4. **Stale contracts** → Regenerate after consumer changes
5. **Over-specification** → Use matchers, not exact values
6. **Missing provider states** → Define states for all scenarios

### Postman Pitfalls
1. **Manual sync issues** → Version collections in Git
2. **False confidence** → Validates running service only
3. **Brittle tests** → Use variables, avoid hardcoding
4. **Incomplete coverage** → Test error scenarios too
5. **No isolation** → Can't test consumer independently
6. **Contract drift** → Automate runs, keep collections updated

## Setup Checklist

### Pact Setup
- [ ] Install PactNet NuGet package
- [ ] Configure PactBuilder with mock server port
- [ ] Write consumer tests with expectations
- [ ] Generate pact files
- [ ] Set up Pact Broker (optional but recommended)
- [ ] Configure provider verification tests
- [ ] Implement provider states middleware
- [ ] Add request filters for dynamic data
- [ ] Integrate with CI/CD pipeline
- [ ] Train team on workflow

### Postman Setup
- [ ] Install Postman desktop/web
- [ ] Install Newman CLI (`npm install -g newman`)
- [ ] Create collection with API requests
- [ ] Add test scripts for validation
- [ ] Create environment files
- [ ] Version collections in Git
- [ ] Integrate Newman in CI/CD
- [ ] Document expected contracts
- [ ] Share collections with team
- [ ] Set up monitors (optional)

## Cost of Ownership

### Pact
**Initial Investment**:
- Setup: 2-4 hours per service pair
- Learning: 2-4 weeks per team
- Infrastructure: Pact Broker setup (1-2 days)
- Training: 1-2 days per team

**Ongoing**:
- Maintenance: 2-4 hours/week
- Contract updates: As needed
- Provider state management: Ongoing
- Broker maintenance: 1-2 hours/month

**Total (Year 1)**: ~80-120 hours for 3 service pairs

### Postman
**Initial Investment**:
- Setup: 2-4 hours per service
- Learning: 1-3 days per team
- Collection creation: 1-2 days
- CI/CD integration: 1 day

**Ongoing**:
- Maintenance: 1-2 hours/week
- Collection updates: As needed
- Test script updates: Ongoing

**Total (Year 1)**: ~30-50 hours for 3 services

## ROI Comparison

### Pact ROI
**Time to Value**: 2-3 months
**Break-even**: 6-12 months (for teams with 5+ services)
**Long-term Savings**: 
- 70-90% reduction in integration bugs
- 40-60% faster development cycles
- 50-70% less time in integration testing

### Postman ROI
**Time to Value**: Immediate
**Break-even**: 1-3 months
**Long-term Savings**:
- 30-50% reduction in manual testing time
- Quick validation of API changes
- Combined testing approach efficiency

## Migration Strategies

### Postman → Pact
**Phased Approach**:
1. **Month 1**: Keep Postman, add Pact for 1-2 critical pairs
2. **Month 2**: Expand Pact to 3-5 service pairs
3. **Month 3**: Set up Pact Broker
4. **Month 4-6**: Train remaining teams
5. **Month 6+**: Pact as default, Postman for exploratory

**Risk Mitigation**:
- Run both tools in parallel initially
- Document patterns and learnings
- Provide dedicated support
- Create templates and examples

### Pact → Postman
**Simplification Approach**:
1. **Week 1**: Audit current Pact usage
2. **Week 2**: Create Postman collections from contracts
3. **Week 3**: Add validation scripts
4. **Week 4**: Integrate Newman in CI
5. **Month 2**: Phase out Pact infrastructure

**Considerations**:
- Loss of consumer isolation
- Manual contract synchronization
- Provider state management

## Command Reference

### Pact Commands
```powershell
# Run consumer tests (generates pact)
dotnet test Consumer/tests

# Run provider verification
dotnet test Provider/tests

# Publish to broker (if using)
pact-broker publish pacts/*.json \
  --consumer-app-version=$VERSION \
  --broker-base-url=$PACT_BROKER_URL

# Can I deploy?
pact-broker can-i-deploy \
  --pacticipant=Consumer \
  --version=$VERSION
```

### Postman/Newman Commands
```powershell
# Run collection
newman run collection.json -e environment.json

# With reporters
newman run collection.json \
  -e environment.json \
  --reporters cli,html,junit \
  --reporter-html-export report.html \
  --reporter-junit-export results.xml

# With variables
newman run collection.json \
  --env-var "baseUrl=http://localhost:5001"

# Folder-specific
newman run collection.json \
  --folder "Contract Tests"
```

## Testing Patterns

### Pact Patterns
```csharp
// Pattern 1: Type matching
.WithJsonBody(new TypeMatcher(expectedObject))

// Pattern 2: Regex matching
.WithHeader("Authorization", Match.Regex("Bearer ...", pattern))

// Pattern 3: Provider states
.Given("product with ID 10 exists")

// Pattern 4: Request filtering
public class AuthTokenRequestFilter : IMiddleware
{
    // Replace auth tokens with valid ones during testing
}
```

### Postman Patterns
```javascript
// Pattern 1: Schema validation
pm.expect(tv4.validate(jsonData, schema)).to.be.true;

// Pattern 2: Type checking
pm.expect(product.id).to.be.a('number');

// Pattern 3: Collection variables
const baseUrl = pm.collectionVariables.get("baseUrl");

// Pattern 4: Dynamic tests
const fields = ['id', 'name', 'type'];
fields.forEach(field => {
    pm.test(`Has ${field}`, () => {
        pm.expect(product).to.have.property(field);
    });
});
```

## Metrics to Track

### Pact Metrics
- Contract coverage (% of interactions)
- Contract violations caught
- Time to detect breaking changes
- Provider verification pass rate
- Consumer test execution time

### Postman Metrics
- Collection execution time
- Test pass rate
- Contract violations found
- API response time trends
- Test maintenance overhead

## Support & Resources

### Pact
- **Docs**: https://docs.pact.io
- **Slack**: pact-foundation.slack.com
- **GitHub**: github.com/pact-foundation
- **Stack Overflow**: Tag `pact`

### Postman
- **Learning Center**: learning.postman.com
- **Community**: community.postman.com
- **Stack Overflow**: Tag `postman`
- **Support**: support.postman.com

---

**Last Updated**: November 2025
**Version**: 1.0
**Repository**: pact-workshop-dotnet-master
