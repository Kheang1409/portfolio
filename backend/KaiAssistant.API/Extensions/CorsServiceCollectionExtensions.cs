namespace KaiAssistant.API.Extensions;

public static class CorsServiceCollectionExtensions
{
    private const string DefaultPolicyName = "AllowNetlifyApp";
    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        string[]? allowedOrigins = null;
        var env = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        if (!string.IsNullOrWhiteSpace(env))
        {
            allowedOrigins = env.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .ToArray();
        }
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        }
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            throw new ArgumentException("Cors 'AllowedOrigins' is missing or empty.");
        }
        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
            });
        });
        return services;
    }
}