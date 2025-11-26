# Sharing Pact Files Between Azure DevOps (ADO) Repositories

## Table of Contents

1. [Overview](#overview)
2. [Scenarios & Solutions](#scenarios--solutions)
3. [Method 1: Git Repository Sharing](#method-1-git-repository-sharing)
4. [Method 2: Pact Broker](#method-2-pact-broker)
5. [Method 3: Azure Artifacts](#method-3-azure-artifacts)
6. [Method 4: Direct File Transfer](#method-4-direct-file-transfer)
7. [CI/CD Pipeline Integration](#cicd-pipeline-integration)
8. [Best Practices](#best-practices)

---

## Overview

### The Challenge

You have two separate Azure DevOps projects:
- **SF (TFS)**: Source Forge - Consumer Team (generates Pact files)
- **VAIS (ADO)**: Your ADO project - Provider Team (verifies Pact files)

**Goal**: Get Pact files from SF to VAIS for provider verification

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                 Azure DevOps                            │
├──────────────────────────┬──────────────────────────────┤
│                          │                              │
│  Project: SF (TFS)       │    Project: VAIS (ADO)       │
│  ┌────────────────────┐  │  ┌───────────────────────┐   │
│  │ Consumer API       │  │  │ Provider API          │   │
│  │ generates pact     │  │  │ verifies pact         │   │
│  │ file               │  │  │                       │   │
│  └────────┬───────────┘  │  └───────────┬───────────┘   │
│           │              │              │               │
│           ▼              │              ▼               │
│  ┌─────────────────┐    │  ┌──────────────────────┐    │
│  │ Pact File       │    │  │ Pact File            │    │
│  │ Consumer-Prov.  │    │  │ (from SF)            │    │
│  │ json            │    │  │                      │    │
│  └─────────────────┘    │  └──────────────────────┘    │
│           │              │              ▲               │
│           └──────────────┼──────────────┘               │
│           (Transfer)     │                              │
│                          │                              │
└──────────────────────────┴──────────────────────────────┘
```

---

## Scenarios & Solutions

### Scenario 1: Same Azure DevOps Organization

**Setup**: Both SF and VAIS in same ADO organization

```
Organization: Siemens Healthineers
├── Project: SF (TFS)
│   └── Repository: Consumer-API
│       └── pacts/
│           └── Consumer-Provider.json
└── Project: VAIS (ADO)
    └── Repository: Provider-API
        └── pacts/
            └── Consumer-Provider.json  ← Pull from SF
```

**Solution**: Git submodule or direct repository pull

---

### Scenario 2: Different Azure DevOps Organizations

**Setup**: SF in one organization, VAIS in another

```
Organization: SourceForge-TFS
└── Project: SF
    └── Repository: Consumer-API
        └── pacts/

Organization: Siemens-ADO
└── Project: VAIS
    └── Repository: Provider-API
        └── pacts/  ← Pull from different org
```

**Solution**: Pact Broker or Azure Artifacts

---

### Scenario 3: Mixed On-Premise & Cloud

**Setup**: SF on TFS (on-premise), VAIS on Azure DevOps (cloud)

**Solution**: Pact Broker with network connectivity or secure file transfer

---

## Method 1: Git Repository Sharing

### Simplest Approach: Git Submodule

#### Step 1: Create Pact Sharing Repository

**In SF (TFS) Organization**:

Create new repository: `Pact-Contracts`

```
pact-contracts/
├── README.md
├── .gitignore
└── pacts/
    ├── consumer-sf/
    │   ├── Consumer-Provider.json
    │   └── Consumer-OtherService.json
    └── consumer-other/
        └── *.json
```

#### Step 2: Push Consumer Pact Files

**In SF project's Azure Pipelines** (or local):

```yaml
# azure-pipelines-publish-pacts.yml (SF Project)
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - checkout: self
    fetchDepth: 1

  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      projects: '**/Consumer.Tests.csproj'
    displayName: 'Run Consumer Pact Tests'

  - task: PowerShell@2
    inputs:
      targetType: 'inline'
      script: |
        # Copy pact files to shared location
        mkdir -p pacts/consumer-sf
        cp -r ./pacts/* pacts/consumer-sf/
    displayName: 'Organize Pact Files'

  - task: Git@1
    inputs:
      repositoryName: 'pact-contracts'
      command: 'checkout'
      checkoutType: 'branch'
      branch: 'main'
    displayName: 'Checkout Pact Repository'

  - task: PowerShell@2
    inputs:
      targetType: 'inline'
      script: |
        cd pact-contracts
        
        # Update pact files
        cp -r ../pacts/consumer-sf/* ./pacts/consumer-sf/
        
        # Commit and push
        git config user.email "build@siemens.com"
        git config user.name "Azure Build"
        git add .
        git commit -m "Update pact files from SF consumer - Build $(Build.BuildId)"
        git push origin main
    displayName: 'Push Pact Files to Shared Repo'
```

#### Step 3: Pull in VAIS (ADO) Project

**In VAIS project's Azure Pipelines** (Provider verification):

```yaml
# azure-pipelines-verify-pacts.yml (VAIS Project)
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - checkout: self
    fetchDepth: 1

  - task: Git@1
    inputs:
      repositoryName: 'pact-contracts'
      command: 'checkout'
      checkoutType: 'branch'
      branch: 'main'
    displayName: 'Fetch Pact Files from SF'

  - task: PowerShell@2
    inputs:
      targetType: 'inline'
      script: |
        # Copy pact files to provider repo
        mkdir -p pacts
        cp pact-contracts/pacts/consumer-sf/* ./pacts/
    displayName: 'Setup Pact Files'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      projects: '**/Provider.Tests.csproj'
    displayName: 'Run Provider Pact Verification'
    env:
      PACT_FILE_PATH: $(Build.SourcesDirectory)/pacts
```

### Using Git Submodule (More Advanced)

#### Step 1: Add Submodule to VAIS

```bash
# In VAIS repository
git submodule add \
  https://dev.azure.com/siemens/sf/_git/pact-contracts \
  pact-contracts

git commit -m "Add pact-contracts submodule"
git push
```

#### Step 2: Update Submodule in CI/CD

```yaml
# azure-pipelines.yml (VAIS)
steps:
  - checkout: self
    submodules: true
    fetchDepth: 1

  - script: |
      cd pact-contracts
      git fetch origin main
      git checkout origin/main
      cd ..
      git add pact-contracts
      git commit -m "Update pact-contracts submodule" || true
      git push
    displayName: 'Update Pact Submodule'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      projects: '**/Provider.Tests.csproj'
      arguments: '--configuration Release'
```

---

## Method 2: Pact Broker

### Recommended for Production

#### Architecture

```
┌──────────────────────────────────────────────────┐
│         Azure Container Registry / VM             │
│                                                  │
│    ┌────────────────────────────────────────┐   │
│    │      Pact Broker (Centralized)         │   │
│    │                                        │   │
│    │  • Stores pact files                   │   │
│    │  • Manages versions                    │   │
│    │  • Tracks verification results         │   │
│    │  • REST API                            │   │
│    └────────────────────────────────────────┘   │
│                      ▲                           │
│        ┌─────────────┼──────────────┐            │
│        │             │              │            │
│        ▼             ▼              ▼            │
│    ┌────────┐  ┌─────────┐  ┌──────────┐       │
│    │  SF    │  │  VAIS   │  │  Other   │       │
│    │Consumer│  │Provider │  │Services  │       │
│    └────────┘  └─────────┘  └──────────┘       │
│      Publish      Verify       Publish          │
│                                                  │
└──────────────────────────────────────────────────┘
```

#### Step 1: Deploy Pact Broker

**Option A: Docker in Azure Container Instances**

```yaml
# deploy-pact-broker.yaml
apiVersion: batch/v1
kind: Deployment
metadata:
  name: pact-broker
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: pact-broker
        image: pactfoundation/pact-broker:latest
        ports:
        - containerPort: 9292
        env:
        - name: PACT_BROKER_DATABASE_URL
          value: "postgres://user:pass@postgres:5432/pact_broker"
        - name: PACT_BROKER_LOG_LEVEL
          value: "INFO"
```

**Option B: Azure Container Registry**

```bash
# Build and push Pact Broker image
docker pull pactfoundation/pact-broker:latest
docker tag pactfoundation/pact-broker:latest \
  myacr.azurecr.io/pact-broker:latest
docker push myacr.azurecr.io/pact-broker:latest
```

#### Step 2: Publish from SF (Consumer)

```csharp
// SF Project - After generating pacts
[Fact]
public async Task PublishPactToBroker()
{
    var pactBroker = new PactBroker("https://pact-broker.company.com");
    
    await pactBroker.PublishAsync(
        consumerName: "SF-Consumer",
        providerName: "VAIS-Provider",
        pactFile: "./pacts/SF-Consumer-VAIS-Provider.json",
        version: "1.0.0",
        branch: "main"
    );
}
```

**Azure Pipeline (SF)**:

```yaml
# Publish pact to broker
- task: PowerShell@2
  inputs:
    targetType: 'inline'
    script: |
      curl -X PUT \
        https://pact-broker.company.com/pacts/provider/VAIS-Provider/consumer/SF-Consumer/version/$(Build.BuildId) \
        -H "Content-Type: application/json" \
        -d @pacts/SF-Consumer-VAIS-Provider.json \
        -u "$(PACT_BROKER_USER):$(PACT_BROKER_PASS)"
  displayName: 'Publish Pact to Broker'
```

#### Step 3: Verify in VAIS (Provider)

```csharp
// VAIS Project - Verify pacts from broker
var verificationUri = new Uri("https://pact-broker.company.com");
var pactClient = new PactBrokerClient(verificationUri);

var result = await pactClient.VerifyAsync(
    providerName: "VAIS-Provider",
    consumerSelector: c => c.Name == "SF-Consumer"
);
```

**Azure Pipeline (VAIS)**:

```yaml
# Verify pacts from broker
- task: PowerShell@2
  inputs:
    targetType: 'inline'
    script: |
      pact-provider-verifier \
        -provider-base-url=http://localhost:5001 \
        -provider-states-url=http://localhost:5001/provider-states \
        -broker-url=https://pact-broker.company.com \
        -broker-username=$(PACT_BROKER_USER) \
        -broker-password=$(PACT_BROKER_PASS) \
        -broker-consumer-selector=SF-Consumer
  displayName: 'Verify Pacts from Broker'
```

---

## Method 3: Azure Artifacts

### Using Universal Packages

#### Step 1: Create Universal Package Feed

```yaml
# azure-pipelines-publish-pacts.yml (SF)
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  artifactFeedName: 'pact-contracts'
  packageName: 'sf-pacts'
  packageVersion: $(Build.BuildNumber)

steps:
  - checkout: self

  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      projects: '**/Consumer.Tests.csproj'
    displayName: 'Generate Pact Files'

  - task: PublishBuildArtifacts@1
    inputs:
      PathtoPublish: '$(Build.SourcesDirectory)/pacts'
      ArtifactName: 'pact-files'
      publishLocation: 'Container'
    displayName: 'Publish Artifacts'

  - task: UniversalPackages@0
    inputs:
      command: 'publish'
      publishDirectory: '$(Build.SourcesDirectory)/pacts'
      feedsToUsePublish: 'internal'
      externalFeedCredentials: ''
      vstsFeedPublish: 'siemens/$(artifactFeedName)'
      packagePublishDescription: 'Pact files from SF Consumer'
      versionOption: 'byBuildNumber'
    displayName: 'Publish Pacts to Artifacts'
```

#### Step 2: Consume in VAIS

```yaml
# azure-pipelines-verify-pacts.yml (VAIS)
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  artifactFeedName: 'pact-contracts'
  packageName: 'sf-pacts'

steps:
  - checkout: self

  - task: UniversalPackages@0
    inputs:
      command: 'download'
      feedsToUse: 'internal'
      vstsFeeds: 'siemens/$(artifactFeedName)'
      vstsFeedPackage: '$(packageName)'
      vstsPackageVersion: '*'
      downloadDirectory: '$(Build.SourcesDirectory)/pacts'
    displayName: 'Download Pacts from Artifacts'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      projects: '**/Provider.Tests.csproj'
    displayName: 'Verify Pacts'
    env:
      PACT_FILE_PATH: $(Build.SourcesDirectory)/pacts
```

---

## Method 4: Direct File Transfer

### Using Azure DevOps REST API

#### Step 1: Generate Pact (SF)

```yaml
# azure-pipelines.yml (SF)
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/Consumer.Tests.csproj'
  displayName: 'Generate Pact Files'
```

#### Step 2: Upload to VAIS Repository

```yaml
# azure-pipelines.yml (SF)
- task: PowerShell@2
  inputs:
    targetType: 'inline'
    script: |
      $org = "https://dev.azure.com/siemens"
      $project = "VAIS"
      $repo = "Provider-API"
      $filePath = "pacts/SF-Consumer-VAIS-Provider.json"
      $pactContent = Get-Content "./pacts/SF-Consumer-VAIS-Provider.json" -Raw
      
      # Upload file to VAIS repo using REST API
      $auth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$(System.AccessToken)"))
      
      $uploadUri = "$org/$project/_apis/git/repositories/$repo/items?path=$filePath&api-version=7.0"
      
      $body = @{
        content = $pactContent
        comment = "Update pact file from SF consumer - Build $(Build.BuildId)"
      } | ConvertTo-Json
      
      Invoke-RestMethod -Uri $uploadUri `
        -Method Put `
        -Headers @{Authorization = "Basic $auth"} `
        -ContentType "application/json" `
        -Body $body
    displayName: 'Upload Pact to VAIS Repository'
  env:
    SYSTEM_ACCESSTOKEN: $(System.AccessToken)
```

#### Step 3: Trigger VAIS Pipeline

```yaml
# Trigger VAIS provider verification
- task: PowerShell@2
  inputs:
    targetType: 'inline'
    script: |
      $org = "https://dev.azure.com/siemens"
      $project = "VAIS"
      $pipelineId = 123  # VAIS verification pipeline ID
      
      $auth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$(System.AccessToken)"))
      
      $uri = "$org/$project/_apis/pipelines/$pipelineId/runs?api-version=7.0-preview.1"
      
      $body = @{} | ConvertTo-Json
      
      Invoke-RestMethod -Uri $uri `
        -Method Post `
        -Headers @{Authorization = "Basic $auth"} `
        -ContentType "application/json" `
        -Body $body
    displayName: 'Trigger VAIS Provider Verification'
  env:
    SYSTEM_ACCESSTOKEN: $(System.AccessToken)
```

---

## CI/CD Pipeline Integration

### Complete End-to-End Pipeline

#### SF Project (Consumer - Generates Pacts)

```yaml
# azure-pipelines-sf.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - checkout: self

  - task: DotNetCoreCLI@2
    inputs:
      command: 'restore'
      projects: '**/Consumer.csproj'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'build'
      projects: '**/Consumer.csproj'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      projects: '**/Consumer.Tests.csproj'
    displayName: 'Run Consumer Pact Tests'

  - task: PublishBuildArtifacts@1
    inputs:
      PathtoPublish: '$(Build.SourcesDirectory)/pacts'
      ArtifactName: 'sf-pacts'

  # Method 1: Push to shared repo
  - script: |
      cd $(Build.SourcesDirectory)
      git clone https://dev.azure.com/siemens/sf/_git/pact-contracts pact-contracts
      cp -r pacts/* pact-contracts/pacts/consumer-sf/
      cd pact-contracts
      git config user.email "build@siemens.com"
      git config user.name "Azure Pipeline"
      git add .
      git commit -m "Pact update from SF - $(Build.BuildId)"
      git push origin main
    displayName: 'Push to Pact Repository'

  # Method 2: Publish to Pact Broker
  - script: |
      curl -X PUT \
        https://pact-broker.company.com/pacts/provider/VAIS-Provider/consumer/SF-Consumer/version/$(Build.BuildId) \
        -H "Content-Type: application/json" \
        -d @pacts/SF-Consumer-VAIS-Provider.json \
        -u "$(PACT_BROKER_USER):$(PACT_BROKER_PASS)"
    displayName: 'Publish to Pact Broker'

  # Method 3: Upload to Azure Artifacts
  - task: UniversalPackages@0
    inputs:
      command: 'publish'
      publishDirectory: '$(Build.SourcesDirectory)/pacts'
      vstsFeedPublish: 'siemens/pact-contracts'
      versionOption: 'byBuildNumber'

  # Trigger VAIS verification
  - task: TriggerBuild@3
    inputs:
      buildDefinition: 'VAIS Pact Verification'
      queueBuildForUserThatTriggeredBuild: true
      ignoreSslCertificateErrors: false
    displayName: 'Trigger VAIS Verification'
```

#### VAIS Project (Provider - Verifies Pacts)

```yaml
# azure-pipelines-vais.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - checkout: self

  # Method 1: Pull from shared repo
  - script: |
      git clone https://dev.azure.com/siemens/sf/_git/pact-contracts
      mkdir -p pacts
      cp -r pact-contracts/pacts/consumer-sf/* ./pacts/
    displayName: 'Fetch Pact Files'

  # Method 2: Download from Pact Broker
  - script: |
      mkdir -p pacts
      curl -X GET \
        "https://pact-broker.company.com/pacts/provider/VAIS-Provider/consumer/SF-Consumer/latest" \
        -H "Accept: application/json" \
        -u "$(PACT_BROKER_USER):$(PACT_BROKER_PASS)" \
        -o pacts/SF-Consumer-VAIS-Provider.json
    displayName: 'Download from Pact Broker'

  # Method 3: Download from Azure Artifacts
  - task: UniversalPackages@0
    inputs:
      command: 'download'
      feedsToUse: 'internal'
      vstsFeeds: 'siemens/pact-contracts'
      vstsFeedPackage: 'sf-pacts'
      vstsPackageVersion: '*'
      downloadDirectory: '$(Build.SourcesDirectory)/pacts'
    displayName: 'Download from Artifacts'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'build'
      projects: '**/Provider.csproj'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      projects: '**/Provider.Tests.csproj'
    displayName: 'Verify Pacts'
    env:
      PACT_FILE_PATH: $(Build.SourcesDirectory)/pacts

  - task: PublishTestResults@2
    inputs:
      testResultsFormat: 'VSTest'
      testResultsFiles: '**/TEST-*.xml'
      mergeTestResults: true
```

---

## Best Practices

### ✅ DO

1. **Version Pact Files**
   ```yaml
   # Use semantic versioning or build number
   pact-file-version: $(Build.BuildNumber)  # 20231126.1
   ```

2. **Tag Pacts by Branch**
   ```
   pacts/
   ├── main/
   ├── develop/
   └── feature-xyz/
   ```

3. **Implement Webhook Triggers**
   ```yaml
   # Automatically verify when consumer pushes pacts
   trigger:
     - main
   resources:
     webhooks:
       - webhook: pact-update
   ```

4. **Store Credentials Securely**
   ```yaml
   # Use Azure Key Vault
   - task: AzureKeyVault@1
     inputs:
       azureSubscription: 'MySubscription'
       KeyVaultName: 'MyVault'
       SecretsFilter: 'PactBrokerPassword'
   ```

5. **Monitor Verification Results**
   ```yaml
   - task: PublishTestResults@2
     inputs:
       testResultsFormat: 'JUnit'
       testResultsFiles: '**/pact-verification-*.xml'
   ```

### ❌ DON'T

1. **Don't hardcode URLs**
   - Use variables/parameters
   - Different per environment

2. **Don't forget cleanup**
   - Remove old pact files
   - Archive old versions

3. **Don't skip verification**
   - Always verify in CI/CD
   - Fail pipeline on verification failure

4. **Don't mix test data with pacts**
   - Keep pacts clean and focused
   - Separate test fixtures

5. **Don't ignore version mismatches**
   - Track pact versions
   - Document breaking changes

---

## Comparison: Methods Summary

| Method | Setup | Security | Scalability | Recommendation |
|--------|-------|----------|-------------|-----------------|
| **Git Submodule** | Easy | Medium | Low | Small teams |
| **Pact Broker** | Medium | High | High | Production |
| **Azure Artifacts** | Easy | High | Medium | Enterprise |
| **Direct API** | Complex | Medium | Low | Quick POC |

---

## For Your Specific Case (SF → VAIS)

### Recommended Setup

**Step 1: Quick Start (Immediate)**
- Use Git Submodule approach
- Create `pact-contracts` shared repo
- Both SF and VAIS reference it

**Step 2: Production (After Testing)**
- Deploy Pact Broker in Azure
- SF publishes pacts to broker
- VAIS pulls and verifies from broker

**Step 3: Enterprise (Long-term)**
- Use Pact Broker with webhooks
- Implement automated verification
- Track compatibility matrix
- Generate deployment reports

---

**Last Updated**: November 26, 2025
