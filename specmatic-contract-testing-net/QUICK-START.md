# Specmatic Contract Testing - .NET Example

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker & Docker Compose (optional)

### Step 1: Start the Provider API

**Option A: Local Development**
```bash
cd Provider
dotnet restore
dotnet run
```

The API will be available at `http://localhost:5000`

**Option B: Docker**
```bash
docker-compose up --build
```

### Step 2: Run Consumer Tests

```bash
cd Consumer
dotnet restore
dotnet test
```

## Project Structure

```
specmatic-contract-testing-net/
├── specs/
│   └── products-api.yaml          # OpenAPI 3.0 specification
├── Provider/
│   ├── src/
│   │   ├── Program.cs             # ASP.NET Core startup
│   │   ├── Models.cs              # Data models
│   │   └── ProductRepository.cs   # In-memory repository
│   ├── provider.csproj
│   └── appsettings.json
├── Consumer/
│   ├── src/
│   │   ├── Models.cs              # Shared models
│   │   └── ProductApiClient.cs    # HTTP client
│   ├── tests/
│   │   └── ProductApiContractTests.cs   # xUnit contract tests
│   └── consumer.csproj
├── Dockerfile
├── docker-compose.yml
└── specmatic.yaml
```

## API Endpoints

### Health Check
```bash
GET /api/health
```

### Products
```bash
GET /api/products                    # Get all products
POST /api/products                   # Create product
GET /api/products/{id}               # Get product by ID
PUT /api/products/{id}               # Update product
DELETE /api/products/{id}            # Delete product
```

## Contract Testing

The contract tests validate:
- ✅ Endpoint availability
- ✅ Response schemas
- ✅ Data types
- ✅ Required fields
- ✅ HTTP status codes
- ✅ Error handling
- ✅ Business rules

## Test Coverage

### GET /api/products
- Returns array of products
- Products have required fields
- Correct data types
- Valid category enum

### GET /api/products/{id}
- Returns single product
- Correct schema
- 404 for non-existent

### POST /api/products
- Creates with valid data
- Returns all required fields
- Validates price > 0
- Validates category enum

### PUT /api/products/{id}
- Updates with valid data
- Returns updated product
- 404 for non-existent

### DELETE /api/products/{id}
- Deletes successfully
- 404 for non-existent

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "ClassName=ProductApiContractTests"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Key Concepts

### Specification-Driven
The OpenAPI spec is the single source of truth for the API contract.

### Contract Tests
Tests verify that both provider and consumer adhere to the specification.

### Automatic Validation
Tests automatically validate:
- Request/response formats
- Data types
- Required fields
- Error cases

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Testing**: xUnit
- **HTTP Client**: HttpClient
- **Assertions**: FluentAssertions
- **Container**: Docker

## Best Practices

1. **Spec First**: Define the specification before implementation
2. **Keep Tests Updated**: Update tests when spec changes
3. **Comprehensive Coverage**: Test happy paths and error cases
4. **Validate Early**: Run tests during development
5. **CI/CD Integration**: Automate in your pipeline

## Troubleshooting

### Port Already in Use
```bash
# Kill process on port 5000
lsof -ti:5000 | xargs kill -9
```

### Tests Cannot Connect
- Ensure Provider is running on http://localhost:5000
- Check firewall settings
- Verify no port conflicts

### Dotnet Not Found
- Install .NET 8.0 SDK from https://dotnet.microsoft.com/download
- Add to PATH if needed

## Next Steps

1. Run the example end-to-end
2. Modify the OpenAPI spec
3. Update Provider implementation
4. Run tests to verify compliance
5. Integrate into your CI/CD pipeline

## Resources

- [OpenAPI Specification](https://spec.openapis.org)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [xUnit Documentation](https://xunit.net)
- [Specmatic Documentation](https://specmatic.io)
