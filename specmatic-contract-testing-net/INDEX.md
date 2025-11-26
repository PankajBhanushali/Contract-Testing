# File Index

Navigation guide for the Specmatic .NET Contract Testing project.

## Documentation Files

| File | Purpose | Audience |
|------|---------|----------|
| **00-START-HERE.md** | Quick overview & first steps | Everyone - START HERE |
| **QUICK-START.md** | 5-minute setup guide | Developers |
| **README.md** | Complete project documentation | Anyone wanting details |
| **ARCHITECTURE.md** | Design patterns & technical deep-dive | Architects, Senior devs |
| **INDEX.md** | This file - navigation guide | When lost |

## Specification

| File | Purpose |
|------|---------|
| **specs/products-api.yaml** | OpenAPI 3.0 specification - THE CONTRACT |

**How to read**: Start with paths, then scroll to components/schemas

## Provider (ASP.NET Core API)

| File | Purpose | Lines |
|------|---------|-------|
| **Provider/provider.csproj** | Project file, dependencies | 18 |
| **Provider/appsettings.json** | Configuration | 10 |
| **Provider/src/Program.cs** | Main application, endpoints | 200+ |
| **Provider/src/Models.cs** | Data models & DTOs | 60 |
| **Provider/src/ProductRepository.cs** | Data access layer | 110 |

**How to read**:
1. Start with `Program.cs` - see endpoints
2. Look at `Models.cs` - understand data structures
3. Check `ProductRepository.cs` - understand data flow

## Consumer (Test Client)

| File | Purpose | Lines |
|------|---------|-------|
| **Consumer/consumer.csproj** | Project file, test dependencies | 18 |
| **Consumer/src/Models.cs** | Shared models | 50 |
| **Consumer/src/ProductApiClient.cs** | HTTP client | 130 |
| **Consumer/tests/ProductApiContractTests.cs** | xUnit contract tests | 300+ |

**How to read**:
1. Start with `ProductApiClient.cs` - understand how to use API
2. Look at `ProductApiContractTests.cs` - understand what tests do
3. Read test names to understand coverage

## Configuration & Deployment

| File | Purpose |
|------|---------|
| **docker-compose.yml** | Local deployment with Docker |
| **Dockerfile** | Provider container image |
| **specmatic.yaml** | Specmatic tool configuration |

**How to use**:
- `docker-compose.yml`: `docker-compose up --build`
- `Dockerfile`: Used by docker-compose
- `specmatic.yaml`: For Specmatic CLI tool

## Quick Reference

### I want to...

**Understand the project**
→ Read: `00-START-HERE.md`

**Get it running quickly**
→ Read: `QUICK-START.md`

**Understand the spec**
→ Look at: `specs/products-api.yaml`

**See the API implementation**
→ Look at: `Provider/src/Program.cs`

**Understand the tests**
→ Look at: `Consumer/tests/ProductApiContractTests.cs`

**Understand design patterns**
→ Read: `ARCHITECTURE.md`

**Deploy with Docker**
→ Use: `docker-compose.yml`

**Learn the complete story**
→ Read: `README.md`

### By Role

**API Developer (Provider)**
1. Read `QUICK-START.md`
2. Look at `Provider/src/Program.cs`
3. Update `ProductRepository.cs` for real data
4. Implement missing endpoints

**Test Developer (Consumer)**
1. Read `QUICK-START.md`
2. Look at `Consumer/src/ProductApiClient.cs`
3. Look at `Consumer/tests/ProductApiContractTests.cs`
4. Add more tests as spec evolves

**Architect/Tech Lead**
1. Read `README.md` (overview)
2. Read `ARCHITECTURE.md` (design)
3. Review `specmatic.yaml` (configuration)
4. Plan integration with CI/CD

**DevOps/Platform Engineer**
1. Read `QUICK-START.md` (setup)
2. Use `docker-compose.yml` (local)
3. Use `Dockerfile` (production)
4. Integrate into CI/CD pipeline

## File Statistics

```
Total Files: 15

Documentation: 5 files
├── 00-START-HERE.md        (quick overview)
├── QUICK-START.md          (setup guide)
├── README.md               (complete guide)
├── ARCHITECTURE.md         (design patterns)
└── INDEX.md                (this file)

Specification: 1 file
└── specs/products-api.yaml (OpenAPI contract)

Provider: 5 files
├── provider.csproj         (project file)
├── appsettings.json        (config)
├── src/Program.cs          (endpoints & startup)
├── src/Models.cs           (data models)
└── src/ProductRepository.cs (data access)

Consumer: 4 files
├── consumer.csproj         (project file)
├── src/Models.cs           (shared models)
├── src/ProductApiClient.cs (HTTP client)
└── tests/ProductApiContractTests.cs (tests)

Deployment: 3 files
├── Dockerfile              (provider image)
├── docker-compose.yml      (local deployment)
└── specmatic.yaml          (tool config)

Lines of Code:
├── Documentation: ~2,500 lines
├── Provider: ~370 lines
├── Consumer: ~350 lines
└── Config: ~100 lines
```

## Learning Path

### Beginner (30 minutes)
1. `00-START-HERE.md` - Overview
2. `QUICK-START.md` - Get running
3. Run example: `dotnet run` + `dotnet test`

### Intermediate (1 hour)
1. `README.md` - Complete picture
2. `specs/products-api.yaml` - Understand spec
3. `Consumer/tests/ProductApiContractTests.cs` - See tests
4. Make one spec change + retest

### Advanced (2 hours)
1. `ARCHITECTURE.md` - Design deep-dive
2. `Provider/src/Program.cs` - Implementation details
3. `Consumer/src/ProductApiClient.cs` - Client details
4. `docker-compose.yml` + Docker setup
5. Plan CI/CD integration

### Expert (4+ hours)
1. All documentation
2. All code
3. Implement custom features:
   - Add database (EF Core)
   - Add authentication
   - Add logging
   - Integrate into CI/CD
   - Deploy to Kubernetes

## File Sizes (Approximate)

```
Small (<500 lines):
├── appsettings.json
├── provider.csproj
├── consumer.csproj
├── Dockerfile
├── specmatic.yaml
└── docker-compose.yml

Medium (500-1000 lines):
├── Models.cs (Provider)
├── Models.cs (Consumer)
└── ProductRepository.cs

Large (1000+ lines):
├── 00-START-HERE.md
├── QUICK-START.md
├── README.md
├── ARCHITECTURE.md
├── Program.cs (Provider)
└── ProductApiContractTests.cs

Longest:
├── README.md (~1,200 lines)
├── ARCHITECTURE.md (~800 lines)
└── ProductApiContractTests.cs (~300 lines)
```

## Dependency Map

```
consumer.csproj
├─ xunit
├─ FluentAssertions
└─ Microsoft.NET.Test.Sdk

provider.csproj
├─ Swashbuckle.AspNetCore
└─ Microsoft.AspNetCore.OpenApi

Shared:
├─ .NET 8.0 SDK
└─ Docker (optional)
```

## Common Commands

```bash
# Provider
cd Provider
dotnet restore           # Install dependencies
dotnet build             # Compile
dotnet run              # Run locally (port 5000)
dotnet clean            # Clean build artifacts

# Consumer
cd Consumer
dotnet restore          # Install dependencies
dotnet build            # Compile
dotnet test             # Run all tests
dotnet test --filter "ClassName=..." # Run specific test

# Docker
docker-compose build    # Build images
docker-compose up       # Run containers
docker-compose down     # Stop containers
docker-compose logs     # View logs
```

## Key Concepts

**Specification (OpenAPI)**
- Single source of truth
- Location: `specs/products-api.yaml`
- Defines: endpoints, schemas, status codes

**Provider (ASP.NET Core)**
- Implements the specification
- Location: `Provider/src/Program.cs`
- Responsibility: HTTP endpoints + business logic

**Consumer (Test Client)**
- Uses the API per specification
- Location: `Consumer/src/ProductApiClient.cs`
- Responsibility: Make valid requests + handle responses

**Contract Tests (xUnit)**
- Validate provider + consumer adherence
- Location: `Consumer/tests/ProductApiContractTests.cs`
- Responsibility: Ensure spec compliance

## Getting Started Checklist

- [ ] Read `00-START-HERE.md`
- [ ] Read `QUICK-START.md`
- [ ] Run Provider: `dotnet run`
- [ ] Run Tests: `dotnet test`
- [ ] See ✓ All tests pass
- [ ] Read `README.md`
- [ ] Explore `specs/products-api.yaml`
- [ ] Review `Provider/src/Program.cs`
- [ ] Review `Consumer/tests/ProductApiContractTests.cs`
- [ ] Read `ARCHITECTURE.md`
- [ ] Make a test change
- [ ] Run tests again
- [ ] Try Docker: `docker-compose up`

## Troubleshooting

**Port 5000 in use**
- Docs: See `QUICK-START.md` → Troubleshooting

**Tests won't connect**
- Docs: See `QUICK-START.md` → Troubleshooting

**Build errors**
- Docs: See `QUICK-START.md` → Troubleshooting

**Need more help**
- Docs: See `README.md` → Resources

---

**Navigation**: Use this file to find what you need. Start with `00-START-HERE.md`.
