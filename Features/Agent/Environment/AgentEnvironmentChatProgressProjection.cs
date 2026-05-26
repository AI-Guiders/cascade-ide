using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Краткий прогресс environment tasks в чат (ADR 0148 W3).</summary>
public sealed class AgentEnvironmentChatProgressProjection : IDisposable
{
    private readonly IDisposable _subscription;

    public AgentEnvironmentChatProgressProjection(
        IDataBus dataBus,
        AgentEnvironmentTimeAccountingSettings settings,
        Action<string, bool> appendTrace)
    {
        if (!settings.ShowTaskProgressInChat)
        {
            _subscription = new NoopDisposable();
            return;
        }

        _subscription = dataBus.Subscribe<AgentEnvironmentTaskChanged>(evt =>
        {
            if (evt.State is not (AgentEnvironmentTaskState.Running or AgentEnvironmentTaskState.Queued))
                return;

            var line = $"[AEE] {evt.Kind} · {evt.State} · run {evt.RunId[..8]}…";
            if (!string.IsNullOrWhiteSpace(evt.ProgressMessage))
                line += $" ({evt.ProgressMessage})";
            appendTrace(line, true);
        });
    }

    public void Dispose() => _subscription.Dispose();

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
