using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

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

        var last = FormatTrace(evt);
        _appendTrace(last, evt.Green);
    }

    internal static string FormatTrace(AgentRunCompleted evt)
    {
        var env = evt.TimeSlices.FirstOrDefault(s => s.Phase == AgentRunPhaseKind.Environment);
        var envText = env is null ? "—" : $"{env.DurationSeconds:0.0}s ({env.Detail ?? "environment"})";
        return $"""
            [AEE] verify {evt.RunId[..8]}…
              Environment: {envText}
              Status: {(evt.Green ? "green" : "failed")} ({evt.MaxRungReached})
            """;
    }

    public void Dispose() => _subscription.Dispose();
}
