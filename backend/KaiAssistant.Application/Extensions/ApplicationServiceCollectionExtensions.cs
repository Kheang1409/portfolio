using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Contacts.Commands;
using KaiAssistant.Application.Resumes.Commands;
using KaiAssistant.Application.Resumes.Queries;
using KaiAssistant.Application.Services;
using MediatR;
using KaiAssistant.Application.Behaviors;
namespace KaiAssistant.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ContactCommand>();
            cfg.RegisterServicesFromAssemblyContaining<CreateResumeCommand>();
            cfg.RegisterServicesFromAssemblyContaining<GetLatestResumeQuery>();
            cfg.RegisterServicesFromAssemblyContaining<GetResumeByIdQuery>();
        });
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // AI services
        services.AddSingleton<IAiPromptBuilder, AiPromptBuilder>();
        services.AddScoped<IResumeContextProvider, ResumeContextProvider>();
        services.AddScoped<IAiModelGateway, GeminiAiModelGatewayAdapter>();
        return services;
    }
}