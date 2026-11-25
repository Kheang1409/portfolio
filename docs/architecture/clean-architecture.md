# Clean Architecture

## Layer Structure

```
KaiAssistant.sln
├── KaiAssistant.Domain          # Enterprise Business Rules
├── KaiAssistant.Application     # Application Business Rules
├── KaiAssistant.Infrastructure  # Interface Adapters (Frameworks, DB, External)
└── KaiAssistant.API             # Interface Adapters (Web, Controllers)
```

---

## Dependency Rule

```
┌─────────────────────────────────────┐
│         KaiAssistant.Domain         │  ◀── NO dependencies
│   (Entities, Value Objects,         │       on outer layers
│    Domain Events, Specifications)   │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│     KaiAssistant.Application        │  ◀── Depends ONLY on Domain
│  (Use Cases, DTOs, Interfaces,      │
│   Validators, MediatR Handlers)     │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│   KaiAssistant.Infrastructure       │  ◀── Implements Application interfaces
│  (MongoDB, Redis, Gemini, PayPal,   │       Depends on Application + Domain
│   SMTP, OpenTelemetry)              │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│         KaiAssistant.API            │  ◀── Composition root
│  (Controllers, Middleware,          │       References all layers
│   DI Registration, OpenAPI)         │
└─────────────────────────────────────┘
```

---

## Domain Layer (`KaiAssistant.Domain`)

**Purpose**: Pure business logic, no external dependencies.

### Contents
```
Domain/
├── Entities/
│   ├── Resume.cs
│   ├── Donation.cs
│   ├── DonationWebhookEvent.cs
│   ├── DonationReceipt.cs
│   ├── Visitor.cs
│   └── Conversation.cs
├── ValueObjects/
│   ├── Email.cs
│   ├── Money.cs
│   ├── Url.cs
│   └── SkillLevel.cs
├── Events/
│   ├── DonationCompletedEvent.cs
│   ├── ResumeUpdatedEvent.cs
│   └── VisitorRecordedEvent.cs
├── Specifications/
│   ├── ResumeSpecifications.cs
│   └── DonationSpecifications.cs
├── Exceptions/
│   ├── DomainException.cs
│   └── ConcurrencyException.cs
└── Common/
    ├── Entity.cs
    ├── ValueObject.cs
    ├── Result.cs
    └── IDomainEventDispatcher.cs
```

### Key Principles
- **Entities** have identity (`Id`), mutable state, lifecycle
- **Value Objects** immutable, equality by value
- **Domain Events** for side-effects (published via `IDomainEventDispatcher`)
- **Specifications** encapsulate business rules for queries
- **Result<T>** for explicit success/failure (no exceptions for control flow)

---

## Application Layer (`KaiAssistant.Application`)

**Purpose**: Orchestrate use cases, define contracts (interfaces).

### Contents
```
Application/
├── Contracts/
│   ├── DTOs/
│   │   ├── ResumeDto.cs
│   │   ├── DonationDto.cs
│   │   ├── AssistantRequestDto.cs
│   │   └── AssistantResponseChunkDto.cs
│   └── Responses/
│       ├── ApiResponse.cs
│       └── PagedResponse.cs
├── Interfaces/
│   ├── Repositories/
│   │   ├── IResumeRepository.cs
│   │   ├── IDonationRepository.cs
│   │   └── IVisitorRepository.cs
│   ├── Services/
│   │   ├── IAssistantService.cs
│   │   ├── IEmbeddingService.cs
│   │   ├── IDonationService.cs
│   │   ├── IEmailService.cs
│   │   └── IRateLimitService.cs
│   └── Infrastructure/
│       ├── IKnowledgeIndexer.cs
│       └── ICacheService.cs
├── Features/
│   ├── Resumes/
│   │   ├── Queries/
│   │   │   ├── GetLatestResumeQuery.cs
│   │   │   └── GetLatestResumeQueryHandler.cs
│   │   └── Commands/
│   │       └── UpsertResumeCommand.cs
│   ├── Assistant/
│   │   ├── Queries/
│   │   │   └── StreamAssistantResponseQuery.cs
│   │   └── Handlers/
│   │       └── StreamAssistantResponseHandler.cs
│   ├── Donations/
│   │   ├── Commands/
│   │   │   ├── CreateDonationOrderCommand.cs
│   │   │   ├── CaptureDonationCommand.cs
│   │   │   └── ProcessWebhookCommand.cs
│   │   └── Queries/
│   │       ├── GetDonationConfigQuery.cs
│   │       └── GetLeaderboardQuery.cs
│   └── Visitors/
│       ├── Commands/
│       │   └── RecordVisitorCommand.cs
│       └── Queries/
│           └── GetVisitorStatsQuery.cs
├── Validators/
│   ├── CreateDonationOrderValidator.cs
│   └── AssistantRequestValidator.cs
├── Behaviors/
│   ├── ValidationBehavior.cs
│   ├── LoggingBehavior.cs
│   └── UnitOfWorkBehavior.cs
└── DependencyInjection.cs
```

### Key Principles
- **MediatR** for Commands/Queries (CQRS-lite)
- **Interfaces** define contracts — Infrastructure implements
- **DTOs** for API boundaries — never expose Domain entities directly
- **Validators** (FluentValidation) via pipeline behavior
- **Behaviors** for cross-cutting concerns (logging, validation, transactions)

---

## Infrastructure Layer (`KaiAssistant.Infrastructure`)

**Purpose**: Implement Application interfaces using concrete technologies.

### Contents
```
Infrastructure/
├── Persistence/
│   ├── MongoDB/
│   │   ├── Repositories/
│   │   │   ├── ResumeRepository.cs
│   │   │   ├── DonationRepository.cs
│   │   │   └── VisitorRepository.cs
│   │   ├── Collections/
│   │   │   ├── ResumeCollection.cs
│   │   │   └── DonationCollection.cs
│   │   ├── Configuration/
│   │   │   └── MongoDbSettings.cs
│   │   └── DependencyInjection.cs
│   └── Redis/
│       ├── Services/
│       │   ├── RateLimitService.cs
│       │   └── CacheService.cs
│       └── DependencyInjection.cs
├── Services/
│   ├── AI/
│   │   ├── GeminiClient.cs
│   │   ├── EmbeddingService.cs
│   │   └── AssistantService.cs
│   ├── PayPal/
│   │   ├── PayPalClient.cs
│   │   ├── DonationService.cs
│   │   └── WebhookVerifier.cs
│   ├── Email/
│   │   ├── SmtpEmailService.cs
│   │   └── OutboxProcessor.cs
│   └── Knowledge/
│       └── KnowledgeBootstrapHostedService.cs
├── Configuration/
│   ├── AiSettings.cs
│   ├── PayPalSettings.cs
│   ├── SmtpSettings.cs
│   └── RedisSettings.cs
├── Observability/
│   ├── OpenTelemetryConfiguration.cs
│   └── SerilogConfiguration.cs
└── DependencyInjection.cs
```

### Key Principles
- **One implementation per interface** (typically)
- **Settings classes** map to `IOptions<T>` sections
- **Hosted Services** for background work (knowledge bootstrap, outbox processor)
- **HttpClient** via `IHttpClientFactory` with resilience policies
- **No Domain/Application references** in Infrastructure (only implements interfaces)

---

## API Layer (`KaiAssistant.API`)

**Purpose**: HTTP endpoints, middleware, composition root.

### Contents
```
API/
├── Controllers/
│   ├── AssistantController.cs
│   ├── ResumesController.cs
│   ├── DonationsController.cs
│   ├── ContactsController.cs
│   ├── VisitsController.cs
│   └── HealthController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   ├── CorrelationIdMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Filters/
│   ├── ApiExceptionFilter.cs
│   └── ValidationFilter.cs
├── Configuration/
│   ├── CorsConfiguration.cs
│   ├── SwaggerConfiguration.cs
│   └── HealthCheckConfiguration.cs
├── Extensions/
│   ├── ServiceCollectionExtensions.cs
│   └── WebApplicationExtensions.cs
├── Program.cs
└── appsettings.json
```

### Key Principles
- **Thin controllers** — delegate to MediatR
- **Middleware** for cross-cutting HTTP concerns
- **Program.cs** = composition root (DI registration order matters)
- **Health Checks** split: `/health/live` (process), `/health/ready` (dependencies)

---

## Dependency Injection Registration Order

```csharp
// Program.cs / ServiceCollectionExtensions.cs

// 1. Domain (no registration needed - pure)

// 2. Application
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.DependencyInjection).Assembly));
services.AddValidatorsFromAssembly(typeof(Application.DependencyInjection).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// 3. Infrastructure
services.AddMongoDb(configuration);           // Repositories
services.AddRedis(configuration);             // RateLimit, Cache
services.AddGemini(configuration);            // AI services
services.AddPayPal(configuration);            // Donation services
services.AddEmail(configuration);             // SMTP, Outbox
services.AddKnowledgeBootstrap();             // Hosted service
services.AddOpenTelemetry(configuration);     // Observability

// 4. API
services.AddControllers()
    .AddJsonOptions(...);
services.AddCors(...);
services.AddHealthChecks(...);
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(...);
```

---

## Testing Strategy by Layer

| Layer | Test Type | Tools |
|-------|-----------|-------|
| Domain | Unit (pure logic) | xUnit, FluentAssertions |
| Application | Unit (handlers, validators) | xUnit, Moq, FluentAssertions |
| Infrastructure | Integration (real DB, Redis) | xUnit, Testcontainers, MongoDB.Driver |
| API | Integration (WebApplicationFactory) | xUnit, Microsoft.AspNetCore.Mvc.Testing |

---

## Adding a New Feature

1. **Domain**: Create Entity/ValueObject, Domain Events, Specifications
2. **Application**: Define DTOs, Interfaces (Repository/Service), Commands/Queries, Handlers, Validators
3. **Infrastructure**: Implement Interfaces (MongoDB, Redis, External APIs)
4. **API**: Add Controller, register in DI, add health checks if needed
5. **Tests**: Unit tests for Domain/Application, Integration for Infrastructure/API

---

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Domain referencing Infrastructure | Keep Domain pure; use Domain Events for side-effects |
| Controller calling Repository directly | Controller → MediatR → Handler → Repository (via Interface) |
| Leaking Entity to API | Map to DTO in Handler or Controller |
| Circular dependency | Check project references; Infrastructure implements Application interfaces |
| Configuration in Domain | Use Options Pattern in Infrastructure, pass values to Domain via constructors |