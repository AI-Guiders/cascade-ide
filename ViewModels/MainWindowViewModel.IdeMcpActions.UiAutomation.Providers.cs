#nullable enable
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: подтверждения, тема/лейаут, провайдеры UI automation и операции над чатом.</summary>
public partial class MainWindowViewModel
{
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

    async Task<string> Services.IIdeMcpActions.GetUiLayoutAsync() =>
        await IdeMcpUiAutomationOrchestrator.InvokeJsonProviderOrDefaultAsync(UiScheduler.Default, GetUiLayoutProvider);

    async Task<string> Services.IIdeMcpActions.GetColorsUnderCursorAsync() =>
        await IdeMcpUiAutomationOrchestrator.InvokeJsonProviderOrDefaultAsync(UiScheduler.Default, GetColorsUnderCursorProvider);

    async Task<string> Services.IIdeMcpActions.GetControlAppearanceAsync(string? name) =>
        await IdeMcpUiAutomationOrchestrator.InvokeJsonAppearanceProviderAsync(
            UiScheduler.Default,
            GetControlAppearanceProvider,
            name);

    async Task<string> Services.IIdeMcpActions.SetControlLayoutAsync(string controlName, string layoutJson) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            SetControlLayoutProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.NormalizeJsonInput(layoutJson),
            IdeMcpUiAutomationOrchestrator.NoLayoutProviderMessage());

    async Task<string> Services.IIdeMcpActions.AddControlAsync(string parentName, string controlType, string? content, string? name) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            AddControlProvider,
            parentName,
            controlType ?? "",
            content,
            name,
            IdeMcpUiAutomationOrchestrator.AddControlDisabledMessage());

    async Task<string> Services.IIdeMcpActions.SetControlTextAsync(string controlName, string text) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            SetControlTextProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.NormalizeTextInput(text),
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());

    async Task<string> Services.IIdeMcpActions.ClickControlAsync(string? controlName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            ClickControlProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());

    async Task<string> Services.IIdeMcpActions.SendKeysAsync(string? controlName, string keys) =>
        await IdeMcpUiAutomationOrchestrator.InvokeSendKeysProviderOrMessageAsync(
            UiScheduler.Default,
            SendKeysProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.NormalizeTextInput(keys),
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());

    async Task<string> Services.IIdeMcpActions.SelectChatMessageAsync(int index) =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            () => ChatPanel.SelectMessageByIndex(index));

    async Task<string> Services.IIdeMcpActions.GetSelectedChatMessageAsync() =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            ChatPanel.GetSelectedMessageJson);

    async Task<string> Services.IIdeMcpActions.EditChatAssistantMessageAsync(string messageId, string newContent, string? reason) =>
        await IdeMcpUiAutomationOrchestrator.EditChatAssistantMessageOnUiAsync(
            UiScheduler.Default,
            messageId,
            newContent,
            reason,
            ChatPanel.EditAssistantMessageById);

    async Task<string> Services.IIdeMcpActions.ExportChatReadableAsync(bool writeFile, string? fileName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            () => ChatPanel.ExportReadableMarkdown(writeFile, fileName));

    async Task<string> Services.IIdeMcpActions.SetFocusAsync(string? controlName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            SetFocusProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());

    async Task<string> Services.IIdeMcpActions.HighlightControlAsync(string? controlName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            HighlightControlProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());

    async Task<string> Services.IIdeMcpActions.SetPanelSizeAsync(string panel, double? width, double? height) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            SetPanelSizeProvider,
            panel,
            width,
            height,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());

    string Services.IIdeMcpActions.GetSupportedEditorLanguages() => EditorLanguageSupport.GetJson();
}
