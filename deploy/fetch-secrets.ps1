#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fetch production secrets from secret store and generate .env file

.DESCRIPTION
    Retrieves secrets from AWS Secrets Manager (or other configured store)
    and writes them to .env file for Docker Compose.

    Supports multiple secret store backends via configuration.

.PARAMETER Provider
    Secret provider: 'aws', 'azure', 'vault', 'gcp', 'file' (default: 'aws')

.PARAMETER SecretName
    Name/ARN of the secret in the store (default: 'kai-assistant/production')

.PARAMETER OutputPath
    Path to write .env file (default: './.env')

.EXAMPLE
    .\fetch-secrets.ps1

.EXAMPLE
    .\fetch-secrets.ps1 -Provider aws -SecretName kai-assistant/production

.EXAMPLE
    .\fetch-secrets.ps1 -Provider file -SecretName ./secrets/.env.production
#>

param(
    [ValidateSet('aws', 'azure', 'vault', 'gcp', 'file')]
    [string]$Provider = 'aws',

    [string]$SecretName = 'kai-assistant/production',

    [string]$OutputPath = '.env'
)

$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $timestamp = Get-Date -Format 'HH:mm:ss'
    Write-Host "[$timestamp] [$Level] $Message"
}

function Write-Success { param([string]$Message) Write-Log $Message 'OK' }
function Write-Warning { param([string]$Message) Write-Log $Message 'WARN' }
function Write-Error { param([string]$Message) Write-Log $Message 'ERROR' }

Write-Log "=== Fetching Secrets from $Provider ==="
Write-Log "Secret: $SecretName"
Write-Log "Output: $OutputPath"

$secretJson = $null

switch ($Provider) {
    'aws' {
        Write-Log "Retrieving from AWS Secrets Manager..."
        try {
            $result = aws secretsmanager get-secret-value `
                --secret-id $SecretName `
                --query SecretString `
                --output text 2>$null
            if ($LASTEXITCODE -ne 0 -or -not $result) {
                Write-Error "AWS CLI failed or secret not found"
                exit 1
            }
            $secretJson = $result
            Write-Success "Secret retrieved from AWS"
        } catch {
            Write-Error "AWS CLI error: $($_.Exception.Message)"
            Write-Error "Ensure AWS CLI is configured with appropriate credentials"
            exit 1
        }
    }

    'azure' {
        Write-Log "Retrieving from Azure Key Vault..."
        # Requires: az keyvault secret show --vault-name <name> --name <secret> --query value -o tsv
        Write-Error "Azure Key Vault provider not yet implemented"
        exit 1
    }

    'vault' {
        Write-Log "Retrieving from HashiCorp Vault..."
        # Requires: vault kv get -format=json <path>
        Write-Error "HashiCorp Vault provider not yet implemented"
        exit 1
    }

    'gcp' {
        Write-Log "Retrieving from GCP Secret Manager..."
        # Requires: gcloud secrets versions access latest --secret=<name> --format='get(payload.data)' | base64 -d
        Write-Error "GCP Secret Manager provider not yet implemented"
        exit 1
    }

    'file' {
        Write-Log "Reading from local file..."
        if (-not (Test-Path $SecretName)) {
            Write-Error "Secret file not found: $SecretName"
            exit 1
        }
        $secretJson = Get-Content $SecretName -Raw
        Write-Success "Secret read from file"
    }
}

# Parse JSON and write .env
try {
    $secrets = $secretJson | ConvertFrom-Json
} catch {
    Write-Error "Failed to parse secret JSON: $($_.Exception.Message)"
    exit 1
}

$envLines = @()
foreach ($prop in $secrets.PSObject.Properties) {
    $key = $prop.Name
    $value = $prop.Value
    # Escape special characters for .env
    $value = $value -replace '"', '\"'
    $envLines += "$key=`"$value`""
}

$envContent = $envLines -join "`n"
Set-Content -Path $OutputPath -Value $envContent -Encoding UTF8

Write-Success ".env written to $OutputPath ($($envLines.Count) variables)"

# Verify required variables
$requiredVars = @(
    'MONGODB_CONNECTIONSTRING',
    'MONGODB_DATABASE',
    'GEMINI_API_KEY',
    'PAYPAL_CLIENT_ID',
    'PAYPAL_CLIENT_SECRET',
    'PAYPAL_ENVIRONMENT',
    'PAYPAL_WEBHOOK_ID',
    'PAYPAL_MERCHANT_ID',
    'SMTP_SERVER',
    'SMTP_PORT',
    'SMTP_SENDER_EMAIL',
    'SMTP_SENDER_PASSWORD',
    'SMTP_RECEIVER_EMAIL',
    'ALLOWED_ORIGINS',
    'REDIS__CONNECTIONSTRING',
    'BACKEND_API_URL'
)

$missing = @()
foreach ($var in $requiredVars) {
    if (-not ($envLines -match "^$var=")) {
        $missing += $var
    }
}

if ($missing.Count -gt 0) {
    Write-Warning "Missing required variables: $($missing -join ', ')"
} else {
    Write-Success "All required variables present"
}

# Secure the file
try {
    # On Windows, restrict to current user
    $acl = Get-Acl $OutputPath
    $acl.SetAccessRuleProtection($true, $false)
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($env:USERNAME, 'FullControl', 'Allow')
    $acl.SetAccessRule($rule)
    Set-Acl $OutputPath $acl
    Write-Log "File permissions restricted to current user"
} catch {
    Write-Warning "Could not restrict file permissions: $($_.Exception.Message)"
}