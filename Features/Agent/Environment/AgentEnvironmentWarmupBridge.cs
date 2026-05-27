using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>AEE ↔ solution warm-up (ADR 0148 W5): отмена verify при смене scope.</summary>
public sealed class AgentEnvironmentWarmupBridge : IDisposable
{
    private readonly IAgentEnvironmentService _environment;
    private readonly IDisposable _subscription;

    public AgentEnvironmentWarmupBridge(IDataBus dataBus, IAgentEnvironmentService environment)
    {
        _environment = environment;
        _subscription = dataBus.Subscribe<SolutionWarmupStateChanged>(OnWarmupChanged);
    }

    private void OnWarmupChanged(SolutionWarmupStateChanged evt)
    {
        if (evt.Lifecycle == SolutionWarmupLifecycle.Cancelled)
        {
            _environment.CancelActive();
            return;
        }

        var status = _environment.GetStatus();
        if (!status.IsActive || string.IsNullOrWhiteSpace(evt.SolutionPath))
            return;

        if (status.SolutionPath is not null
            && !string.Equals(status.SolutionPath, evt.SolutionPath, StringComparison.OrdinalIgnoreCase))
            _environment.CancelActive();
    }

    public void Dispose() => _subscription.Dispose();
}
