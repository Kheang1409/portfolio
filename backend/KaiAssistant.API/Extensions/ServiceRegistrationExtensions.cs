using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Services;
using KaiAssistant.Infrastructure.Persistence;
using KaiAssistant.Infrastructure.Services;
namespace KaiAssistant.API.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAiOrchestration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Infrastructure Services
        services.AddSingleton<IVisitDeduplicationService, VisitDeduplicationService>();
        services.AddSingleton<IRateLimitingService, RateLimitingService>();
        services.AddSingleton<ITokenBudgetService, TokenBudgetService>();
        // Application Services
        services.AddScoped<IAssistantContextBuilder, AssistantContextBuilder>();
        services.AddScoped<AssistantOrchestrator>();
        services.AddScoped<IAssistantOrchestrator>(sp => sp.GetRequiredService<AssistantOrchestrator>());
        // Persistence
        services.AddScoped<IConversationRepository, ConversationRepository>();
        return services;
    }
}