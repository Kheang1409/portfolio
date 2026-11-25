#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Rollback script for KaiAssistant Portfolio

.DESCRIPTION
    Rolls back to a previous deployment by restoring Docker image tags.
    Supports quick rollback (previous tag) and git-based rollback.

.PARAMETER Type
    Rollback type: 'quick' (previous image tag) or 'git' (checkout commit)

.PARAMETER TargetTag
    For quick rollback: the tag to restore (default: 'previous')

.PARAMETER CommitHash
    For git rollback: the commit hash to checkout

.PARAMETER SkipVerification
    Skip post-rollback verification (default: false)

.EXAMPLE
    .\rollback.ps1 -Type quick

.EXAMPLE
    .\rollback.ps1 -Type quick -TargetTag backup-20261123-143000

.EXAMPLE
    .\rollback.ps1 -Type git -CommitHash abc1234
#>

param(
    [ValidateSet('quick', 'git')]
    [string]$Type = 'quick',

    [string]$TargetTag = 'previous',

    [string]$CommitHash = '',

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

Write-Log "=== KaiAssistant Rollback ==="
Write-Log "Type: $Type"

Set-Location $projectRoot

if ($Type -eq 'quick') {
    Write-Log "Quick rollback to tag: $TargetTag"

    # Verify target tags exist
    $backendTag = "portfolio-backend:$TargetTag"
    $frontendTag = "portfolio-frontend:$TargetTag"

    $backendExists = docker image inspect $backendTag -ErrorAction SilentlyContinue
    $frontendExists = docker image inspect $frontendTag -ErrorAction SilentlyContinue

    if (-not $backendExists -or -not $frontendExists) {
        Write-Error "Target images not found: $backendTag, $frontendTag"
        Write-Log "Available backend tags:"
        docker images portfolio-backend --format '{{.Tag}}'
        Write-Log "Available frontend tags:"
        docker images portfolio-frontend --format '{{.Tag}}'
        exit 1
    }

    # Tag current images as backup with timestamp
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    Write-Log "Backing up current images..."
    docker tag portfolio-backend:latest portfolio-backend:backup-$timestamp
    docker tag portfolio-frontend:latest portfolio-frontend:backup-$timestamp
    Write-Success "Current images backed up as backup-$timestamp"

    # Restore target tags to latest
    Write-Log "Restoring $TargetTag to latest..."
    docker tag $backendTag portfolio-backend:latest
    docker tag $frontendTag portfolio-frontend:latest
    Write-Success "Images restored"

    # Restart services
    Write-Log "Restarting services..."
    docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps backend frontend
    Write-Success "Services restarted"

} elseif ($Type -eq 'git') {
    if (-not $CommitHash) {
        Write-Error "Commit hash required for git rollback"
        exit 1
    }

    Write-Log "Git rollback to commit: $CommitHash"

    # Show recent commits for reference
    Write-Log "Recent commits:"
    git log --oneline -10

    # Fetch secrets first
    $fetchSecretsScript = Join-Path $scriptDir 'fetch-secrets.ps1'
    if (Test-Path $fetchSecretsScript) {
        Write-Log "Fetching secrets..."
        & $fetchSecretsScript
    }

    # Checkout target commit
    Write-Log "Checking out $CommitHash..."
    git checkout $CommitHash
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Git checkout failed"
        exit 1
    }

    # Rebuild and deploy
    Write-Log "Building images..."
    docker compose -f docker-compose.yml -f docker-compose.prod.yml build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        exit 1
    }

    Write-Log "Starting services..."
    docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Deployment failed"
        exit 1
    }

    # Return to main branch
    Write-Log "Returning to main branch..."
    git checkout main
}

# Verification
if (-not $SkipVerification) {
    Write-Log "Running post-rollback verification..."
    $verifyScript = Join-Path $scriptDir 'verify-deployment.ps1'
    if (Test-Path $verifyScript) {
        & $verifyScript
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Post-rollback verification failed"
            exit 1
        }
    } else {
        Write-Warning "verify-deployment.ps1 not found, skipping automated verification"
    }
}

Write-Success "=== Rollback Complete ==="