# CI/CD Pipeline Setup for Postman Contract Tests

## Overview

This guide explains how to set up a CI/CD pipeline to automatically run Postman contract tests whenever code changes are pushed to GitHub. It covers the roles, tools, and step-by-step implementation.

---

## Who Does What?

### Roles & Responsibilities

| Role | Responsibility |
|------|-----------------|
| **Developer** | Writes code, creates/updates Postman collections, commits to Git |
| **DevOps Engineer** | Sets up GitHub Actions workflow, configures CI/CD pipeline |
| **Automation/QA** | Creates test scenarios, maintains Postman collections and test assertions |

### In This Project (Your Context)

- **You (Developer/DevOps)** - Will handle all three roles since this is a learning project
- **GitHub Actions** - Automated CI/CD platform (free with GitHub)
- **Newman CLI** - Command-line tool to run Postman collections

---

## Architecture Overview

```
Developer commits code
         ↓
GitHub detects push
         ↓
GitHub Actions triggered
         ↓
Workflow file (.github/workflows/*.yml) executes
         ↓
Runner spins up (Ubuntu/Windows/macOS)
         ↓
Newman CLI installs & runs Postman collection
         ↓
Tests execute against Provider API
         ↓
Results reported (Pass/Fail)
         ↓
Status badge shown on GitHub repository
```

---

## Step-by-Step Setup

### Step 1: Create GitHub Actions Workflow File

**Who:** DevOps Engineer  
**What:** Create workflow file  
**Where:** `.github/workflows/postman-tests.yml`

```yaml
name: Postman Contract Tests

on:
  push:
    branches: [ master, main ]
  pull_request:
    branches: [ master, main ]

jobs:
  postman-tests:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Install Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install Newman CLI
        run: npm install -g newman

      - name: Start Provider API
        run: |
          cd postman-contract-testing/Provider/src
          dotnet run &
          sleep 10  # Wait for API to start

      - name: Run Postman Collection Tests
        run: newman run \
          "postman-contract-testing/postman-collections/Product-API-Contract-Tests.postman_collection.json" \
          -e "postman-contract-testing/postman-collections/Product-API-Environment.postman_environment.json" \
          -r json \
          --reporter-json-export results.json

      - name: Publish Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: postman-test-results
          path: results.json
```

### Step 2: Add Required Tools

**Who:** DevOps Engineer  
**What:** Ensure tools are available  
**Actions:**

1. **Newman CLI** - Already configured in workflow (Step 1)
2. **.NET SDK** - Needed to run Provider API
   ```yaml
   - name: Setup .NET
     uses: actions/setup-dotnet@v3
     with:
       dotnet-version: '8.0.x'
   ```

### Step 3: Configure Provider API Startup

**Who:** DevOps Engineer  
**What:** Ensure Provider API starts before tests run  
**Where:** Workflow file

```yaml
- name: Start Provider API
  run: |
    cd postman-contract-testing/Provider/src
    dotnet run &
    sleep 10  # Wait for API to fully start
```

**Why:** Tests need the API to be running on `localhost:5001`

### Step 4: Define Test Execution

**Who:** Automation/QA  
**What:** Specify how to run tests  
**Command:**

```bash
newman run <collection-file> \
  -e <environment-file> \
  -r json \
  --reporter-json-export results.json
```

**Components:**
- `<collection-file>` - Postman collection with test cases
- `-e <environment-file>` - Environment variables (baseUrl, etc.)
- `-r json` - JSON report format
- `--reporter-json-export` - Export results file

### Step 5: Handle Test Results

**Who:** DevOps Engineer  
**What:** Store and display test results  
**Options:**

**Option A: Upload Artifacts** (Current Approach)
```yaml
- name: Publish Test Results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: postman-test-results
    path: results.json
```

**Option B: Fail Workflow on Test Failure** (Stricter)
```yaml
- name: Check Test Results
  if: always()
  run: |
    # Fail if any tests failed
    if grep -q '"failed".*true' results.json; then
      exit 1
    fi
```

**Option C: Post Comment on Pull Request**
```yaml
- name: Comment PR with Results
  if: always()
  uses: actions/github-script@v6
  with:
    script: |
      const fs = require('fs');
      const results = JSON.parse(fs.readFileSync('results.json'));
      github.rest.issues.createComment({
        issue_number: context.issue.number,
        owner: context.repo.owner,
        repo: context.repo.repo,
        body: `Test Results: ${results.run.stats.tests.total} tests, ${results.run.stats.tests.failed} failed`
      })
```

---

## Complete Workflow Example

Here's a production-ready workflow file:

```yaml
name: Postman Contract Tests

on:
  push:
    branches: [ master, main ]
  pull_request:
    branches: [ master, main ]
  schedule:
    - cron: '0 0 * * *'  # Daily at midnight

env:
  API_PORT: 5001
  API_URL: http://localhost:5001

jobs:
  postman-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install Newman CLI
        run: npm install -g newman

      - name: Restore .NET dependencies
        run: dotnet restore postman-contract-testing/Provider/src/provider.csproj

      - name: Start Provider API
        run: |
          cd postman-contract-testing/Provider/src
          dotnet run &
          # Wait for API to be ready
          for i in {1..30}; do
            curl -f http://localhost:5001/api/products && break
            echo "Waiting for API... ($i/30)"
            sleep 1
          done

      - name: Run Postman Collection Tests
        run: |
          newman run \
            "postman-contract-testing/postman-collections/Product-API-Contract-Tests.postman_collection.json" \
            -e "postman-contract-testing/postman-collections/Product-API-Environment.postman_environment.json" \
            --reporters cli,json \
            --reporter-json-export test-results.json

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: postman-test-results
          path: test-results.json
          retention-days: 30

      - name: Publish Test Report
        if: always()
        uses: dorny/test-reporter@v1
        with:
          name: Postman Tests
          path: test-results.json
          reporter: 'newman'

      - name: Fail on Test Failure
        if: failure()
        run: exit 1
```

---

## Implementation Checklist

### Phase 1: Setup (DevOps Engineer)

- [ ] Create `.github/workflows/postman-tests.yml` in repository root
- [ ] Commit workflow file to GitHub
- [ ] Verify workflow appears in GitHub Actions tab

### Phase 2: Configuration (DevOps + Automation)

- [ ] Ensure Postman collection is committed to repository
- [ ] Ensure environment file defines correct `baseUrl`
- [ ] Test workflow manually in GitHub UI
- [ ] Verify Provider API starts successfully
- [ ] Verify Newman can find and run collection

### Phase 3: Testing (Developer)

- [ ] Make a test commit to trigger workflow
- [ ] Check GitHub Actions tab for workflow execution
- [ ] Verify tests run and pass
- [ ] Check test results artifact

### Phase 4: Refinement (DevOps)

- [ ] Add failure notifications (Slack, email)
- [ ] Add status badges to README
- [ ] Configure branch protection rules
- [ ] Set up required checks before merge

---

## Step-by-Step Execution Flow

### When Developer Pushes Code:

```
1. Developer: git push to master
   ↓
2. GitHub: Detects push event
   ↓
3. GitHub Actions: Triggers workflow
   ↓
4. Runner Setup:
   - Checkout code from repository
   - Install .NET SDK
   - Install Node.js
   - Install Newman globally
   ↓
5. Build Phase:
   - Restore .NET dependencies
   ↓
6. Start Services:
   - Start Provider API on localhost:5001
   - Wait until API is responding
   ↓
7. Test Phase:
   - Run Newman with Postman collection
   - Execute all test cases against running API
   - Generate results.json with outcomes
   ↓
8. Report Phase:
   - Upload results artifact
   - Display in GitHub Actions UI
   - Optional: Comment on PR with results
   ↓
9. GitHub: Shows workflow status (✅ Pass or ❌ Fail)
   ↓
10. Developer: Sees results in GitHub UI or PR
```

---

## Key Concepts

### GitHub Actions Workflow

**What it is:** YAML file that defines automated jobs and steps  
**Location:** `.github/workflows/*.yml` in repository root  
**Triggers:** Push, Pull Request, Schedule, Manual dispatch  
**Benefits:** Free tier includes 2000 free minutes/month per repository

### Newman CLI

**What it is:** Command-line runner for Postman collections  
**Installation:** `npm install -g newman`  
**Usage:** `newman run <collection.json> -e <environment.json>`  
**Output:** Can generate JSON, HTML, or CLI reports

### Environment File

**What it is:** YAML/JSON file with variables for tests  
**Contains:** baseUrl, authentication, test data  
**Example:**
```json
{
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5001"
    }
  ]
}
```

### Test Results

**Format:** JSON with pass/fail for each test  
**Contains:**
- Total test count
- Passed/failed count
- Detailed error messages
- Execution time
- Response data

---

## Common Issues & Solutions

### Issue: "API connection refused"
**Cause:** API not started before tests run  
**Solution:** Add health check before running tests
```yaml
- name: Wait for API
  run: |
    for i in {1..30}; do
      curl -f http://localhost:5001/api/products && break
      sleep 1
    done
```

### Issue: "Newman command not found"
**Cause:** Newman not installed globally  
**Solution:** Add installation step
```yaml
- name: Install Newman
  run: npm install -g newman
```

### Issue: "Collection file not found"
**Cause:** Wrong path to collection file  
**Solution:** Verify path relative to repository root
```bash
# Correct:
postman-contract-testing/postman-collections/Product-API-Contract-Tests.postman_collection.json

# Wrong:
postman-collections/Product-API-Contract-Tests.postman_collection.json
```

### Issue: "Tests pass locally but fail in CI"
**Cause:** Environment differences (paths, timeouts, etc.)  
**Solution:** Use explicit environment variables, longer timeouts
```yaml
env:
  API_PORT: 5001
  API_URL: http://localhost:5001
  TIMEOUT: 30000
```

---

## Best Practices

### 1. **Run Tests on Every Push**
```yaml
on:
  push:
    branches: [ master, main ]
```

### 2. **Also Run on Pull Requests**
```yaml
on:
  pull_request:
    branches: [ master, main ]
```

### 3. **Add Scheduled Runs**
```yaml
on:
  schedule:
    - cron: '0 0 * * *'  # Daily at midnight
```

### 4. **Keep Workflow Files in Repository**
- Enables version control
- Allows tracking changes over time
- Team can review and update

### 5. **Use Explicit Tool Versions**
```yaml
- uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'

- uses: actions/setup-node@v3
  with:
    node-version: '18'
```

### 6. **Always Upload Results**
```yaml
- uses: actions/upload-artifact@v3
  if: always()  # Even if tests fail
```

### 7. **Set Reasonable Timeouts**
```yaml
jobs:
  postman-tests:
    timeout-minutes: 15
```

---

## Advanced Options

### Option 1: Parallel Test Execution

```yaml
jobs:
  test-group-1:
    # Tests 1-3
  test-group-2:
    # Tests 4-6
```

### Option 2: Matrix Strategy

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest]
    node-version: [16, 18, 20]
```

### Option 3: Conditional Steps

```yaml
- name: Notify on Failure
  if: failure()
  run: echo "Tests failed!"
```

### Option 4: Deployment After Tests Pass

```yaml
deploy:
  needs: postman-tests
  if: success()
  runs-on: ubuntu-latest
  steps:
    # Deployment steps here
```

---

## For Your Project

### Recommended Implementation

1. **Create** `.github/workflows/postman-tests.yml` with production workflow
2. **Commit** to repository
3. **Push** to trigger first run
4. **Verify** tests execute successfully
5. **Monitor** workflow runs in GitHub Actions tab

### Next Steps

- Add Pact contract tests workflow
- Add code coverage reports
- Add notifications to Slack/Email
- Set up branch protection rules
- Create status badges for README

---

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Newman CLI Documentation](https://learning.postman.com/docs/running-collections/using-newman-cli/)
- [Postman Testing Guide](https://learning.postman.com/docs/writing-scripts/test-scripts/)
- [Setup Actions for .NET](https://github.com/actions/setup-dotnet)
- [Setup Actions for Node.js](https://github.com/actions/setup-node)

---

**Last Updated:** November 26, 2025  
**Project:** Contract Testing - Postman CI/CD Setup
