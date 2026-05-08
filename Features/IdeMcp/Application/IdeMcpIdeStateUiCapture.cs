using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Поля главного окна, читаемые на UI-потоке для <see cref="IdeMcpWorkspaceOrchestrator.BuildIdeStatePayload"/>.
/// </summary>
public readonly record struct IdeMcpIdeStateUiCapture(
    string? SolutionPath,
    string? CurrentFilePath,
    string? SelectedSolutionPath,
    int EditorTextLength,
    int? SelectionStart,
    int? SelectionLength,
    IReadOnlyCollection<int> CurrentFileBreakpoints,
    DebugSessionSnapshot DebugSnapshot,
    bool IsBuildOutputVisible,
    string BuildOutputPreview,
    string? BinlogPath,
    bool IsTerminalVisible,
    string? UiMode,
    bool IsPfdRegionExpanded,
    bool IsMfdRegionExpanded,
    bool IsGitPanelVisible,
    bool IsInstrumentationDockVisible,
    string? SafetyLevel,
    int EditorGroupCount,
    int AgentTraceStepCount,
    bool IsAutonomousRunning,
    CockpitSurfaceState CockpitSurface);
