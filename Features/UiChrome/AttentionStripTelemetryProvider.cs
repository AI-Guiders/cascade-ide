using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Services;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Собирает <see cref="AttentionStripInputSnapshot"/> из состояния окна (сборка, тесты, отладка, git).
/// </summary>
public sealed class AttentionStripTelemetryProvider : IAttentionStripTelemetryProvider
{
    private readonly Func<bool> _isBuilding;
    private readonly Func<string> _lastTestSummary;
    private readonly Func<int> _impactedTestsBadge;
    private readonly IdeDapDebugSession _dapDebug;
    private readonly Func<InstrumentationPanelViewModel> _instrumentation;
    private readonly UiChromeViewModel _chrome;

    public AttentionStripTelemetryProvider(
        Func<bool> isBuilding,
        Func<string> lastTestSummary,
        Func<int> impactedTestsBadge,
        IdeDapDebugSession dapDebug,
        Func<InstrumentationPanelViewModel> instrumentation,
        UiChromeViewModel chrome)
    {
        _isBuilding = isBuilding;
        _lastTestSummary = lastTestSummary;
        _impactedTestsBadge = impactedTestsBadge;
        _dapDebug = dapDebug;
        _instrumentation = instrumentation;
        _chrome = chrome;
    }

    public AttentionStripInputSnapshot GetSnapshot()
    {
        var inst = _instrumentation();
        return AttentionStripTelemetryFormat.Compose(
            _isBuilding(),
            _lastTestSummary(),
            _impactedTestsBadge(),
            _dapDebug.HasActiveSession,
            _dapDebug.IsExecutionStopped,
            inst.DebugStackFrames.Count,
            inst.DebugVariables.Count,
            _chrome.TelemetryGitText,
            _chrome.TelemetryGitCockpitShort);
    }
}
