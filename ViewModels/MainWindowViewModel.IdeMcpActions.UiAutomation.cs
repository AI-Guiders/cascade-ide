using System.IO;
using Avalonia.Threading;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: UI automation.</summary>
public partial class MainWindowViewModel
{
    void Services.IIdeMcpActions.FocusEditor()
    {
        UiScheduler.Default.Post(() => _focusEditorAction?.Invoke());
    }

    void Services.IIdeMcpActions.SetBreakpoint(string filePath, int line, string? condition)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return;
        var path = Path.GetFullPath(filePath);
        if (_breakpoints.Any(b => string.Equals(Path.GetFullPath(b.FilePath), path, StringComparison.OrdinalIgnoreCase) && b.Line == line))
            return;
        _breakpoints.Add((path, line));
        var ws = GetWorkspacePath();
        if (!string.IsNullOrEmpty(ws))
            BreakpointsFileService.SetBreakpointForDefaultTarget(ws, path, line, condition);
        OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
    }

    void Services.IIdeMcpActions.RemoveBreakpoint(string filePath, int line)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return;
        var path = Path.GetFullPath(filePath);
        _ = _breakpoints.RemoveAll(b => string.Equals(Path.GetFullPath(b.FilePath), path, StringComparison.OrdinalIgnoreCase) && b.Line == line) > 0;
        var ws = GetWorkspacePath();
        if (!string.IsNullOrEmpty(ws))
            BreakpointsFileService.RemoveBreakpointForDefaultTarget(ws, path, line);
        OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
    }

    /// <summary>Переключить брейкпоинт в .dotnet-debug-mcp-breakpoints.json для текущего файла и строки (клик по полю в редакторе).</summary>
    public void ToggleBreakpointInFile(int line)
    {
        if (line < 1 || string.IsNullOrEmpty(CurrentFilePath))
            return;
        var ws = GetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
            return;
        BreakpointsFileService.ToggleBreakpoint(ws, CurrentFilePath, line);
        OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
    }

    void Services.IIdeMcpActions.ShowPreview(string title, string content)
    {
        var t = title ?? "Превью";
        var c = content ?? "";
        UiScheduler.Default.Post(() => RequestShowMarkdownPreviewWindow?.Invoke(t, c));
    }

    void Services.IIdeMcpActions.ShowEditorPreview()
    {
        UiScheduler.Default.Post(() => RequestShowMarkdownPreviewForEditor?.Invoke());
    }

    [RelayCommand]
    private void OpenPreviewWindow()
    {
        RequestShowMarkdownPreviewForEditor?.Invoke();
    }

    Task<string> Services.IIdeMcpActions.RequestConfirmationAsync(string message, CancellationToken cancellationToken)
    {
        var request = RequestConfirmation;
        if (request is null)
            return Task.FromResult(ConfirmationResponses.Ok);
        return request(message ?? "", cancellationToken);
    }

    string Services.IIdeMcpActions.GetUiTheme() => UiThemeSnapshot.GetJson();

    async Task<string> Services.IIdeMcpActions.SetUiThemeAsync(string themeJson) =>
        await UiThemeApply.ApplyOnUiThreadAsync(themeJson ?? "");

    async Task<string> Services.IIdeMcpActions.GetUiLayoutAsync()
    {
        var provider = GetUiLayoutProvider;
        if (provider is null)
            return "{}";
        return await UiScheduler.Default.InvokeAsync(() => provider() ?? "{}");
    }

    async Task<string> Services.IIdeMcpActions.GetColorsUnderCursorAsync()
    {
        var provider = GetColorsUnderCursorProvider;
        if (provider is null)
            return "{}";
        return await UiScheduler.Default.InvokeAsync(() => provider() ?? "{}");
    }

    async Task<string> Services.IIdeMcpActions.GetControlAppearanceAsync(string? name)
    {
        var provider = GetControlAppearanceProvider;
        if (provider is null)
            return "{}";
        return await UiScheduler.Default.InvokeAsync(() => provider(name) ?? "{}");
    }

    async Task<string> Services.IIdeMcpActions.SetControlLayoutAsync(string controlName, string layoutJson)
    {
        var provider = SetControlLayoutProvider;
        if (provider is null)
            return "No layout provider.";
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName, layoutJson ?? "{}"));
    }

    async Task<string> Services.IIdeMcpActions.AddControlAsync(string parentName, string controlType, string? content, string? name)
    {
        var provider = AddControlProvider;
        if (provider is null)
            return "AddControl disabled.";
        return await UiScheduler.Default.InvokeAsync(() => provider(parentName, controlType ?? "", content, name));
    }

    async Task<string> Services.IIdeMcpActions.SetControlTextAsync(string controlName, string text)
    {
        var provider = SetControlTextProvider;
        if (provider is null)
            return "No provider.";
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName, text ?? ""));
    }

    async Task<string> Services.IIdeMcpActions.ClickControlAsync(string? controlName)
    {
        var provider = ClickControlProvider;
        if (provider is null)
            return "No provider.";
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.SendKeysAsync(string? controlName, string keys)
    {
        var provider = SendKeysProvider;
        if (provider is null)
            return "No provider.";
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName, keys ?? ""));
    }

    async Task<string> Services.IIdeMcpActions.SetFocusAsync(string? controlName)
    {
        var provider = SetFocusProvider;
        if (provider is null)
            return "No provider.";
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.HighlightControlAsync(string? controlName)
    {
        var provider = HighlightControlProvider;
        if (provider is null)
            return "No provider.";
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.SetPanelSizeAsync(string panel, double? width, double? height)
    {
        var provider = SetPanelSizeProvider;
        if (provider is null)
            return "No provider.";
        return await UiScheduler.Default.InvokeAsync(() => provider(panel, width, height));
    }

    string Services.IIdeMcpActions.GetSupportedEditorLanguages() => EditorLanguageSupport.GetJson();
}
