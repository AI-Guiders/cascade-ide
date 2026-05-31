using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Inject verify trace into chat on AgentRunCompleted (ADR 0148 W3).</summary>
public sealed class AgentEnvironmentChatProjection : IDisposable
{
    private readonly IDisposable _subscription;
    private readonly AgentEnvironmentTimeAccountingSettings _settings;
    private readonly Action<string, bool> _appendTrace;

    public AgentEnvironmentChatProjection(
        IDataBus dataBus,
        AgentEnvironmentTimeAccountingSettings settings,
        Action<string, bool> appendTrace)
    {
        _settings = settings;
        _appendTrace = appendTrace;
        _subscription = dataBus.Subscribe<AgentRunCompleted>(OnCompleted);
    }

    private void OnCompleted(AgentRunCompleted evt)
    {
        if (!_settings.ShowInChat)
            return;

        _appendTrace(AgentVerifyEpochFormatter.FormatCompletedChatTrace(evt), evt.Green);
    }

    public void Dispose() => _subscription.Dispose();
}
