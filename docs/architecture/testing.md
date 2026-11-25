# Backend Testing Strategy

## Test Pyramid

```
        ┌─────────────┐
        │   E2E /     │  ← Few, slow, high confidence
        │ Integration │
        └──────┬──────┘
               │
       ┌───────┴───────┐
       │  Integration  │  ← Some, medium speed
       │   (API, DB)   │
       └───────┬───────┘
               │
    ┌──────────┴──────────┐
    │      Unit           │  ← Many, fast, isolated
    │  (Domain, Handlers) │
    └─────────────────────┘
```

---

## Project Structure

```
KaiAssistant.Tests/
├── Unit/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── ResumeTests.cs
│   │   │   ├── DonationTests.cs
│   │   │   └── VisitorTests.cs
│   │   ├── ValueObjects/
│   │   │   ├── EmailTests.cs
│   │   │   └── MoneyTests.cs
│   │   └── Specifications/
│   │       └── ResumeSpecificationTests.cs
│   └── Application/
│       ├── Features/
│       │   ├── Resumes/
│       │   │   └── GetLatestResumeQueryHandlerTests.cs
│       │   ├── Assistant/
│       │   │   └── StreamAssistantResponseHandlerTests.cs
│       │   └── Donations/
│       │       ├── CreateDonationOrderHandlerTests.cs
│       │       └── CaptureDonationHandlerTests.cs
│       └── Validators/
│           └── CreateDonationOrderValidatorTests.cs
├── Integration/
│   ├── Api/
│   │   ├── AssistantControllerTests.cs
│   │   ├── ResumesControllerTests.cs
│   │   └── DonationsControllerTests.cs
│   ├── Persistence/
│   │   ├── ResumeRepositoryTests.cs
│   │   └── DonationRepositoryTests.cs
│   └── Services/
│       ├── GeminiClientTests.cs
│       └── PayPalClientTests.cs
└── TestBase/
    ├── TestFixture.cs
    ├── MongoDbFixture.cs
    ├── RedisFixture.cs
    └── WebApplicationFactoryFixture.cs
```

---

## Unit Tests (xUnit + Moq + FluentAssertions)

### Domain Entity Test

```csharp
public class DonationTests
{
    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var orderId = "order_123";
        var amount = new Money(10.00m, "USD");
        var donorEmail = "donor@example.com";

        // Act
        var result = Donation.Create(orderId, amount, donorEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.Status.Should().Be(DonationStatus.Pending);
        result.Value.Amount.Should().Be(amount);
    }

    [Fact]
    public void Capture_ValidApproval_UpdatesStatus()
    {
        // Arrange
        var donation = Donation.Create("order_123", new Money(10m, "USD"), "test@test.com").Value;
        var captureId = "capture_456";

        // Act
        var result = donation.Capture(captureId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        donation.Status.Should().Be(DonationStatus.Completed);
        donation.CaptureId.Should().Be(captureId);
        donation.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
```

### Application Handler Test

```csharp
public class GetLatestResumeQueryHandlerTests
{
    private readonly Mock<IResumeRepository> _repoMock;
    private readonly GetLatestResumeQueryHandler _handler;

    public GetLatestResumeQueryHandlerTests()
    {
        _repoMock = new Mock<IResumeRepository>();
        _handler = new GetLatestResumeQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ResumeExists_ReturnsResumeDto()
    {
        // Arrange
        var resume = ResumeBuilder.CreateDefault().Build();
        _repoMock.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        // Act
        var result = await _handler.Handle(new GetLatestResumeQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Profile.FullName.Should().Be(resume.Profile.FullName);
    }

    [Fact]
    public async Task Handle_NoResume_ReturnsNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume?)null);

        // Act
        var result = await _handler.Handle(new GetLatestResumeQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Resume.NotFound");
    }
}
```

### Validator Test

```csharp
public class CreateDonationOrderValidatorTests
{
    private readonly CreateDonationOrderValidator _validator;

    public CreateDonationOrderValidatorTests()
    {
        _validator = new CreateDonationOrderValidator();
    }

    [Theory]
    [InlineData(0, "Amount must be greater than zero")]
    [InlineData(-5, "Amount must be greater than zero")]
    public async Task Validate_InvalidAmount_ReturnsError(decimal amount, string expectedError)
    {
        var command = new CreateDonationOrderCommand { Amount = amount, Currency = "USD" };
        var result = await _validator.ValidateAsync(command);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedError);
    }

    [Fact]
    public async Task Validate_ValidInput_Passes()
    {
        var command = new CreateDonationOrderCommand { Amount = 10m, Currency = "USD", DonorEmail = "test@test.com" };
        var result = await _validator.ValidateAsync(command);
        
        result.IsValid.Should().BeTrue();
    }
}
```

---

## Integration Tests (WebApplicationFactory + Testcontainers)

### Base Fixture

```csharp
public class WebApplicationFactoryFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    public async Task InitializeAsync()
    {
        // Start Testcontainers for MongoDB + Redis
        var mongoContainer = new MongoDbBuilder().Build();
        await mongoContainer.StartAsync();
        
        var redisContainer = new RedisBuilder().Build();
        await redisContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace MongoDB connection
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoClient));
                    if (descriptor != null) services.Remove(descriptor);
                    
                    services.AddSingleton<IMongoClient>(_ => 
                        new MongoClient(mongoContainer.GetConnectionString()));
                    
                    // Replace Redis connection
                    var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                    if (redisDescriptor != null) services.Remove(redisDescriptor);
                    
                    services.AddSingleton<IConnectionMultiplexer>(_ => 
                        ConnectionMultiplexer.Connect(redisContainer.GetConnectionString()));
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }
}
```

### API Controller Test

```csharp
public class AssistantControllerTests : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly HttpClient _client;

    public AssistantControllerTests(WebApplicationFactoryFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task StreamAssistant_ReturnsStreamedResponse()
    {
        // Arrange
        var request = new { query = "What is your experience?", history = new object[] { } };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/assistant", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/x-ndjson");
        
        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var firstLine = await reader.ReadLineAsync();
        firstLine.Should().Contain("content");
    }

    [Fact]
    public async Task StreamAssistant_EmptyQuery_ReturnsBadRequest()
    {
        var request = new { query = "", history = new object[] { } };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/assistant", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

### Repository Test (Real MongoDB)

```csharp
public class ResumeRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly IMongoDatabase _database;
    private readonly ResumeRepository _repository;

    public ResumeRepositoryTests(MongoDbFixture fixture)
    {
        _database = fixture.Database;
        _repository = new ResumeRepository(_database, Mock.Of<ILogger<ResumeRepository>>());
    }

    [Fact]
    public async Task UpsertAndGetLatest_RoundTrip()
    {
        // Arrange
        var resume = ResumeBuilder.CreateDefault().Build();

        // Act
        await _repository.UpsertAsync(resume, CancellationToken.None);
        var result = await _repository.GetLatestAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Profile.FullName.Should().Be(resume.Profile.FullName);
        result.Experience.Should().HaveCount(resume.Experience.Count);
    }
}
```

---

## RAG Evaluation (KaiAssistant.RagEvaluation)

### Evaluation Harness

```csharp
public class RagEvaluator
{
    public async Task<EvaluationReport> EvaluateAsync(
        IEnumerable<TestCase> testCases,
        IAssistantService assistantService)
    {
        var results = new List<TestResult>();

        foreach (var testCase in testCases)
        {
            var response = await assistantService.GetFullResponseAsync(testCase.Query);
            var score = EvaluateResponse(response, testCase.ExpectedFacts);
            
            results.Add(new TestResult
            {
                Query = testCase.Query,
                ExpectedFacts = testCase.ExpectedFacts,
                ActualResponse = response,
                Score = score,
                Passed = score >= testCase.Threshold
            });
        }

        return new EvaluationReport
        {
            TotalCases = results.Count,
            Passed = results.Count(r => r.Passed),
            AverageScore = results.Average(r => r.Score),
            Results = results
        };
    }
}
```

### Test Cases (JSON)

```json
// Data/rag-evaluation-cases.json
[
  {
    "query": "What is your current role?",
    "expectedFacts": ["Senior Software Engineer", "TechCorp"],
    "threshold": 0.8
  },
  {
    "query": "What cloud platforms do you know?",
    "expectedFacts": ["Azure", "AWS"],
    "threshold": 0.7
  },
  {
    "query": "Tell me about the e-commerce project",
    "expectedFacts": ["E-Commerce Platform", "Microservices", ".NET 8"],
    "threshold": 0.75
  }
]
```

### Run Evaluation

```powershell
cd backend
dotnet run --project KaiAssistant.RagEvaluation
```

---

## Test Commands

```powershell
# All tests
dotnet test KaiAssistant.sln

# Unit only
dotnet test KaiAssistant.Tests --filter "Category=Unit"

# Integration only
dotnet test KaiAssistant.Tests --filter "Category=Integration"

# With coverage
dotnet test KaiAssistant.Tests --collect:"XPlat Code Coverage"

# Specific project
dotnet test KaiAssistant.Tests/KaiAssistant.Tests.csproj
dotnet test KaiAssistant.RagEvaluation/KaiAssistant.RagEvaluation.csproj

# Verbose
dotnet test KaiAssistant.Tests -v n
```

---

## CI Integration

```yaml
# .github/workflows/ci-security.yml (excerpt)
- name: Run backend tests
  run: dotnet test KaiAssistant.sln --logger "trx;LogFileName=test-results.trx"

- name: Upload test results
  uses: actions/upload-artifact@v4
  if: always()
  with:
    name: test-results
    path: '**/*.trx'
```

---

## Best Practices

| Practice | Implementation |
|----------|----------------|
| **Test Naming** | `Method_Scenario_ExpectedResult` |
| **Arrange-Act-Assert** | Clear separation in every test |
| **One Assert per Test** | Or related assertions grouped |
| **No Test Interdependence** | Each test runs in isolation |
| **Deterministic** | No random, no time-dependent (use `TimeProvider`) |
| **Fast Unit Tests** | < 100ms each, no I/O |
| **Real Dependencies in Integration** | Testcontainers for MongoDB/Redis |
| **Mock External Services** | HttpClient mock for Gemini, PayPal |

---

## Related Documents

- [Clean Architecture](clean-architecture.md) — Layer testing strategy
- [Resilience Patterns](resilience.md) — Testing failure scenarios
- [CI Security Workflow](../.github/workflows/ci-security.yml) — Test execution in CI