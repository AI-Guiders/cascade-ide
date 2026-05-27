using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Сообщение в чат при <see cref="AgentVerifyEpochStale"/> (ADR 0148 W3).</summary>
public sealed class AgentEnvironmentEpochStaleChatProjection : IDisposable
{
    private readonly IDisposable _subscription;

    public AgentEnvironmentEpochStaleChatProjection(
        IDataBus dataBus,
        AgentEnvironmentTimeAccountingSettings settings,
        Action<string, bool> appendTrace)
    {
        if (!settings.ShowInChat)
        {
            _subscription = new NoopDisposable();
            return;
        }

        _subscription = dataBus.Subscribe<AgentVerifyEpochStale>(evt =>
        {
            var glyph = evt.Reason switch
            {
                "write_in_epoch" => "⚠",
                "cancel" => "⊘",
                "superseded" => "↻",
                _ => "⚠",
            };
            var line = $"{glyph} [AEE] verify epoch устарел ({evt.Reason}) · snapshot {evt.VerifySnapshotId[..8]}…";
            appendTrace(line, false);
        });
    }

    public void Dispose() => _subscription.Dispose();

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
