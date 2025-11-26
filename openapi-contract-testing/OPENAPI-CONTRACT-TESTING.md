# Contract Testing with OpenAPI/Swagger Validation

## Overview

### Alternative to Pact/Postman: OpenAPI-Based Contract Testing

```
Traditional Contract Testing (Pact/Postman)
├── Consumer generates contracts
├── Provider verifies contracts
└── Contracts live in code/tests

OpenAPI-Based Contract Testing
├── Single source of truth: OpenAPI spec
├── Consumer validates against spec
├── Provider validates against spec
├── Spec lives in repository
```

### When to Use OpenAPI vs Pact

| Criteria | Pact | OpenAPI |
|----------|------|---------|
| **Source of Truth** | Consumer-driven | Spec-driven |
| **Setup Complexity** | Simple | Medium |
| **Contract Generation** | Automatic from tests | Manual spec creation |
| **Schema Validation** | Built-in | Requires validator |
| **API Documentation** | Not included | Built-in |
| **Multi-consumer** | Separate contracts | Single spec |
| **Versioning** | Per contract | Version in spec |
| **Best for** | Microservices | REST APIs with docs |

---

## Architecture: OpenAPI Contract Testing

```
┌─────────────────────────────────────────────────────────────────┐
│                    OpenAPI Contract Testing Flow                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Define OpenAPI Spec                                         │
│     ┌──────────────────────────────────────────────────────┐   │
│     │ openapi.yaml (Single Source of Truth)               │   │
│     │ ├─ Endpoints: /users, /products, etc.               │   │
│     │ ├─ Request schemas                                  │   │
│     │ ├─ Response schemas                                 │   │
│     │ └─ Status codes, headers                            │   │
│     └──────────────────────────────────────────────────────┘   │
│                      ↓                                           │
│  2. Consumer Validation                                         │
│     ┌──────────────────────────────────────────────────────┐   │
│     │ SF Consumer (17.1, 18.1)                            │   │
│     │ Before making request:                              │   │
│     │ ├─ Validate request against spec                    │   │
│     │ ├─ Check headers match spec                         │   │
│     │ └─ Validate request body schema                     │   │
│     │ After receiving response:                           │   │
│     │ ├─ Validate response against spec                   │   │
│     │ ├─ Check status code in spec                        │   │
│     │ └─ Validate response body schema                    │   │
│     └──────────────────────────────────────────────────────┘   │
│                      ↓                                           │
│  3. Provider Validation                                         │
│     ┌──────────────────────────────────────────────────────┐   │
│     │ VAIS Provider                                       │   │
│     │ Before sending response:                            │   │
│     │ ├─ Validate request matches spec                    │   │
│     │ ├─ Validate response matches spec                   │   │
│     │ └─ Check all required fields present                │   │
│     │ At runtime:                                         │   │
│     │ ├─ Generate responses per spec                      │   │
│     │ └─ Reject invalid requests                          │   │
│     └──────────────────────────────────────────────────────┘   │
│                      ↓                                           │
│  4. Automated Testing                                           │
│     ┌──────────────────────────────────────────────────────┐   │
│     │ Test Tools (Dredd, Prism, Schemathesis)             │   │
│     │ ├─ Generate test cases from spec                    │   │
│     │ ├─ Execute requests                                 │   │
│     │ ├─ Validate responses                               │   │
│     │ └─ Report violations                                │   │
│     └──────────────────────────────────────────────────────┘   │
│                      ↓                                           │
│  5. CI/CD Integration                                           │
│     ├─ Validate spec syntax                               │   │
│     ├─ Generate mock servers                              │   │
│     ├─ Run contract tests                                 │   │
│     └─ Enforce spec compliance                            │   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Step 1: Create OpenAPI Specification

### OpenAPI 3.0 Spec for Your Scenario

```yaml
# openapi.yaml
openapi: 3.0.0
info:
  title: Users API
  version: 1.0.0
  description: |
    API for managing users with multiple versions
    - v1: Basic user information
    - v2: Extended user information with email and role

servers:
  - url: http://localhost:5001
    description: Development server
  - url: https://api.production.com
    description: Production server

tags:
  - name: Users
    description: User management endpoints

paths:
  /users:
    get:
      tags:
        - Users
      summary: Get all users
      description: |
        Retrieve all users.
        
        **Version support:**
        - Without `apiVersion` query param: Returns v1 format (SF 17.1 compatible)
        - With `apiVersion=2`: Returns v2 format (SF 18.1 compatible)
      operationId: getUsers
      parameters:
        - name: apiVersion
          in: query
          description: API version to use for response format
          required: false
          schema:
            type: string
            enum: ["1", "2"]
            default: "1"
        - name: limit
          in: query
          description: Maximum number of users to return
          required: false
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 50
        - name: offset
          in: query
          description: Number of users to skip
          required: false
          schema:
            type: integer
            minimum: 0
            default: 0
      responses:
        "200":
          description: Successful response with users list
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsersResponseV1"
              examples:
                v1:
                  value:
                    users:
                      - id: 1
                        name: "John"
                      - id: 2
                        name: "Jane"
        "400":
          description: Invalid query parameters
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Error"
        "500":
          description: Internal server error
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Error"

components:
  schemas:
    # V1 Response Schema
    UsersResponseV1:
      type: object
      required:
        - users
      properties:
        users:
          type: array
          items:
            $ref: "#/components/schemas/UserV1"

    UserV1:
      type: object
      required:
        - id
        - name
      properties:
        id:
          type: integer
          description: User unique identifier
          example: 1
        name:
          type: string
          description: User full name
          example: "John Doe"

    # V2 Response Schema (Extended)
    UsersResponseV2:
      type: object
      required:
        - users
      properties:
        users:
          type: array
          items:
            $ref: "#/components/schemas/UserV2"

    UserV2:
      type: object
      required:
        - id
        - name
        - email
        - role
      properties:
        id:
          type: integer
          description: User unique identifier
          example: 1
        name:
          type: string
          description: User full name
          example: "John Doe"
        email:
          type: string
          format: email
          description: User email address
          example: "john@company.com"
        role:
          type: string
          enum:
            - admin
            - user
            - guest
          description: User role
          example: "admin"

    Error:
      type: object
      required:
        - code
        - message
      properties:
        code:
          type: string
          example: "INVALID_PARAMETER"
        message:
          type: string
          example: "Invalid apiVersion parameter"
```

---

## Step 2: Consumer-Side Validation

### .NET Consumer with OpenAPI Validation

```csharp
// Program.cs - Setup
var services = builder.Services;

// Register OpenAPI validator
services.AddHttpClient<IOpenApiValidator, OpenApiValidator>();
services.AddSingleton<IOpenApiSpecProvider>(sp => 
    new YamlOpenApiSpecProvider("./openapi.yaml"));
```

```csharp
// IOpenApiValidator.cs
public interface IOpenApiValidator
{
    /// <summary>
    /// Validates request before sending to API
    /// </summary>
    Task<ValidationResult> ValidateRequestAsync(
        string method, 
        string path, 
        object requestBody = null,
        Dictionary<string, string> headers = null);

    /// <summary>
    /// Validates response received from API
    /// </summary>
    Task<ValidationResult> ValidateResponseAsync(
        string method,
        string path,
        int statusCode,
        object responseBody,
        Dictionary<string, string> headers = null);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
```

```csharp
// OpenApiValidator.cs
public class OpenApiValidator : IOpenApiValidator
{
    private readonly IOpenApiSpecProvider _specProvider;
    private readonly HttpClient _httpClient;

    public OpenApiValidator(HttpClient httpClient, IOpenApiSpecProvider specProvider)
    {
        _httpClient = httpClient;
        _specProvider = specProvider;
    }

    public async Task<ValidationResult> ValidateRequestAsync(
        string method,
        string path,
        object requestBody = null,
        Dictionary<string, string> headers = null)
    {
        var result = new ValidationResult { IsValid = true };
        var spec = await _specProvider.GetSpecAsync();

        // Get path item from spec
        if (!spec.Paths.TryGetValue(path, out var pathItem))
        {
            result.IsValid = false;
            result.Errors.Add($"Path '{path}' not found in OpenAPI spec");
            return result;
        }

        // Get operation (e.g., GET, POST)
        var operation = GetOperation(pathItem, method);
        if (operation == null)
        {
            result.IsValid = false;
            result.Errors.Add($"Operation {method} {path} not found in spec");
            return result;
        }

        // Validate request body if provided
        if (requestBody != null && operation.RequestBody != null)
        {
            var bodyValidation = ValidateRequestBody(requestBody, operation.RequestBody);
            if (!bodyValidation.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(bodyValidation.Errors);
            }
        }

        // Validate headers
        if (headers != null)
        {
            var headerValidation = ValidateHeaders(headers, operation.Parameters);
            if (!headerValidation.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(headerValidation.Errors);
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateResponseAsync(
        string method,
        string path,
        int statusCode,
        object responseBody,
        Dictionary<string, string> headers = null)
    {
        var result = new ValidationResult { IsValid = true };
        var spec = await _specProvider.GetSpecAsync();

        // Get operation
        var pathItem = spec.Paths[path];
        var operation = GetOperation(pathItem, method);

        // Check if status code is documented
        if (!operation.Responses.ContainsKey(statusCode.ToString()))
        {
            result.Warnings.Add(
                $"Status code {statusCode} not documented in spec for {method} {path}");
        }

        var response = operation.Responses[statusCode.ToString()];

        // Validate response body against schema
        if (responseBody != null && response.Content != null)
        {
            var content = response.Content["application/json"];
            if (content?.Schema != null)
            {
                var schemaValidation = ValidateAgainstSchema(responseBody, content.Schema);
                if (!schemaValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(schemaValidation.Errors);
                }
            }
        }

        return result;
    }

    private ValidationResult ValidateAgainstSchema(object data, OpenApiSchema schema)
    {
        var result = new ValidationResult { IsValid = true };

        // Use JSON Schema validator
        var validator = new JsonSchemaValidator();
        var jsonData = JsonConvert.SerializeObject(data);
        var schemaJson = JsonConvert.SerializeObject(schema);

        var validationErrors = validator.Validate(jsonData, schemaJson);
        if (validationErrors.Any())
        {
            result.IsValid = false;
            result.Errors.AddRange(validationErrors);
        }

        return result;
    }

    private OpenApiOperation GetOperation(OpenApiPathItem pathItem, string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => pathItem.Get,
            "POST" => pathItem.Post,
            "PUT" => pathItem.Put,
            "DELETE" => pathItem.Delete,
            "PATCH" => pathItem.Patch,
            _ => null
        };
    }

    private ValidationResult ValidateRequestBody(object body, OpenApiRequestBody requestBody)
    {
        var result = new ValidationResult { IsValid = true };
        var content = requestBody.Content["application/json"];
        
        if (content?.Schema != null)
        {
            return ValidateAgainstSchema(body, content.Schema);
        }

        return result;
    }

    private ValidationResult ValidateHeaders(
        Dictionary<string, string> headers,
        IList<OpenApiParameter> parameters)
    {
        var result = new ValidationResult { IsValid = true };
        
        var headerParams = parameters?.Where(p => p.In == ParameterLocation.Header).ToList();
        if (headerParams == null) return result;

        foreach (var param in headerParams)
        {
            if (param.Required && !headers.ContainsKey(param.Name))
            {
                result.IsValid = false;
                result.Errors.Add($"Required header '{param.Name}' is missing");
            }
        }

        return result;
    }
}
```

```csharp
// ApiClient.cs - Consumer making requests
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOpenApiValidator _validator;

    public ApiClient(HttpClient httpClient, IOpenApiValidator validator)
    {
        _httpClient = httpClient;
        _validator = validator;
    }

    public async Task<UsersResponse> GetUsersAsync(string apiVersion = "1")
    {
        var path = "/users";
        var queryParams = new Dictionary<string, string> { { "apiVersion", apiVersion } };
        
        // Validate request BEFORE sending
        var requestValidation = await _validator.ValidateRequestAsync("GET", path);
        if (!requestValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"Request validation failed: {string.Join(", ", requestValidation.Errors)}");
        }

        // Make request
        var url = $"{_httpClient.BaseAddress}{path}?apiVersion={apiVersion}";
        var response = await _httpClient.GetAsync(url);

        var content = await response.Content.ReadAsAsync<UsersResponse>();

        // Validate response AFTER receiving
        var responseValidation = await _validator.ValidateResponseAsync(
            "GET",
            path,
            (int)response.StatusCode,
            content);

        if (!responseValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"Response validation failed: {string.Join(", ", responseValidation.Errors)}");
        }

        return content;
    }
}
```

---

## Step 3: Provider-Side Validation

### .NET Provider with OpenAPI Middleware

```csharp
// OpenApiValidationMiddleware.cs
public class OpenApiValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOpenApiSpecProvider _specProvider;
    private readonly ILogger<OpenApiValidationMiddleware> _logger;

    public OpenApiValidationMiddleware(
        RequestDelegate next,
        IOpenApiSpecProvider specProvider,
        ILogger<OpenApiValidationMiddleware> logger)
    {
        _next = next;
        _specProvider = specProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var spec = await _specProvider.GetSpecAsync();
        var path = context.Request.Path.Value;
        var method = context.Request.Method;

        // Check if path exists in spec
        if (!spec.Paths.TryGetValue(path, out var pathItem))
        {
            _logger.LogWarning($"Path {method} {path} not found in OpenAPI spec");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Path not found" });
            return;
        }

        // Check if operation exists
        var operation = GetOperation(pathItem, method);
        if (operation == null)
        {
            _logger.LogWarning($"Operation {method} {path} not found in OpenAPI spec");
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            await context.Response.WriteAsJsonAsync(new { error = "Method not allowed" });
            return;
        }

        // Validate request body if POST/PUT/PATCH
        if (IsBodyOperationMethod(method) && operation.RequestBody != null)
        {
            var body = await ReadRequestBodyAsync(context);
            var bodyValidation = ValidateRequestBody(body, operation.RequestBody);
            
            if (!bodyValidation.IsValid)
            {
                _logger.LogWarning($"Request body validation failed: {string.Join(", ", bodyValidation.Errors)}");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Request validation failed",
                    details = bodyValidation.Errors
                });
                return;
            }
        }

        // Call next middleware
        await _next(context);
    }

    private ValidationResult ValidateRequestBody(string body, OpenApiRequestBody requestBody)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            var content = requestBody.Content["application/json"];
            if (content?.Schema != null)
            {
                var jsonData = JsonConvert.DeserializeObject(body);
                result = ValidateAgainstSchema(jsonData, content.Schema);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        return body;
    }

    private bool IsBodyOperationMethod(string method) =>
        method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
        method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
        method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);

    private OpenApiOperation GetOperation(OpenApiPathItem pathItem, string method) =>
        method.ToUpperInvariant() switch
        {
            "GET" => pathItem.Get,
            "POST" => pathItem.Post,
            "PUT" => pathItem.Put,
            "DELETE" => pathItem.Delete,
            "PATCH" => pathItem.Patch,
            _ => null
        };

    private ValidationResult ValidateAgainstSchema(object data, OpenApiSchema schema)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            var validator = new JsonSchemaValidator();
            var jsonData = JsonConvert.SerializeObject(data);
            var schemaJson = JsonConvert.SerializeObject(schema);

            var errors = validator.Validate(jsonData, schemaJson);
            if (errors.Any())
            {
                result.IsValid = false;
                result.Errors.AddRange(errors);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Schema validation error: {ex.Message}");
        }

        return result;
    }
}

// Register in Program.cs
app.UseMiddleware<OpenApiValidationMiddleware>();
```

---

## Step 4: Automated Testing Tools

### Option A: Dredd - API Contract Testing

```bash
# Installation
npm install -g dredd

# Configuration file: dredd.yml
dry-run: null
hookfiles: ./hooks/hooks.js
sandbox: true
server: http://localhost:5001
endpoint: http://localhost:5001

# Run Dredd
dredd
```

```javascript
// hooks/hooks.js
const hooks = require('hooks');

// Hook for GET /users
hooks.before('GET /users', function (transaction) {
    // Modify request if needed
    transaction.request.headers['X-API-Version'] = '1';
});

hooks.after('GET /users', function (transaction) {
    // Validate response after transaction
    console.log('Response status:', transaction.response.status);
});

// Hook for GET /users with apiVersion=2
hooks.before('GET /users > 200 > application/json > Example 0', function (transaction) {
    // Modify request for v2
    transaction.fullPath += '?apiVersion=2';
});
```

### Option B: Prism - Mock Server & Validation

```bash
# Installation
npm install -g @stoplight/prism-cli

# Start mock server (validates against spec)
prism mock openapi.yaml -p 4010

# Run validating proxy (validates both request and response)
prism proxy openapi.yaml http://localhost:5001 -p 4010
```

```csharp
// Consumer uses Prism as proxy
var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:4010")  // Prism validates
};

// All requests/responses validated against spec
var response = await httpClient.GetAsync("/users?apiVersion=2");
```

### Option C: Schemathesis - Property-Based Testing

```python
# Installation
pip install schemathesis

# Test using Schemathesis
schemathesis run \
  --hypothesis-seed=42 \
  --checks all \
  --hypothesis-max-examples=100 \
  http://localhost:5001 \
  --schema=./openapi.yaml
```

---

## Step 5: CI/CD Pipeline with OpenAPI Validation

### Azure Pipelines Implementation

```yaml
# azure-pipelines-openapi.yml
trigger:
  - main
  - docs

pool:
  vmImage: 'ubuntu-latest'

variables:
  dotnetVersion: '8.0.x'
  nodeVersion: '18.x'

stages:
  - stage: ValidateSpec
    displayName: 'Validate OpenAPI Specification'
    jobs:
      - job: ValidateOpenAPI
        steps:
          - checkout: self

          - task: NodeTool@0
            inputs:
              versionSpec: '$(nodeVersion)'
            displayName: 'Setup Node.js'

          - script: npm install -g swagger-cli
            displayName: 'Install Swagger CLI'

          - script: swagger-cli validate openapi.yaml
            displayName: 'Validate OpenAPI Syntax'

          - script: npm install -g @stoplight/spectral-cli
            displayName: 'Install Spectral'

          - script: spectral lint openapi.yaml
            displayName: 'Lint OpenAPI Spec'

  - stage: GenerateClients
    displayName: 'Generate API Clients'
    dependsOn: ValidateSpec
    jobs:
      - job: GenerateClients
        steps:
          - checkout: self

          - task: NodeTool@0
            inputs:
              versionSpec: '$(nodeVersion)'

          - script: npm install -g @openapitools/openapi-generator-cli
            displayName: 'Install OpenAPI Generator'

          - script: |
              mkdir -p generated-clients
              
              # Generate C# client
              openapi-generator-cli generate \
                -i openapi.yaml \
                -g csharp-netcore \
                -o generated-clients/csharp
            displayName: 'Generate C# Client'

  - stage: ConsumerTests
    displayName: 'Consumer Contract Tests'
    dependsOn: ValidateSpec
    jobs:
      - job: ConsumerTests
        steps:
          - checkout: self

          - task: DotNetCoreCLI@2
            inputs:
              command: 'restore'
              projects: '**/Consumer.Tests.csproj'

          - task: DotNetCoreCLI@2
            inputs:
              command: 'build'
              projects: '**/Consumer.Tests.csproj'

          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/Consumer.Tests.csproj'
              arguments: '--configuration Release'
            displayName: 'Run Consumer Tests'

          - task: PublishTestResults@2
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/TEST-*.xml'
            condition: always()

  - stage: ProviderTests
    displayName: 'Provider Contract Verification'
    dependsOn: ValidateSpec
    jobs:
      - job: ProviderDredd
        steps:
          - checkout: self

          - task: NodeTool@0
            inputs:
              versionSpec: '$(nodeVersion)'

          - task: DotNetCoreCLI@2
            inputs:
              command: 'build'
              projects: '**/Provider.csproj'
            displayName: 'Build Provider'

          - script: |
              cd Provider/src
              dotnet run &
              PROVIDER_PID=$!
              sleep 10
              
              cd ../../
              npm install -g dredd
              dredd --config dredd.yml
              
              kill $PROVIDER_PID
            displayName: 'Run Dredd Contract Tests'
            continueOnError: false

          - task: PublishBuildArtifacts@1
            inputs:
              PathtoPublish: '$(Build.SourcesDirectory)/dredd_result.json'
              ArtifactName: 'dredd-results'
            condition: always()

  - stage: IntegrationTests
    displayName: 'Integration Tests with Prism'
    dependsOn: ValidateSpec
    jobs:
      - job: PrismValidation
        steps:
          - checkout: self

          - task: NodeTool@0
            inputs:
              versionSpec: '$(nodeVersion)'

          - script: npm install -g @stoplight/prism-cli
            displayName: 'Install Prism'

          - task: DotNetCoreCLI@2
            inputs:
              command: 'build'
              projects: '**/Provider.csproj'

          - script: |
              # Start real provider
              cd Provider/src
              dotnet run &
              PROVIDER_PID=$!
              sleep 5
              
              # Start Prism proxy (validates all traffic)
              cd ../../
              prism proxy openapi.yaml http://localhost:5001 &
              PRISM_PID=$!
              sleep 5
              
              # Run integration tests through Prism
              cd Consumer.Tests
              dotnet test -- --logger=json > test-results.json
              
              kill $PROVIDER_PID $PRISM_PID
            displayName: 'Run Tests Through Prism Proxy'
            continueOnError: false

          - task: PublishTestResults@2
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/TEST-*.xml'
            condition: always()
```

---

## Comparison: Pact vs OpenAPI vs Postman

```
┌──────────────────────┬──────────────────┬──────────────────┬──────────────────┐
│ Aspect               │ Pact             │ OpenAPI          │ Postman          │
├──────────────────────┼──────────────────┼──────────────────┼──────────────────┤
│ Setup Time           │ Quick (code-based)│ Medium (YAML)    │ Fastest (UI)     │
│ Learning Curve       │ Medium           │ Steep            │ Gentle           │
│ Contract Source      │ Tests            │ Spec             │ Collections      │
│ Multi-Consumer       │ Separate contracts│ Single spec      │ Single collection│
│ Schema Validation    │ Built-in         │ Excellent        │ Basic            │
│ Versioning           │ Per file         │ In spec          │ Per collection   │
│ API Documentation    │ No               │ Yes              │ Built-in         │
│ Mock Server          │ Built-in         │ Requires Prism   │ Built-in         │
│ Consumer-Driven      │ Yes              │ No               │ Both             │
│ Tooling Ecosystem    │ Rich             │ Very Rich        │ Rich             │
│ CI/CD Integration    │ Excellent        │ Excellent        │ Good             │
│ Regression Testing   │ Excellent        │ Good             │ Good             │
│ Ease of Update       │ Difficult        │ Easy             │ Easy             │
└──────────────────────┴──────────────────┴──────────────────┴──────────────────┘
```

---

## Best Practices

### ✅ DO

1. **Keep OpenAPI spec as source of truth**
   ```yaml
   # openapi.yaml always reflects actual API capability
   openapi: 3.0.0
   info:
     version: 1.0.0
   paths:
     /users:
       get: ...
   ```

2. **Version your OpenAPI specs**
   ```
   docs/
   ├── openapi-v1.0.0.yaml
   ├── openapi-v2.0.0.yaml
   └── openapi-current.yaml
   ```

3. **Validate in CI/CD before deployment**
   ```bash
   swagger-cli validate openapi.yaml
   spectral lint openapi.yaml
   dredd --config dredd.yml
   ```

4. **Use examples in spec**
   ```yaml
   responses:
     "200":
       content:
         application/json:
           examples:
             v1:
               value:
                 users:
                   - id: 1
                     name: "John"
   ```

5. **Document API changes in spec**
   ```yaml
   deprecated: false  # Set true before removal
   x-sunset-date: "2024-12-31"  # Deprecation date
   description: "v2 endpoint coming in Dec 2024"
   ```

### ❌ DON'T

1. **Don't let spec become stale**
   - Update spec when API changes
   - Enforce spec validation in CI/CD

2. **Don't ignore deprecation warnings**
   - Plan migration timeline
   - Communicate with consumers

3. **Don't skip response validation**
   - Always validate responses match spec
   - Catch bugs early

4. **Don't manually write clients**
   - Generate from OpenAPI spec
   - Reduces mismatches

5. **Don't have multiple specs**
   - Single source of truth
   - Easy versioning management

---

## Practical Example: Your SF/VAIS Scenario

```yaml
# openapi-vais-api.yaml
openapi: 3.0.0
info:
  title: VAIS API
  version: 2.0.0
  description: Version 2 supports both v1 and v2 clients

paths:
  /users:
    get:
      operationId: getUsers
      parameters:
        - name: apiVersion
          in: query
          schema:
            type: string
            enum: ["1", "2"]
            default: "1"
      responses:
        "200":
          description: Users list
          content:
            application/json:
              schema:
                oneOf:
                  - $ref: "#/components/schemas/UsersResponseV1"
                  - $ref: "#/components/schemas/UsersResponseV2"
```

**SF 17.1 Consumer**:
- Calls `GET /users` (no apiVersion param)
- Validates response against `UsersResponseV1` schema
- ✅ Works with all VAIS versions

**SF 18.1 Consumer**:
- Calls `GET /users?apiVersion=2`
- Validates response against `UsersResponseV2` schema
- ✅ Works with VAIS 2.0+

**VAIS Provider 2.0**:
- Implements both response formats
- Returns v1 or v2 based on query param
- Validates requests against spec
- ✅ Supports both consumers

---

## Key Takeaway

**OpenAPI is ideal when**:
- You want single source of truth
- You need comprehensive API documentation
- You have multiple consumers
- You want tooling ecosystem (mock servers, clients)
- You prefer spec-driven development

**Pact is ideal when**:
- You want consumer-driven contracts
- You have complex request/response combinations
- You want maximum testing flexibility
- You prefer code over YAML

**You can combine both**: Use OpenAPI for spec, Pact for advanced scenarios.

---

**Last Updated**: November 26, 2025
