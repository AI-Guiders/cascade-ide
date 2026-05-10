#nullable enable
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: фокус редактора, брейкпоинты, превью Markdown и relay-команды страницы превью.</summary>
public partial class MainWindowViewModel
{
    void Services.IIdeMcpActions.FocusEditor()
    {
        UiScheduler.Default.Post(() => _focusEditorAction?.Invoke());
    }

    void Services.IIdeMcpActions.SetBreakpoint(string filePath, int line, string? condition) =>
        RegisterIdeMcpBreakpoint(filePath, line, condition);

    void Services.IIdeMcpActions.RemoveBreakpoint(string filePath, int line)
    {
        if (!IdeMcpUiAutomationOrchestrator.TryGetRemoveBreakpointNormalizedPath(filePath, line, out var path))
            return;
        var ws = GetWorkspacePath();
        if (!string.IsNullOrEmpty(ws))
            BreakpointsFileService.RemoveBreakpointForBundledSampleTarget(ws, path, line);
        NotifyBreakpointGlyphBindings();
        ResyncDapBreakpointsFireAndForget();
    }

    /// <summary>Переключить брейкпоинт в .dotnet-debug-mcp-breakpoints.json для текущего файла и строки (клик по полю в редакторе).</summary>
    public void ToggleBreakpointInFile(int line)
    {
        if (IdeMcpUiAutomationOrchestrator.ShouldSkipToggleBreakpointInEditor(line, CurrentFilePath))
            return;
        var ws = GetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
            return;
        var filePath = CurrentFilePath!;
        BreakpointsFileService.ToggleBreakpoint(ws, filePath, line);
        NotifyBreakpointGlyphBindings();
        ResyncDapBreakpointsFireAndForget();
    }

    void Services.IIdeMcpActions.ShowPreview(string title, string content)
    {
        var t = IdeMcpUiAutomationOrchestrator.ResolveMarkdownPreviewTitle(title);
        var c = IdeMcpUiAutomationOrchestrator.NormalizeTextInput(content);
        UiScheduler.Default.Post(() => RequestShowMarkdownPreviewWindow?.Invoke(t, c));
    }

    void Services.IIdeMcpActions.ShowEditorPreview()
    {
        UiScheduler.Default.Post(() =>
        {
            if (ShowMarkdownPreviewPageCommand.CanExecute(null))
                ShowMarkdownPreviewPageCommand.Execute(null);
        });
    }

    [RelayCommand]
    private void ShowMarkdownPreviewPage()
    {
        ApplyMfdRegionExpanded(true);
        MarkdownPreviewTool.RefreshFromEditor();
        TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
    }

    [RelayCommand]
    private void OpenPreviewWindow()
    {
        RequestShowMarkdownPreviewForEditor?.Invoke();
    }
}
