#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Production deployment script for KaiAssistant Portfolio

.DESCRIPTION
    Deploys the application using Docker Compose with production configuration.
    Supports initial deployment and updates.

.PARAMETER Action
    Deployment action: 'initial' or 'update' (default: 'update')

.PARAMETER SkipBuild
    Skip Docker image build (default: false)

.PARAMETER SkipVerification
    Skip post-deployment verification (default: false)

.EXAMPLE
    .\deploy.ps1 -Action initial

.EXAMPLE
    .\deploy.ps1 -Action update

.EXAMPLE
    .\deploy.ps1 -Action update -SkipBuild
#>

param(
    [ValidateSet('initial', 'update')]
    [string]$Action = 'update',

    [switch]$SkipBuild,
    [switch]$SkipVerification
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = Join-Path $scriptDir '..'

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Write-Host "[$timestamp] [$Level] $Message"
}

function Write-Success { param([string]$Message) Write-Log $Message 'SUCCESS' }
function Write-Warning { param([string]$Message) Write-Log $Message 'WARN' }
function Write-Error { param([string]$Message) Write-Log $Message 'ERROR' }

# Verify prerequisites
Write-Log "=== KaiAssistant Production Deployment ==="
Write-Log "Action: $Action"

# Check Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker not found. Please install Docker Engine."
    exit 1
}
Write-Success "Docker found: $(docker --version)"

# Check Docker Compose
if (-not (docker compose version -ErrorAction SilentlyContinue)) {
    Write-Error "Docker Compose v2 not found. Please install Docker Compose plugin."
    exit 1
}
Write-Success "Docker Compose found: $(docker compose version)"

# Verify required files exist
$composeFiles = @('docker-compose.yml', 'docker-compose.prod.yml')
foreach ($file in $composeFiles) {
    $path = Join-Path $projectRoot $file
    if (-not (Test-Path $path)) {
        Write-Error "Required file not found: $file"
        exit 1
    }
}
Write-Success "Docker Compose files verified"

# Verify .env exists
$envPath = Join-Path $projectRoot '.env'
if (-not (Test-Path $envPath)) {
    Write-Warning ".env file not found at $envPath"
    Write-Warning "Create .env from secret store or .env.example before deployment"
    if ($Action -eq 'initial') {
        Write-Error "Aborting initial deployment - .env required"
        exit 1
    }
}

# Verify redis-certs volume exists (production)
$volumeExists = docker volume ls --format '{{.Name}}' | Where-Object { $_ -eq 'redis-certs' }
if (-not $volumeExists) {
    Write-Warning "redis-certs volume not found. Creating..."
    docker volume create redis-certs
    Write-Warning "IMPORTANT: Copy CA-signed Redis TLS certificates to the redis-certs volume:"
    Write-Warning "  docker run --rm -v redis-certs:/certs -v <path-to-certs>:/source alpine cp /source/*.crt /source/*.key /certs/"
}

# Change to project root
Set-Location $projectRoot

# Fetch secrets if script exists
$fetchSecretsScript = Join-Path $scriptDir 'fetch-secrets.ps1'
if (Test-Path $fetchSecretsScript) {
    Write-Log "Fetching secrets from secret store..."
    & $fetchSecretsScript
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Secret fetch script returned non-zero exit code"
    }
} else {
    Write-Warning "No fetch-secrets.ps1 found. Ensure .env is populated manually."
}

# Build images
if (-not $SkipBuild) {
    Write-Log "Building Docker images..."
    $buildCmd = 'docker compose -f docker-compose.yml -f docker-compose.prod.yml build'
    Write-Log "Executing: $buildCmd"
    $result = Invoke-Expression $buildCmd
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker build failed"
        exit 1
    }
    Write-Success "Docker images built successfully"
} else {
    Write-Log "Skipping build (--SkipBuild specified)"
}

# Deploy
Write-Log "Starting deployment..."
if ($Action -eq 'initial') {
    $deployCmd = 'docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build'
} else {
    $deployCmd = 'docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps backend frontend'
}
Write-Log "Executing: $deployCmd"
$result = Invoke-Expression $deployCmd
if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed"
    exit 1
}
Write-Success "Containers started"

# Verification
if (-not $SkipVerification) {
    Write-Log "Running post-deployment verification..."
    $verifyScript = Join-Path $scriptDir 'verify-deployment.ps1'
    if (Test-Path $verifyScript) {
        & $verifyScript
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Post-deployment verification failed"
            exit 1
        }
    } else {
        Write-Warning "verify-deployment.ps1 not found, skipping automated verification"
        Write-Log "Run manual verification: curl http://localhost:5000/health/ready"
    }
}

Write-Success "=== Deployment Complete ==="
Write-Log "Backend: http://localhost:5000"
Write-Log "Frontend: http://localhost:3000"
Write-Log "Health: http://localhost:5000/health/ready"