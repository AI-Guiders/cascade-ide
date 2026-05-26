using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>AEE ↔ solution warm-up (ADR 0148 W5): отмена verify при смене scope.</summary>
public sealed class AgentEnvironmentWarmupBridge : IDisposable
{
    private readonly IDisposable _subscription;

    public AgentEnvironmentWarmupBridge(IDataBus dataBus, IAgentEnvironmentService environment)
    {
        _subscription = dataBus.Subscribe<SolutionWarmupStateChanged>(evt =>
        {
            if (evt.Lifecycle == SolutionWarmupLifecycle.Cancelled)
                environment.CancelActive();
        });
    }

    public void Dispose() => _subscription.Dispose();
}
