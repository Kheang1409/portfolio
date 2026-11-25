using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.Infrastructure.Observability;

public sealed class InstanceIdentity : IInstanceIdentity
{
    public string InstanceId { get; }
    public InstanceIdentity()
    {
        var configured = Environment.GetEnvironmentVariable("INSTANCE_ID");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            InstanceId = configured.Trim();
            return;
        }
        var host = Environment.MachineName;
        InstanceId = $"{host}:{Guid.NewGuid():N}";
    }
}