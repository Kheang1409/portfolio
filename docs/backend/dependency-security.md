# Backend Dependency Security

## NuGet Security Practices

### Central Package Management

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Framework -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Versioning" Version="5.0.0" />
    
    <!-- Data -->
    <PackageVersion Include="MongoDB.Driver" Version="3.0.0" />
    <PackageVersion Include="MongoDB.Driver.Core" Version="3.0.0" />
    <PackageVersion Include="StackExchange.Redis" Version="2.8.0" />
    
    <!-- Resilience -->
    <PackageVersion Include="Polly" Version="8.4.0" />
    <PackageVersion Include="Polly.Extensions.Http" Version="3.0.0" />
    
    <!-- Observability -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OTLP" Version="1.9.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.MongoDB" Version="1.9.0" />
    
    <!-- Validation -->
    <PackageVersion Include="FluentValidation" Version="11.9.0" />
    <PackageVersion Include="FluentValidation.AspNetCore" Version="11.3.0" />
    
    <!-- Mediator -->
    <PackageVersion Include="MediatR" Version="12.4.0" />
    <PackageVersion Include="MediatR.Contracts" Version="2.0.0" />
    
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.0" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.0" />
    <PackageVersion Include="Moq" Version="4.20.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageVersion Include="Testcontainers.MongoDB" Version="3.0.0" />
    <PackageVersion Include="Testcontainers.Redis" Version="3.0.0" />
  </ItemGroup>
</Project>
```

---

## Automated Vulnerability Scanning

### CI Pipeline (GitHub Actions)

```yaml
# .github/workflows/ci-security.yml
- name: Run NuGet vulnerability audit
  run: dotnet list KaiAssistant.sln package --vulnerable --include-transitive

- name: Run NuGet audit with SARIF output
  run: |
    dotnet tool install --global dotnet-scan || true
    export PATH="$PATH:$HOME/.dotnet/tools"
    dotnet scan KaiAssistant.sln --format sarif --output nuget-audit.sarif

- name: Upload NuGet audit results to GitHub Security
  uses: github/codeql-action/upload-sarif@v3
  if: always()
  with:
    sarif_file: backend/nuget-audit.sarif
    category: nuget-audit
```

### Manual Commands

```powershell
# List vulnerable packages (direct + transitive)
dotnet list KaiAssistant.sln package --vulnerable --include-transitive

# List outdated packages
dotnet list KaiAssistant.sln package --outdated --include-transitive

# Generate SARIF for GitHub Security tab
dotnet tool install --global dotnet-scan
dotnet scan KaiAssistant.sln --format sarif --output nuget-audit.sarif
```

---

## Package Source Security

```xml
<!-- nuget.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  
  <disabledPackageSources>
    <!-- No private feeds without security review -->
  </disabledPackageSources>
  
  <config>
    <add key="globalPackagesFolder" value="~/.nuget/packages" />
  </config>
</configuration>
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
# Generate SBOM (SPDX format)
dotnet sbom KaiAssistant.sln -o sbom-backend.spdx.json

# Or CycloneDX
dotnet tool install --global CycloneDX
cyclonedx dotnet KaiAssistant.sln -o sbom-backend.cyclonedx.json
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

## Transitive Dependency Pinning

```xml
<!-- Directory.Packages.props -->
<PropertyGroup>
  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>

<!-- Override specific transitive if needed -->
<ItemGroup>
  <PackageVersion Include="System.Text.Json" Version="9.0.0" Override="true" />
</ItemGroup>
```

---

## Related Documents

- [Architecture Dependency Security](../architecture/dependency-security.md) — Full policy
- [CI Security Workflow](../.github/workflows/ci-security.yml) — Implementation
- [Production Runbook](../deployment/production-runbook.md) — Deployment with security gates