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
        var rungLines = FormatRungLines(evt);
        var status = evt.Green ? "green" : "failed";
        return $"""
            [AEE] verify {evt.RunId[..8]}…
            {rungLines}
              Status: {status} · max rung: {evt.MaxRungReached}
            """;
    }

    private static string FormatRungLines(AgentRunCompleted evt)
    {
        var envSlices = evt.TimeSlices
            .Where(s => s.Phase == AgentRunPhaseKind.Environment)
            .ToList();

        if (envSlices.Count == 0)
            return "  — (no environment slices)";

        var lines = new List<string>();
        foreach (var slice in envSlices)
        {
            var rung = TryExtractRungId(slice.Detail);
            if (rung is null)
                continue;

            var glyph = evt.Green || !IsFailureDetail(slice.Detail) ? "✓" : "✗";
            var duration = slice.DurationSeconds > 0 ? $"{slice.DurationSeconds:0.0}s" : "—";
            lines.Add($"  {glyph} {rung,-18} {duration}");
        }

        if (lines.Count == 0)
        {
            var fallback = envSlices.FirstOrDefault();
            var envText = fallback is null
                ? "—"
                : $"{fallback.DurationSeconds:0.0}s ({fallback.Detail ?? "environment"})";
            return $"  Environment: {envText}";
        }

        return string.Join('\n', lines);
    }

    private static string? TryExtractRungId(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return null;

        foreach (var rung in s_orderedRungs)
        {
            if (detail.StartsWith(rung, StringComparison.Ordinal)
                || detail.Contains(rung, StringComparison.Ordinal))
                return rung;
        }

        return null;
    }

    private static bool IsFailureDetail(string? detail) =>
        detail?.Contains("error", StringComparison.OrdinalIgnoreCase) == true
        || detail?.Contains("fail", StringComparison.OrdinalIgnoreCase) == true
        || detail?.Contains("missing", StringComparison.OrdinalIgnoreCase) == true;

    private static readonly string[] s_orderedRungs =
    [
        VerifyRung.DiagnoseFiles,
        VerifyRung.CompileProject,
        VerifyRung.BuildAffected,
        VerifyRung.TestScoped,
        VerifyRung.TestFull,
    ];

    public void Dispose() => _subscription.Dispose();
}
