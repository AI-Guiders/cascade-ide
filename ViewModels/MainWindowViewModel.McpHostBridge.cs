using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Git;
using CascadeIDE.Features.IdeMcp.Execution;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Внутренний мост для <see cref="Features.IdeMcp.Application.MainWindowIdeMcpHost"/> (Wave 2 Big Bang).</summary>
public partial class MainWindowViewModel
{
    internal ContextMinimizer McpContextMinimizer => _contextMinimizer;

    internal McpDotnetBuildTestService McpBuildTest => _mcpBuildTest;

    internal McpAgentNotesService McpAgentNotes => _mcpAgentNotes;

    internal HybridIndexOrchestrator McpHybridIndex => _hybridIndex;

    internal CascadeIdeSettings McpSettings => _settings;

    internal IGitCommandRunner McpGitRunner => _gitRunner;

    internal string McpGetWorkspacePath() => GetWorkspacePath();

    internal Task McpRefreshGitSummaryAsync() => RefreshGitSummaryAsync();

    internal void McpRegisterIdeMcpBreakpoint(string filePath, int line, string? condition) =>
        Editor.RegisterIdeMcpBreakpoint(filePath, line, condition);

    internal void McpNotifyBreakpointGlyphBindings() => Editor.NotifyBreakpointGlyphBindings();

    internal void McpResyncDapBreakpointsFireAndForget() => ResyncDapBreakpointsFireAndForget();

    internal int? McpEditorCaretOffset => NavigationMap.EditorCaretOffset;

    internal Action? McpFocusEditorAction => _focusEditorAction;

    internal Func<int?, EditorStateDto?>? McpEditorStateProvider => _editorStateProvider;

    internal Func<int, int, string?>? McpEditorContentRangeProvider => _editorContentRangeProvider;

    internal Action<string, int, int, int, int, string>? McpApplyEditAction => _applyEditAction;

    internal Action<string?, int, int, int?>? McpRevealEditorRangeAction => _revealEditorRangeAction;

    internal void McpNotifyPropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

    internal void McpPublishToIdeDataBusAndRebuild<T>(T evt) => PublishToIdeDataBusAndRebuild(evt);

    internal void HostSaveSettingsIfChanged() => SaveSettingsIfChanged();

    internal Services.CSharpLanguageService HostCsharpLanguageService => _csharpLanguageService;

    internal Services.WorkspaceDiagnosticsCoordinator HostWorkspaceDiagnostics => _workspaceDiagnostics;

    internal Services.IDotnetCommandRunner HostDotnetRunner => _dotnetRunner;

    internal void HostUpdateCodeNavigationMapCaretOffset(int? offset) =>
        UpdateCodeNavigationMapCaretOffset(offset);

    internal int McpDebugStackSelectedIndex
    {
        get => _debugStackSelectedIndex;
        set => _debugStackSelectedIndex = value;
    }

    internal bool McpSuppressDebugStackSelectedIndex
    {
        get => _suppressDebugStackSelectedIndex;
        set => _suppressDebugStackSelectedIndex = value;
    }

    internal string? McpLastBuildBinlogPath
    {
        get => _lastBuildBinlogPath;
        set => _lastBuildBinlogPath = value;
    }

    /// <summary>Прокси для UI редактора (клик по полю брейкпоинта).</summary>
    public void ToggleBreakpointInFile(int line) => _ideMcpHost.ToggleBreakpointInFile(line);

    MainWindowViewModel IMainWindowMcpHostContext.Vm => this;
}
