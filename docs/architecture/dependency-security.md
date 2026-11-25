# Dependency Security

## Overview

Supply chain security practices for both .NET (NuGet) and Node.js (npm) dependencies.

---

## NuGet (.NET Backend)

### Automated Scanning (CI)

```yaml
# .github/workflows/ci-security.yml
- name: Run NuGet vulnerability audit
  run: dotnet list KaiAssistant.sln package --vulnerable --include-transitive

- name: Run NuGet audit with SARIF output
  run: |
    dotnet tool install --global dotnet-scan || true
    export PATH="$PATH:$HOME/.dotnet/tools"
    dotnet scan KaiAssistant.sln --format sarif --output nuget-audit.sarif
```

### Manual Commands

```powershell
# List vulnerable packages (direct + transitive)
dotnet list KaiAssistant.sln package --vulnerable --include-transitive

# List outdated packages
dotnet list KaiAssistant.sln package --outdated --include-transitive

# Audit with SARIF (for GitHub Security tab)
dotnet tool install --global dotnet-scan
dotnet scan KaiAssistant.sln --format sarif --output nuget-audit.sarif
```

### Policy

| Severity | Action |
|----------|--------|
| **Critical** | Block merge; upgrade immediately |
| **High** | Block merge; upgrade within 48h |
| **Medium** | Warn; upgrade in next sprint |
| **Low** | Informational; track |

### Approved Package Sources

```xml
<!-- nuget.config -->
<packageSources>
  <clear />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  <!-- No private feeds without security review -->
</packageSources>
```

### Central Package Management

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Pinned versions with security review -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageVersion Include="MongoDB.Driver" Version="3.0.0" />
    <PackageVersion Include="StackExchange.Redis" Version="2.8.0" />
    <PackageVersion Include="Polly" Version="8.4.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageVersion Include="FluentValidation" Version="11.9.0" />
    <PackageVersion Include="MediatR" Version="12.4.0" />
    <PackageVersion Include="xunit" Version="2.9.0" />
    <PackageVersion Include="Moq" Version="4.20.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
</Project>
```

---

## npm (Frontend)

### Automated Scanning (CI)

```yaml
# .github/workflows/ci-security.yml
- name: Run npm audit
  run: npm audit --audit-level=high

- name: Run npm audit with SARIF output
  run: |
    npm audit --json > npm-audit.json || true
    npx audit-ci --config audit-ci.json --output sarif --output-file npm-audit.sarif
```

### Audit CI Config

```json
// frontend/audit-ci.json
{
  "ignore": [
    "GHSA-xxxx-xxxx-xxxx"  // Specific advisory IDs with justification
  ],
  "moderate": "warn",
  "high": "error",
  "critical": "error"
}
```

### Manual Commands

```bash
# Audit production deps only
npm audit --omit=dev --audit-level=high

# Fix automatically (non-breaking)
npm audit fix

# Fix including breaking changes (review carefully!)
npm audit fix --force

# Generate SARIF for GitHub
npm audit --json > npm-audit.json
npx audit-ci --config audit-ci.json --output sarif --output-file npm-audit.sarif
```

### Policy

| Severity | Action |
|----------|--------|
| **Critical** | Block merge; fix immediately |
| **High** | Block merge; fix within 48h |
| **Moderate** | Warn; fix in next sprint |
| **Low** | Informational |

### Dependency Pinning

```json
// package.json - exact versions for production deps
{
  "dependencies": {
    "next": "16.2.1",
    "react": "18.2.0",
    "react-dom": "18.2.0"
    // No ^ or ~ for production
  },
  "devDependencies": {
    // Dev deps can use ^ for minor updates
    "typescript": "^5.2.0",
    "eslint": "^10.10.0"
  }
}
```

---

## Container Security (Trivy)

### CI Scanning

```yaml
# .github/workflows/ci-security.yml
- name: Build backend Docker image
  run: docker build -t portfolio-backend:scan -f backend/Dockerfile .

- name: Run Trivy vulnerability scanner on backend image
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: 'portfolio-backend:scan'
    format: 'sarif'
    output: 'trivy-backend.sarif'
    severity: 'CRITICAL,HIGH'
    vuln-type: 'os,library'
    ignore-unfixed: true
```

### Base Image Policy

| Image | Policy |
|-------|--------|
| `mcr.microsoft.com/dotnet/sdk:10.0` | Microsoft maintained, monthly updates |
| `mcr.microsoft.com/dotnet/aspnet:10.0` | Microsoft maintained, monthly updates |
| `node:24-alpine` | Alpine Linux, minimal attack surface |

**Rebuild Schedule**: Weekly CI rebuild to pick up base image patches

---

## Secret Scanning (TruffleHog)

### CI Scanning

```yaml
# .github/workflows/ci-security.yml
- name: Run secret scanning (truffleHog)
  uses: trufflesecurity/trufflehog@master
  with:
    path: ./backend
    base: ${{ github.event.pull_request.base.sha || github.sha }}
    head: ${{ github.event.pull_request.head.sha || github.sha }}
    extra_args: --json --fail
```

### Pre-commit Hook (Optional)

```yaml
# .pre-commit-config.yaml
repos:
  - repo: https://github.com/trufflesecurity/trufflehog
    rev: v3.100.1
    hooks:
      - id: trufflehog
        args: [--json, --fail]
```

---

## CodeQL Analysis

### CI Configuration

```yaml
# .github/workflows/ci-security.yml
- name: Initialize CodeQL
  uses: github/codeql-action/init@v3
  with:
    languages: csharp, javascript-typescript
    queries: +security-extended,+security-and-quality

- name: Perform CodeQL Analysis
  uses: github/codeql-action/analyze@v3
```

### Custom Queries (Optional)

```yaml
# .github/codeql/codeql-config.yml
queries:
  - uses: ./custom-queries
paths-ignore:
  - "**/bin/**"
  - "**/obj/**"
  - "**/node_modules/**"
  - "**/.next/**"
```

---

## Dependency Review (PR Gate)

```yaml
# .github/workflows/ci-security.yml
- name: Review dependencies
  uses: actions/dependency-review-action@v4
  with:
    config-file: '.github/dependency-review-config.yml'
    fail-on-severity: 'high'
```

### Review Config

```yaml
# .github/dependency-review-config.yml
fail-on-severity: high
allow-ghsas:
  # Documented exceptions with justification
  - GHSA-xxxx-xxxx-xxxx  # "False positive: not reachable in our code paths"
```

---

## SBOM Generation

```bash
# Backend
dotnet sbom KaiAssistant.sln -o sbom-backend.spdx.json

# Frontend
cd frontend
npx @cyclonedx/bom@latest -o sbom-frontend.json
```

---

## Vulnerability Response Process

1. **Detect**: CI scan fails or GitHub Dependabot alert
2. **Triage**: Assess exploitability in our context
3. **Fix**: Upgrade package, test, verify
4. **Deploy**: Merge to main, deploy to staging, then prod
5. **Verify**: Post-deploy scan passes

### SLA by Severity

| Severity | Detection → Fix | Fix → Deploy |
|----------|-----------------|--------------|
| Critical | 4 hours | 1 hour |
| High | 24 hours | 4 hours |
| Medium | 1 week | 1 day |
| Low | Next sprint | Next deploy |

---

## Approved Exceptions (Documented)

| Advisory | Package | Justification | Expires |
|----------|---------|---------------|---------|
| GHSA-xxxx | Example.Package | Not reachable; mitigated by input validation | 2026-12-31 |

---

## Related Documents

- [CI Security Workflow](../.github/workflows/ci-security.yml) — Implementation
- [Production Runbook](../deployment/production-runbook.md) — Deployment with security gates
- [Architecture Overview](system-overview.md) — Technology stack