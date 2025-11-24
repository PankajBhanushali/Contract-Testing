# Postman Contract Testing Example

## Overview
This directory contains a simplified implementation of contract testing using Postman for the same Product API used in the Pact workshop.

## Structure
```
postman-contract-testing/
├── Provider/src/          # Provider API (ASP.NET Core)
├── Consumer/src/          # Consumer client application
└── postman-collections/   # Postman collections and environments
```

## Setup & Run

### 1. Start the Provider
```powershell
cd Provider\src
dotnet restore
dotnet run
```
Provider will start on `http://localhost:5001`

### 2. Run Consumer
```powershell
cd Consumer\src
dotnet restore
dotnet run
```

### 3. Run Contract Tests with Postman

#### Option A: Postman GUI
1. Import `Product-API-Contract-Tests.postman_collection.json` into Postman
2. Import `Product-API-Environment.postman_environment.json`
3. Select the environment
4. Run the collection using the Collection Runner
5. Review test results

#### Option B: Newman CLI
```powershell
# Install Newman (if not already installed)
npm install -g newman

# Run contract tests
newman run postman-collections/Product-API-Contract-Tests.postman_collection.json `
  -e postman-collections/Product-API-Environment.postman_environment.json `
  --reporters cli,html `
  --reporter-html-export newman-report.html
```

## Contract Test Coverage

The Postman collection tests the following contracts:

1. **Get All Products**
   - Status: 200 OK
   - Response: JSON array
   - Each product has: id (number), name (string), type (string), version (string)
   - Array is not empty

2. **Get Product by ID (Exists)**
   - Status: 200 OK
   - Response: JSON object
   - Product has required fields with correct types
   - ID matches requested ID

3. **Get Product by ID (Not Found)**
   - Status: 404 Not Found
   - Empty or minimal response body

## How to Use for Contract Testing

### Consumer Team Workflow:
1. Define API expectations in Postman collection
2. Share collection with Provider team
3. Provider team ensures their API passes these tests
4. Both teams agree on the contract

### CI/CD Integration:
```yaml
# Example: Azure Pipelines
- task: Npm@1
  inputs:
    command: 'custom'
    customCommand: 'install -g newman'

- script: |
    newman run postman-collections/Product-API-Contract-Tests.postman_collection.json \
      -e postman-collections/Product-API-Environment.postman_environment.json \
      --reporters cli,junit \
      --reporter-junit-export test-results.xml
  displayName: 'Run Contract Tests'
```

## Limitations Compared to Pact

1. **No Consumer-Driven Development**: Tests are written manually, not generated from consumer code
2. **No Mock Server**: Cannot run consumer tests in isolation
3. **Manual Synchronization**: Contract file must be manually shared and kept in sync
4. **Provider States**: No built-in mechanism for provider state management
5. **Version Management**: No automatic contract versioning or compatibility checking
6. **Bi-directional Testing**: Requires running provider to test consumer

## Advantages Over Pact

1. **Simpler Setup**: No special frameworks or libraries needed
2. **Familiar Tool**: Many teams already use Postman
3. **Quick Start**: Can begin testing immediately
4. **Visual Interface**: Easy to debug and understand tests
5. **Broader Testing**: Can include performance, monitoring, and API documentation
