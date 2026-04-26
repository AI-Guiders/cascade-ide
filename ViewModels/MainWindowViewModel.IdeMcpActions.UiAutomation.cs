#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Features.IdeMcp.Application;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: UI automation.</summary>
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
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return;
        var path = Path.GetFullPath(filePath);
        var ws = GetWorkspacePath();
        if (!string.IsNullOrEmpty(ws))
            BreakpointsFileService.RemoveBreakpointForBundledSampleTarget(ws, path, line);
        OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
        ResyncDapBreakpointsFireAndForget();
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
        OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
        ResyncDapBreakpointsFireAndForget();
    }

    void Services.IIdeMcpActions.ShowPreview(string title, string content)
    {
        var t = title ?? "Превью";
        var c = content ?? "";
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
            return IdeMcpUiAutomationOrchestrator.DefaultJsonObject();
        return await UiScheduler.Default.InvokeAsync(() => provider() ?? IdeMcpUiAutomationOrchestrator.DefaultJsonObject());
    }

    async Task<string> Services.IIdeMcpActions.GetColorsUnderCursorAsync()
    {
        var provider = GetColorsUnderCursorProvider;
        if (provider is null)
            return IdeMcpUiAutomationOrchestrator.DefaultJsonObject();
        return await UiScheduler.Default.InvokeAsync(() => provider() ?? IdeMcpUiAutomationOrchestrator.DefaultJsonObject());
    }

    async Task<string> Services.IIdeMcpActions.GetControlAppearanceAsync(string? name)
    {
        var provider = GetControlAppearanceProvider;
        if (provider is null)
            return IdeMcpUiAutomationOrchestrator.DefaultJsonObject();
        return await UiScheduler.Default.InvokeAsync(() => provider(name) ?? IdeMcpUiAutomationOrchestrator.DefaultJsonObject());
    }

    async Task<string> Services.IIdeMcpActions.SetControlLayoutAsync(string controlName, string layoutJson)
    {
        var provider = SetControlLayoutProvider;
        if (provider is null)
            return "No layout provider.";
        return await UiScheduler.Default.InvokeAsync(() =>
            provider(controlName, IdeMcpUiAutomationOrchestrator.NormalizeJsonInput(layoutJson)));
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
            return IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage();
        return await UiScheduler.Default.InvokeAsync(() =>
            provider(controlName, IdeMcpUiAutomationOrchestrator.NormalizeTextInput(text)));
    }

    async Task<string> Services.IIdeMcpActions.ClickControlAsync(string? controlName)
    {
        var provider = ClickControlProvider;
        if (provider is null)
            return IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage();
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.SendKeysAsync(string? controlName, string keys)
    {
        var provider = SendKeysProvider;
        if (provider is null)
            return IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage();
        return await UiScheduler.Default.InvokeAsync(() =>
            provider(controlName, IdeMcpUiAutomationOrchestrator.NormalizeTextInput(keys)));
    }

    async Task<string> Services.IIdeMcpActions.SelectChatMessageAsync(int index) =>
        await UiScheduler.Default.InvokeAsync(() => ChatPanel.SelectMessageByIndex(index));

    async Task<string> Services.IIdeMcpActions.GetSelectedChatMessageAsync() =>
        await UiScheduler.Default.InvokeAsync(ChatPanel.GetSelectedMessageJson);

    async Task<string> Services.IIdeMcpActions.EditChatAssistantMessageAsync(string messageId, string newContent, string? reason) =>
        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!Guid.TryParse(messageId, out var id))
                return IdeMcpUiAutomationOrchestrator.InvalidMessageIdJson();
            return ChatPanel.EditAssistantMessageById(id, IdeMcpUiAutomationOrchestrator.NormalizeTextInput(newContent), reason);
        });

    async Task<string> Services.IIdeMcpActions.ExportChatReadableAsync(bool writeFile, string? fileName) =>
        await UiScheduler.Default.InvokeAsync(() => ChatPanel.ExportReadableMarkdown(writeFile, fileName));

    async Task<string> Services.IIdeMcpActions.SetFocusAsync(string? controlName)
    {
        var provider = SetFocusProvider;
        if (provider is null)
            return IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage();
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.HighlightControlAsync(string? controlName)
    {
        var provider = HighlightControlProvider;
        if (provider is null)
            return IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage();
        return await UiScheduler.Default.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.SetPanelSizeAsync(string panel, double? width, double? height)
    {
        var provider = SetPanelSizeProvider;
        if (provider is null)
            return IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage();
        return await UiScheduler.Default.InvokeAsync(() => provider(panel, width, height));
    }

    string Services.IIdeMcpActions.GetSupportedEditorLanguages() => EditorLanguageSupport.GetJson();
}
