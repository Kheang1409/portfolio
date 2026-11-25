using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using KaiAssistant.Infrastructure.Cache;
using KaiAssistant.Infrastructure.Extensions;
using KaiAssistant.Infrastructure.HostedServices;
using KaiAssistant.Infrastructure.Mongo;
using KaiAssistant.Infrastructure.Observability;
using KaiAssistant.Infrastructure.EventBus;
using KaiAssistant.Infrastructure.Services;
using KaiAssistant.Infrastructure.AI;
using KaiAssistant.Domain.Interfaces.Repositories;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Application.Services;
using KaiAssistant.Application.Donations;
using KaiAssistant.Infrastructure.Governance;
using KaiAssistant.Infrastructure.AI.Conversation;
using KaiAssistant.Infrastructure.AI.Providers;
using KaiAssistant.Infrastructure.AI.Embeddings;
using KaiAssistant.Infrastructure.AI.Rag;
using KaiAssistant.Infrastructure.FeatureFlags;
using KaiAssistant.Infrastructure.Payments;
using KaiAssistant.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace KaiAssistant.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.Configure<AiGovernanceOptions>(configuration.GetSection(AiGovernanceOptions.SectionName));
        services.Configure<FeatureFlagsOptions>(configuration.GetSection(FeatureFlagsOptions.SectionName));
        services.Configure<AiModelOrchestrationOptions>(configuration.GetSection(AiModelOrchestrationOptions.SectionName));
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<KnowledgeOptions>(configuration.GetSection(KnowledgeOptions.SectionName));
        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        services.Configure<ConversationOptions>(configuration.GetSection(ConversationOptions.SectionName));
        services.Configure<PayPalOptions>(configuration.GetSection(PayPalOptions.SectionName));
        services
            .AddGeminiAiServices(configuration)
            .AddEmailServices(configuration)
            .AddMongo(configuration);
        services.AddSingleton<IFeatureFlagService, DynamicFeatureFlagService>();
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.AddSingleton<RedisExecutionHelper>();
        services.AddSingleton<IResilienceStatusProvider, ResilienceStatusProvider>();
        services.AddSingleton<IInstanceIdentity, InstanceIdentity>();
        services.TryAddSingleton<IClientContextAccessor, DefaultClientContextAccessor>();
        services.AddSingleton<RedisCacheService>();
        services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<RedisCacheService>());
        services.AddSingleton<ICacheDiagnosticsService>(sp => sp.GetRequiredService<RedisCacheService>());
        services.AddSingleton<IAiUsageGuard, AiUsageGuard>();
        services.AddHttpClient("PayPal", client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IDonationService, PayPalDonationService>();
        services.AddSingleton<IPromptSecurityService, PromptSecurityService>();
        services.AddSingleton<IObservabilityService, ObservabilityService>();
        services.AddHttpClient("GeminiEmbeddings", client => client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/"));
        services.AddSingleton<IEmbeddingService, GeminiEmbeddingService>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<IVectorStore, MongoVectorStore>();
        services.AddScoped<IKeywordSearch, MongoKeywordSearch>();
        services.AddScoped<IHybridRetriever, HybridRetriever>();
        services.AddSingleton<IRagContextSelector, RagContextSelector>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();
        services.AddScoped<IAiProvider, GeminiProvider>();
        services.AddScoped<IAssistantService, AssistantService>();
        // AI orchestration & evaluation services (used by ops endpoints and model routing)
        services.AddScoped<IAiOrchestratorService, AiOrchestratorService>();
        services.AddScoped<IAiEvaluationService, AiEvaluationService>();
        // Model autotuning & traffic simulation (optional, feature-flagged)
        services.AddSingleton<IAiTuningState, AiTuningState>();
        services.AddSingleton<IAiTrafficSimulationService, AiTrafficSimulationService>();
        services.AddSingleton<IAiDecisionAuditStore, AiDecisionAuditStore>();
        // GeminiGateway uses this lightweight in-process circuit state; it is not model autotuning.
        services.AddSingleton<IModelHealthService, ModelHealthService>();
        services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
        // Visit tracking (singleton to support ObservabilityService singleton dependency)
        services.AddSingleton<IVisitTrackingService, RedisVisitTrackingService>();
        services.AddScoped<IResumeWriteService, ResumeWriteService>();
        services.AddSingleton<IResumeRepository, Repositories.ResumeRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddHostedService<MongoIndexInitializerHostedService>();
        services.AddHostedService<DonationReceiptOutboxWorker>();
        services.AddHostedService<KnowledgeBootstrapHostedService>();
        // AI autotuning and traffic simulation hosted services (controlled by feature flags)
        services.AddHostedService<AiAutoTuneHostedService>();
        services.AddHostedService<AiTrafficSimulationHostedService>();
        return services;
    }
}
