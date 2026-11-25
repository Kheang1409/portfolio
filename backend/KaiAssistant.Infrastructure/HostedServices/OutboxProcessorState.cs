using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class OutboxProcessorState : IOutboxProcessorState
{
    private readonly object _sync = new();
    private readonly OutboxProcessorSnapshot _snapshot = new();
    public OutboxProcessorState(IInstanceIdentity instanceIdentity)
    {
        _snapshot.InstanceId = instanceIdentity.InstanceId;
    }
    public OutboxProcessorSnapshot Snapshot
    {
        get
        {
            lock (_sync)
            {
                return new OutboxProcessorSnapshot
                {
                    InstanceId = _snapshot.InstanceId,
                    IsRunning = _snapshot.IsRunning,
                    LastCycleStartedAtUtc = _snapshot.LastCycleStartedAtUtc,
                    LastSuccessAtUtc = _snapshot.LastSuccessAtUtc,
                    LastError = _snapshot.LastError
                };
            }
        }
    }
    public void MarkCycleStarted()
    {
        lock (_sync)
        {
            _snapshot.IsRunning = true;
            _snapshot.LastCycleStartedAtUtc = DateTimeOffset.UtcNow;
        }
    }
    public void MarkCycleSucceeded()
    {
        lock (_sync)
        {
            _snapshot.IsRunning = true;
            _snapshot.LastSuccessAtUtc = DateTimeOffset.UtcNow;
            _snapshot.LastError = null;
        }
    }
    public void MarkCycleFailed(string error)
    {
        lock (_sync)
        {
            _snapshot.IsRunning = true;
            _snapshot.LastError = error;
        }
    }
}