# AWS Lambda Deployment (Alternative)

## Overview

The backend can be deployed to **AWS Lambda** via **SAM (Serverless Application Model)** as an alternative to Docker Compose. Frontend remains on Docker/Node.js or can be deployed to Amplify/CloudFront + S3.

---

## Architecture (Lambda)

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Frontend   │────▶│  API Gateway │────▶│  Lambda     │
│  (Docker/   │     │  (REST API)  │     │  (.NET 10)  │
│   Amplify)  │     └──────┬──────┘     └──────┬──────┘
└─────────────┘            │                   │
                           ▼                   ▼
                    ┌─────────────┐     ┌─────────────┐
                    │  MongoDB    │     │  ElastiCache│
                    │  (Atlas)    │     │  (Redis)    │
                    └─────────────┘     └─────────────┘
```

---

## Prerequisites

| Resource | Purpose |
|----------|---------|
| AWS Account | Deployment target |
| AWS CLI v2 | CLI operations |
| SAM CLI | Build/deploy |
| MongoDB Atlas | Database (external) |
| ElastiCache Redis | Cache/rate limit |
| IAM Role | Lambda execution role |
| CloudFormation Role | Stack deployment role |

---

## SAM Template (`backend/infra/template.yaml`)

```yaml
AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31
Description: KaiAssistant API

Globals:
  Function:
    Runtime: dotnet10
    Architecture: arm64
    Timeout: 30
    MemorySize: 512
    Environment:
      Variables:
        ASPNETCORE_ENVIRONMENT: Production
        ASPNETCORE_URLS: http://+:8080
        MONGODB_CONNECTIONSTRING: {{resolve:secretsmanager:kaiassistant/mongodb:SecretString:connectionString}}
        MONGODB_DATABASE: KaiAssistant
        REDIS__CONNECTIONSTRING: {{resolve:secretsmanager:kaiassistant/redis:SecretString:connectionString}}
        REDIS__STRICTDISTRIBUTEDMODE: true
        GEMINI_API_KEY: {{resolve:secretsmanager:kaiassistant/gemini:SecretString:apiKey}}
        PAYPAL_CLIENT_ID: {{resolve:secretsmanager:kaiassistant/paypal:SecretString:clientId}}
        PAYPAL_CLIENT_SECRET: {{resolve:secretsmanager:kaiassistant/paypal:SecretString:clientSecret}}
        PAYPAL_ENVIRONMENT: live
        PAYPAL_WEBHOOK_ID: {{resolve:secretsmanager:kaiassistant/paypal:SecretString:webhookId}}
        PAYPAL_MERCHANT_ID: {{resolve:secretsmanager:kaiassistant/paypal:SecretString:merchantId}}
        SMTP_SERVER: smtp.gmail.com
        SMTP_PORT: 587
        SMTP_SENDER_EMAIL: {{resolve:secretsmanager:kaiassistant/smtp:SecretString:senderEmail}}
        SMTP_SENDER_PASSWORD: {{resolve:secretsmanager:kaiassistant/smtp:SecretString:senderPassword}}
        SMTP_RECEIVER_EMAIL: {{resolve:secretsmanager:kaiassistant/smtp:SecretString:receiverEmail}}
        SMTP_ENABLED: true
        ALLOWED_ORIGINS: https://yourdomain.com,https://www.yourdomain.com
        OTEL_EXPORTER_OTLP_ENDPOINT: 
        OTEL_SERVICE_NAME: kai-assistant-api

Resources:
  ApiFunction:
    Type: AWS::Serverless::Function
    Properties:
      FunctionName: kaiassistant-api
      CodeUri: ../KaiAssistant.API/bin/Release/net10.0/publish
      Handler: KaiAssistant.API::KaiAssistant.API.LambdaEntryPoint::FunctionHandlerAsync
      Policies:
        - AWSLambdaBasicExecutionRole
        - SecretsManagerReadWrite
      Events:
        ApiEvent:
          Type: HttpApi
          Properties:
            ApiId: !Ref HttpApi
            Method: ANY
            Path: /{proxy+}

  HttpApi:
    Type: AWS::Serverless::HttpApi
    Properties:
      StageName: prod
      CorsConfiguration:
        AllowOrigins:
          - https://yourdomain.com
          - https://www.yourdomain.com
        AllowMethods:
          - GET
          - POST
          - OPTIONS
        AllowHeaders:
          - Content-Type
          - Authorization
        MaxAge: 300

  # Secrets (created manually or via external process)
  # MongoDB, Redis, Gemini, PayPal, SMTP secrets in Secrets Manager

Outputs:
  ApiUrl:
    Description: API Gateway URL
    Value: !Sub https://${HttpApi}.execute-api.${AWS::Region}.amazonaws.com/prod/
```

---

## Lambda Entry Point

```csharp
// KaiAssistant.API/LambdaEntryPoint.cs
using Amazon.Lambda.AspNetCoreServer;

public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseStartup<Startup>();
    }
}
```

**NuGet**: `Amazon.Lambda.AspNetCoreServer` (version 8.0+)

---

## Build & Deploy

### 1. Build .NET Project

```powershell
cd backend
dotnet restore KaiAssistant.sln
dotnet build KaiAssistant.sln --configuration Release
dotnet publish KaiAssistant.API/KaiAssistant.API.csproj -c Release -o ./publish
```

### 2. SAM Build

```bash
cd backend/infra
sam build --template template.yaml
```

### 3. Deploy

```bash
sam deploy \
  --template .aws-sam/build/template.yaml \
  --stack-name kaiassistant-api-stack \
  --capabilities CAPABILITY_IAM CAPABILITY_NAMED_IAM \
  --s3-bucket kaiassistant-lambda-deployments \
  --parameter-overrides \
    MongoConnectionString=<value> \
    RedisConnectionString=<value> \
    MongoDatabaseName=KaiAssistant \
    AllowedOrigins=https://yourdomain.com,https://www.yourdomain.com \
    PayPalClientId=<value> \
    PayPalClientSecret=<value> \
    PayPalEnvironment=live \
    PayPalWebhookId=<value> \
    PayPalMerchantId=<value> \
    SmtpSenderEmail=<value> \
    SmtpReceiverEmail=<value> \
    SmtpSenderPassword=<value> \
    SmtpEnabled=true \
    ASPNetCoreEnvironment=Production \
    GeminiApiKey=<value>
```

---

## GitHub Actions Deploy (`.github/workflows/deploy-lambda.yml`)

```yaml
name: Deploy to AWS Lambda

on:
  push:
    branches:
      - production
  workflow_dispatch:

env:
  AWS_REGION: us-east-1
  DOTNET_VERSION: "10.0.x"

jobs:
  deploy:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      id-token: write

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install AWS SAM CLI
        uses: aws-actions/setup-sam@v2
        with:
          use-installer: true

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: ${{ secrets.AWS_DEPLOY_ROLE_ARN }}
          aws-region: ${{ env.AWS_REGION }}
          role-session-name: KaiAssistantGitHubActions

      - name: Verify assumed AWS role
        run: aws sts get-caller-identity

      - name: Validate CloudFormation execution role
        env:
          CLOUDFORMATION_EXECUTION_ROLE_ARN: ${{ secrets.AWS_CLOUDFORMATION_EXECUTION_ROLE_ARN }}
        run: |
          set -euo pipefail
          if [[ ! "$CLOUDFORMATION_EXECUTION_ROLE_ARN" =~ ^arn:aws:iam::[0-9]{12}:role/.+ ]]; then
            echo "::error::Set the AWS_CLOUDFORMATION_EXECUTION_ROLE_ARN repository secret."
            exit 1
          fi

      - name: Restore dependencies
        run: dotnet restore

      - name: Audit NuGet dependencies
        run: dotnet list KaiAssistant.sln package --vulnerable --include-transitive

      - name: Build solution
        run: dotnet build KaiAssistant.sln --configuration Release --no-restore

      - name: Validate SAM template
        run: sam validate --template infra/template.yaml

      - name: SAM Build
        run: sam build --template infra/template.yaml

      - name: Validate PayPal deployment secrets
        env:
          PAYPAL_CLIENT_ID: ${{ secrets.PAYPAL_CLIENT_ID }}
          PAYPAL_CLIENT_SECRET: ${{ secrets.PAYPAL_CLIENT_SECRET }}
          PAYPAL_ENVIRONMENT: ${{ secrets.PAYPAL_ENVIRONMENT }}
          PAYPAL_WEBHOOK_ID: ${{ secrets.PAYPAL_WEBHOOK_ID }}
        run: |
          set -euo pipefail
          if [[ -z "$PAYPAL_CLIENT_ID" || -z "$PAYPAL_CLIENT_SECRET" || -z "$PAYPAL_WEBHOOK_ID" ]]; then
            echo "::error::Configure PAYPAL_CLIENT_ID, PAYPAL_CLIENT_SECRET, and PAYPAL_WEBHOOK_ID as GitHub Actions secrets."
            exit 1
          fi
          if [[ "$PAYPAL_ENVIRONMENT" != "sandbox" && "$PAYPAL_ENVIRONMENT" != "live" ]]; then
            echo "::error::Set PAYPAL_ENVIRONMENT to exactly sandbox or live."
            exit 1
          fi
          if [[ "$PAYPAL_WEBHOOK_ID" == http* || "$PAYPAL_WEBHOOK_ID" == */* ]]; then
            echo "::error::PAYPAL_WEBHOOK_ID must be the ID from PayPal, not a URL."
            exit 1
          fi

      - name: Check and clean failed stack
        run: |
          STACK_STATUS=$(aws cloudformation describe-stacks --stack-name kaiassistant-api-stack --query 'Stacks[0].StackStatus' --output text 2>/dev/null || echo "DOES_NOT_EXIST")
          if [ "$STACK_STATUS" = "ROLLBACK_COMPLETE" ]; then
            echo "Stack in ROLLBACK_COMPLETE state, deleting..."
            aws cloudformation delete-stack --stack-name kaiassistant-api-stack
            aws cloudformation wait stack-delete-complete --stack-name kaiassistant-api-stack
            echo "Stack deleted successfully"
          fi

      - name: SAM Deploy
        env:
          CLOUDFORMATION_EXECUTION_ROLE_ARN: ${{ secrets.AWS_CLOUDFORMATION_EXECUTION_ROLE_ARN }}
          PAYPAL_CLIENT_ID: ${{ secrets.PAYPAL_CLIENT_ID }}
          PAYPAL_CLIENT_SECRET: ${{ secrets.PAYPAL_CLIENT_SECRET }}
          PAYPAL_ENVIRONMENT: ${{ secrets.PAYPAL_ENVIRONMENT }}
          PAYPAL_WEBHOOK_ID: ${{ secrets.PAYPAL_WEBHOOK_ID }}
          PAYPAL_MERCHANT_ID: ${{ secrets.PAYPAL_MERCHANT_ID }}
        run: |
          set -euo pipefail
          SMTP_SENDER_PASSWORD_CLEAN="$(printf '%s' "${{ secrets.SMTP_SENDER_PASSWORD }}" | tr -d '[:space:]')"
          PARAM_OVERRIDES=(
            "ParameterKey=MongoConnectionString,ParameterValue=${{ secrets.MONGODB_CONNECTIONSTRING }}"
            "ParameterKey=RedisConnectionString,ParameterValue=${{ secrets.REDIS__CONNECTIONSTRING }}"
            "ParameterKey=MongoDatabaseName,ParameterValue=${{ secrets.MONGODB_DATABASE }}"
            "ParameterKey=AllowedOrigins,ParameterValue=${{ secrets.ALLOWED_ORIGINS }}"
            "ParameterKey=PayPalClientId,ParameterValue=$PAYPAL_CLIENT_ID"
            "ParameterKey=PayPalClientSecret,ParameterValue=$PAYPAL_CLIENT_SECRET"
            "ParameterKey=PayPalEnvironment,ParameterValue=$PAYPAL_ENVIRONMENT"
            "ParameterKey=PayPalWebhookId,ParameterValue=$PAYPAL_WEBHOOK_ID"
            "ParameterKey=PayPalMerchantId,ParameterValue=$PAYPAL_MERCHANT_ID"
            "ParameterKey=SmtpSenderEmail,ParameterValue=${{ secrets.SMTP_SENDER_EMAIL }}"
            "ParameterKey=SmtpReceiverEmail,ParameterValue=${{ secrets.SMTP_RECEIVER_EMAIL }}"
            "ParameterKey=SmtpSenderPassword,ParameterValue=$SMTP_SENDER_PASSWORD_CLEAN"
            "ParameterKey=SmtpEnabled,ParameterValue=false"
            "ParameterKey=ASPNetCoreEnvironment,ParameterValue=Production"
            "ParameterKey=GeminiApiKey,ParameterValue=${{ secrets.GEMINI_API_KEY }}"
          )
          sam deploy \
            --template .aws-sam/build/template.yaml \
            --no-confirm-changeset \
            --no-fail-on-empty-changeset \
            --stack-name kaiassistant-api-stack \
            --capabilities CAPABILITY_IAM CAPABILITY_NAMED_IAM \
            --s3-bucket kaiassistant-lambda-deployments \
            --role-arn "$CLOUDFORMATION_EXECUTION_ROLE_ARN" \
            --parameter-overrides "${PARAM_OVERRIDES[@]}"

      - name: Get CloudFormation events on failure
        if: failure()
        run: |
          aws cloudformation describe-stack-events \
            --stack-name kaiassistant-api-stack \
            --max-items 20 \
            --query 'StackEvents[?ResourceStatus==`CREATE_FAILED`].[LogicalResourceId,ResourceStatusReason]' \
            --output table

      - name: Get API URL
        run: |
          API_URL=$(aws cloudformation describe-stacks \
            --stack-name kaiassistant-api-stack \
            --query 'Stacks[0].Outputs[?OutputKey==`ApiUrl`].OutputValue' \
            --output text)
          echo "API_URL=$API_URL" >> $GITHUB_ENV
          echo "🚀 API deployed to: $API_URL"
```

---

## Secrets Management (AWS Secrets Manager)

Create secrets manually or via script:

```bash
# MongoDB
aws secretsmanager create-secret \
  --name kaiassistant/mongodb \
  --secret-string '{"connectionString":"mongodb+srv://...","database":"KaiAssistant"}'

# Redis
aws secretsmanager create-secret \
  --name kaiassistant/redis \
  --secret-string '{"connectionString":"rediss://...:6380"}'

# Gemini
aws secretsmanager create-secret \
  --name kaiassistant/gemini \
  --secret-string '{"apiKey":"your-key"}'

# PayPal
aws secretsmanager create-secret \
  --name kaiassistant/paypal \
  --secret-string '{"clientId":"...","clientSecret":"...","webhookId":"wh_live_...","merchantId":"..."}'

# SMTP
aws secretsmanager create-secret \
  --name kaiassistant/smtp \
  --secret-string '{"senderEmail":"...","senderPassword":"...","receiverEmail":"..."}'
```

**Lambda IAM Policy** for secrets access:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["secretsmanager:GetSecretValue"],
      "Resource": [
        "arn:aws:secretsmanager:us-east-1:123456789012:secret:kaiassistant/*"
      ]
    }
  ]
}
```

---

## Frontend Deployment (Amplify + CloudFront)

### amplify.yml
```yaml
version: 1
frontend:
  phases:
    preBuild:
      commands:
        - npm ci --legacy-peer-deps
    build:
      commands:
        - npm run build
  artifacts:
    baseDirectory: .next
    files:
      - '**/*'
  cache:
    paths:
      - node_modules/**/*
      - .next/cache/**/*
```

### Environment Variables (Amplify Console)
| Variable | Value |
|----------|-------|
| `NEXT_PUBLIC_SITE_URL` | `https://yourdomain.com` |
| `BACKEND_API_URL` | `https://api.yourdomain.com` (Lambda URL) |
| `GITHUB_USER` | `your-username` |
| `GITHUB_TOKEN` | `ghp_xxx` |

---

## Custom Domain Setup

### API Gateway Custom Domain
1. Request ACM certificate for `api.yourdomain.com`
2. Create custom domain mapping in API Gateway
3. Create Route53 alias record → API Gateway domain

### Frontend Custom Domain
1. Request ACM certificate for `yourdomain.com`, `www.yourdomain.com`
2. Configure in Amplify Console or CloudFront + S3
3. Create Route53 records

---

## Monitoring (Lambda)

| Metric | Source | Alert |
|--------|--------|-------|
| Invocations | CloudWatch | — |
| Errors | CloudWatch | > 1% |
| Duration | CloudWatch | p99 > 10s |
| Throttles | CloudWatch | > 0 |
| Dead Letter Queue | SQS | > 0 messages |

**X-Ray Tracing**: Enabled via `AWS_XRAY_DAEMON_ADDRESS` + `TracingConfig: Active`

---

## Cost Optimization

| Setting | Value | Rationale |
|---------|-------|-----------|
| Architecture | arm64 | 20% cheaper, better performance |
| Memory | 512 MB | Balanced for .NET 10 |
| Timeout | 30s | Covers streaming responses |
| Provisioned Concurrency | 2-5 | Cold start mitigation |

---

## Rollback

```bash
# Previous deployment
sam deploy --template .aws-sam/build/template.yaml --stack-name kaiassistant-api-stack --no-confirm-changeset

# Or via CloudFormation console: Stack → Stack actions → Rollback
```

---

## Related Documents

- [Production Runbook](production-runbook.md) — Docker Compose deployment
- [Production Environment Variables](production-env-vars.md) — Variable reference
- [Data Protection](data-protection.md) — Backup strategy
- [Architecture Overview](../architecture/system-overview.md) — System diagram