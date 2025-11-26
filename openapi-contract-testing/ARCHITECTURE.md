# OpenAPI Demo - Visual Architecture

## System Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                    OpenAPI Contract Testing Demo                      │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │ 1. SINGLE SOURCE OF TRUTH                                  │    │
│  │ ┌──────────────────────────────────────────────────────┐   │    │
│  │ │ openapi.yaml (API Contract)                         │   │    │
│  │ │                                                      │   │    │
│  │ │ /users endpoint:                                   │   │    │
│  │ │  - Query param: apiVersion (1 or 2)               │   │    │
│  │ │  - V1 Schema: {id, name}                          │   │    │
│  │ │  - V2 Schema: {id, name, email, role}             │   │    │
│  │ └──────────────────────────────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                          ▲                                            │
│                          │                                            │
│        ┌─────────────────┼─────────────────┐                         │
│        │                 │                 │                         │
│        ▼                 ▼                 ▼                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │
│  │ 2. PROVIDER  │  │ 3. CONSUMER  │  │ 4. CONSUMER  │             │
│  │     API      │  │   V1 CLIENT  │  │   V2 CLIENT  │             │
│  │              │  │              │  │              │             │
│  │ Node.js      │  │ SF 17.1      │  │ SF 18.1      │             │
│  │ Express      │  │              │  │              │             │
│  │              │  │ GET /users   │  │ GET /users   │             │
│  │ Port: 5001   │  │ (no params)  │  │ ?apiVersion=2│             │
│  │              │  │              │  │              │             │
│  │ Logic:       │  │ Expects:     │  │ Expects:     │             │
│  │ if (v=2)     │  │ {id, name}   │  │ {id, name,   │             │
│  │  return v2   │  │              │  │  email, role}│             │
│  │ else         │  │              │  │              │             │
│  │  return v1   │  │              │  │              │             │
│  └─────────────┬┘  └──────────────┘  └──────────────┘             │
│                │         │                   │                    │
│                │         └─────────┬─────────┘                    │
│                │                   │                              │
│                └───────────────────┼──────────────────────────┐   │
│                                    ▼                          │   │
│                         ┌────────────────────┐               │   │
│                         │ 5. TEST EXECUTION  │               │   │
│                         │                    │               │   │
│                         │ Jest Tests         │               │   │
│                         │                    │               │   │
│                         │ v1.test.js (7)     │               │   │
│                         │ ├─ Status 200      │               │   │
│                         │ ├─ Has users array │               │   │
│                         │ ├─ V1 schema valid │               │   │
│                         │ ├─ No email/role   │               │   │
│                         │ ├─ Limit works     │               │   │
│                         │ ├─ Headers valid   │               │   │
│                         │ └─ Error handling  │               │   │
│                         │                    │               │   │
│                         │ v2.test.js (9)     │               │   │
│                         │ ├─ Status 200      │               │   │
│                         │ ├─ Has users array │               │   │
│                         │ ├─ V2 schema valid │               │   │
│                         │ ├─ Has email/role  │               │   │
│                         │ ├─ Email format ok │               │   │
│                         │ ├─ Role enum valid │               │   │
│                         │ ├─ Limit works     │               │   │
│                         │ ├─ Headers valid   │               │   │
│                         │ └─ Backward compat │               │   │
│                         └──────┬─────────────┘               │   │
│                                │                            │   │
│                                ▼                            │   │
│                         ┌────────────────┐                  │   │
│                         │ 6. RESULTS     │                  │   │
│                         │                │                  │   │
│                         │ ✓ 16 tests OK  │                  │   │
│                         │ ✓ V1 contract  │                  │   │
│                         │   validated    │                  │   │
│                         │ ✓ V2 contract  │                  │   │
│                         │   validated    │                  │   │
│                         │ ✓ Backward     │                  │   │
│                         │   compatible   │                  │   │
│                         └────────────────┘                  │   │
│                                                              │   │
└──────────────────────────────────────────────────────────────┘   │
```

## Response Format Comparison

```
Request: GET /users                    vs    Request: GET /users?apiVersion=2
           (V1 - SF 17.1)                              (V2 - SF 18.1)

Response:                                  Response:
{                                          {
  "users": [                                 "users": [
    {                                          {
      "id": 1,               ◄──────►           "id": 1,
      "name": "John"         ◄──────►           "name": "John",
                            NEW                "email": "john@co.com",
                                               "role": "admin"
    }                                        }
  ]                                        ]
}                                          }

Schema:                                    Schema:
Type: object                               Type: object
Required: [users]                          Required: [users]
  users:                                     users:
    Type: array                                Type: array
    Items:                                     Items:
      Type: object                               Type: object
      Required: [id, name]                      Required: [id, name, email, role]
      Properties:                                Properties:
        id: integer                              id: integer
        name: string                             name: string
                            NEW                  email: string (email format)
                                                 role: enum [admin, user, guest]
```

## Data Flow Sequence

```
Timeline: SF 17.1 Consumer requests data

┌─────────────────────────────────────────────────────────────┐
│ Consumer (SF 17.1)                                          │
└─────────────────────────────────────────────────────────────┘
           │
           │ client.getUsers()
           │ GET /users
           │
           ▼
┌─────────────────────────────────────────────────────────────┐
│ Network Request                                             │
│                                                             │
│ GET /users HTTP/1.1                                         │
│ Host: localhost:5001                                        │
│ Content-Type: application/json                              │
└─────────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────┐
│ Provider (VAIS)                                             │
│                                                             │
│ app.get('/users', (req, res) => {                           │
│   const apiVersion = req.query.apiVersion || '1';           │
│   if (apiVersion === '2') {                                 │
│     // Return v2 response                                   │
│   }                                                         │
│   // Return v1 response                                     │
│ })                                                          │
└─────────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────┐
│ Response                                                    │
│                                                             │
│ HTTP/1.1 200 OK                                             │
│ Content-Type: application/json                              │
│                                                             │
│ {                                                           │
│   "users": [                                                │
│     { "id": 1, "name": "John Doe" },                        │
│     { "id": 2, "name": "Jane Smith" }                       │
│   ]                                                         │
│ }                                                           │
└─────────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────┐
│ Consumer Validation                                         │
│                                                             │
│ ✓ Status 200 OK                                             │
│ ✓ Response has 'users' array                                │
│ ✓ Each user has 'id' and 'name'                             │
│ ✓ No 'email' or 'role' (V1 format)                          │
│ ✓ Schema matches OpenAPI spec                               │
└─────────────────────────────────────────────────────────────┘
           │
           ▼
       Contract ✓ SATISFIED
```

## Test Coverage Matrix

```
┌────────────────┬───────────────┬───────────────┬──────────────┐
│ Aspect         │ SF 17.1 (V1)  │ SF 18.1 (V2)  │ Both         │
├────────────────┼───────────────┼───────────────┼──────────────┤
│ Status Code    │ ✓ 200         │ ✓ 200         │              │
│ Array Format   │ ✓ users       │ ✓ users       │              │
│ Id Field       │ ✓ required    │ ✓ required    │              │
│ Name Field     │ ✓ required    │ ✓ required    │              │
│ Email Field    │ ✗ not present │ ✓ required    │              │
│ Role Field     │ ✗ not present │ ✓ required    │              │
│ Email Format   │ N/A           │ ✓ valid       │              │
│ Role Enum      │ N/A           │ ✓ valid       │              │
│ Limit Param    │ ✓ works       │ ✓ works       │              │
│ Headers        │ ✓ JSON        │ ✓ JSON        │              │
│ Backward Compat│ N/A           │ ✓ includes v1 │              │
│ Error Handling │ ✓ graceful    │ N/A           │              │
├────────────────┼───────────────┼───────────────┼──────────────┤
│ Total Tests    │ 7             │ 9             │ 16 total     │
└────────────────┴───────────────┴───────────────┴──────────────┘
```

## Deployment Timeline

```
Timeline showing version evolution

Phase 1: Initial State
├── Provider: VAIS 1.0
├── Consumer: SF 17.1 only
├── Endpoint: GET /users (v1)
└── Contract: SF-v17.1-Consumer-VAIS-Provider.json

        ↓ (New version released)

Phase 2: Dual Support
├── Provider: VAIS 2.0 (backward compatible)
│   ├── GET /users → v1 response
│   └── GET /users?apiVersion=2 → v2 response
├── Consumer: SF 17.1 + SF 18.1
├── Contracts:
│   ├── SF-v17.1-Consumer-VAIS-Provider.json
│   └── SF-v18.1-Consumer-VAIS-Provider.json
└── Status: Both versions work ✓

        ↓ (Transition period: 2-3 quarters)

Phase 3: New Version Dominant
├── Most consumers upgraded to SF 18.1
├── V1 support continues (for legacy systems)
└── Ready for Phase 4 if needed

        ↓ (Deprecation: After timeline expires)

Phase 4: V1 Deprecation (optional)
├── Provider: VAIS 3.0
├── Endpoint: GET /users?apiVersion=2 only
└── Consumer: SF 18.1+ required
```

## Quality Gates

```
┌──────────────────────────────────────────────────────────────┐
│                    Quality Assurance Checks                   │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  1. OpenAPI Spec Validation                                   │
│     └─ swagger-cli validate openapi.yaml                      │
│        ├─ Syntax OK                                           │
│        ├─ Schema references valid                             │
│        └─ Examples match schemas                              │
│                                                               │
│  2. Consumer Contract Tests                                   │
│     ├─ V1 Tests (SF 17.1)                                     │
│     │  ├─ 7 tests must pass                                   │
│     │  └─ Validates v1 contract                               │
│     └─ V2 Tests (SF 18.1)                                     │
│        ├─ 9 tests must pass                                   │
│        └─ Validates v2 contract                               │
│                                                               │
│  3. Response Schema Validation                                │
│     ├─ V1: Only {id, name}                                    │
│     ├─ V2: {id, name, email, role}                            │
│     └─ Enum values correct                                    │
│                                                               │
│  4. Backward Compatibility                                    │
│     ├─ V2 includes all v1 fields                              │
│     ├─ V1 clients work with new provider                      │
│     └─ No breaking changes                                    │
│                                                               │
│  5. Performance                                               │
│     ├─ Response time < 500ms                                  │
│     └─ Handles 100+ concurrent requests                       │
│                                                               │
│  6. Error Handling                                            │
│     ├─ Invalid apiVersion → 400                               │
│     ├─ Server error → 500                                     │
│     └─ Graceful degradation                                   │
│                                                               │
│  All Checks Pass → ✓ Deploy to Production                     │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

**Created**: November 26, 2025
