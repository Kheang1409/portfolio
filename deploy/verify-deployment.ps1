#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Post-deployment verification for KaiAssistant Portfolio

.DESCRIPTION
    Verifies that all services are healthy and responding correctly after deployment.
    Runs health checks and smoke tests against the deployed application.

.PARAMETER BaseUrl
    Frontend base URL (default: http://localhost:3000)

.PARAMETER ApiUrl
    Backend API base URL (default: http://localhost:5000)

.PARAMETER TimeoutSeconds
    Maximum time to wait for health checks (default: 120)

.PARAMETER ExternalUrl
    External domain URL for reverse proxy verification (optional)

.EXAMPLE
    .\verify-deployment.ps1

.EXAMPLE
    .\verify-deployment.ps1 -ExternalUrl https://yourdomain.com
#>

param(
    [string]$BaseUrl = 'http://localhost:3000',
    [string]$ApiUrl = 'http://localhost:5000',
    [int]$TimeoutSeconds = 120,
    [string]$ExternalUrl = ''
)

$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $timestamp = Get-Date -Format 'HH:mm:ss'
    Write-Host "[$timestamp] [$Level] $Message"
}

function Write-Success { param([string]$Message) Write-Log $Message 'PASS' }
function Write-Warning { param([string]$Message) Write-Log $Message 'WARN' }
function Write-Error { param([string]$Message) Write-Log $Message 'FAIL' }

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = 'GET',
        [string]$Body = $null,
        [int[]]$ExpectedStatuses = @(200)
    )
    try {
        $ht = @{
            Method = $Method
            Uri = $Url
            ContentType = 'application/json'
            UseBasicParsing = $true
        }
        if ($Body) { $ht.Body = $Body }
        $response = Invoke-WebRequest @ht
        $status = $response.StatusCode
        $content = $response.Content
    } catch [System.Net.WebException] {
        $status = $_.Exception.Response.StatusCode
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $content = $reader.ReadToEnd()
    } catch {
        Write-Error "$Name - Exception: $($_.Exception.Message)"
        return $false
    }

    if ($ExpectedStatuses -contains $status) {
        Write-Success "$Name - Status: $status"
        return $true
    }

    # 400 with validation error = endpoint functional
    if ($status -eq 400 -and $content -match 'validation|required') {
        Write-Success "$Name - Status: $status (validation = functional)"
        return $true
    }

    Write-Error "$Name - Expected: $($ExpectedStatuses -join ',') Got: $status"
    if ($content) { Write-Error "  Response: $content" }
    return $false
}

Write-Log "=== Post-Deployment Verification ==="
Write-Log "Base URL: $BaseUrl"
Write-Log "API URL: $ApiUrl"
if ($ExternalUrl) { Write-Log "External URL: $ExternalUrl" }

$failed = 0
$startTime = Get-Date

# Phase 1: Wait for container health checks
Write-Log "Phase 1: Waiting for container health checks..."
$maxWait = $TimeoutSeconds
$interval = 5
$elapsed = 0

while ($elapsed -lt $maxWait) {
    $containers = docker compose -f docker-compose.yml -f docker-compose.prod.yml ps --format 'json' | ConvertFrom-Json
    $allHealthy = $true
    foreach ($c in $containers) {
        $health = $c.Health
        $state = $c.State
        if ($state -ne 'running') {
            Write-Log "Container $($c.Name) not running (state: $state)"
            $allHealthy = $false
        } elseif ($health -and $health -ne 'healthy') {
            Write-Log "Container $($c.Name) not healthy (health: $health)"
            $allHealthy = $false
        }
    }
    if ($allHealthy) {
        Write-Success "All containers healthy"
        break
    }
    Start-Sleep -Seconds $interval
    $elapsed += $interval
}

if ($elapsed -ge $maxWait) {
    Write-Error "Timeout waiting for healthy containers after ${maxWait}s"
    $failed++
}

# Phase 2: Health endpoint verification
Write-Log "Phase 2: Verifying health endpoints..."

$healthChecks = @(
    @{ Name = 'Backend Liveness'; Url = "$ApiUrl/health/live" },
    @{ Name = 'Backend Readiness'; Url = "$ApiUrl/health/ready" },
    @{ Name = 'Frontend Liveness'; Url = "$BaseUrl/api/health/live" }
)

foreach ($check in $healthChecks) {
    if (-not (Test-Endpoint $check.Name $check.Url)) { $failed++ }
}

# Phase 3: Smoke tests
Write-Log "Phase 3: Running smoke tests..."

$smokeTests = @(
    @{ Name = 'Homepage'; Url = "$BaseUrl/"; Method = 'GET' },
    @{ Name = 'Resume Page'; Url = "$BaseUrl/resume"; Method = 'GET' },
    @{ Name = 'PDF Download'; Url = "$BaseUrl/resume.pdf"; Method = 'GET' },
    @{ Name = 'GitHub Feed'; Url = "$BaseUrl/api/github/projects"; Method = 'GET' },
    @{ Name = 'AI Assistant'; Url = "$ApiUrl/api/assistant"; Method = 'POST'; Body = '{"prompt":"Health check test","stream":false}'; Expected = @(200, 400) },
    @{ Name = 'Contact Form'; Url = "$ApiUrl/api/contacts"; Method = 'POST'; Body = '{"name":"Verify","email":"verify@test.com","subject":"Test","message":"Deployment verification"}' },
    @{ Name = 'Visitor Tracking'; Url = "$ApiUrl/api/visits"; Method = 'POST'; Body = '{"path":"/verify"}'; Expected = @(200, 202) },
    @{ Name = 'Donation Create Order'; Url = "$ApiUrl/api/donations/orders"; Method = 'POST'; Body = '{"amount":1,"currency":"USD","donorEmail":"verify@test.com","donorName":"Verify","idempotencyKey":"verify-' + (Get-Date -Format 'yyyyMMddHHmmss') + '"}'; Expected = @(200, 400) },
    @{ Name = '404 Page'; Url = "$BaseUrl/verify-nonexistent-$(Get-Random)"; Method = 'GET'; Expected = @(404) }
)

foreach ($test in $smokeTests) {
    $expected = if ($test.Expected) { $test.Expected } else { @(200) }
    if (-not (Test-Endpoint $test.Name $test.Url $test.Method $test.Body $expected)) { $failed++ }
}

# Phase 4: External access verification (if provided)
if ($ExternalUrl) {
    Write-Log "Phase 4: Verifying external access via reverse proxy..."
    $extChecks = @(
        @{ Name = 'External Frontend'; Url = "$ExternalUrl/" },
        @{ Name = 'External API Health'; Url = "$ExternalUrl/api/health/live" },
        @{ Name = 'External Backend Ready'; Url = "$ExternalUrl/api/health/ready" }
    )
    foreach ($check in $extChecks) {
        if (-not (Test-Endpoint $check.Name $check.Url)) { $failed++ }
    }
}

# Summary
$duration = [math]::Round((Get-Date) - $startTime).TotalSeconds
Write-Log "=== Verification Complete (${duration}s) ==="
Write-Log "Failed: $failed"

if ($failed -eq 0) {
    Write-Success "ALL VERIFICATIONS PASSED"
    exit 0
} else {
    Write-Error "$failed VERIFICATION(S) FAILED"
    exit 1
}