using System.Text.Json;
using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    public Task<string> RequestConfirmationAsync(string message, CancellationToken cancellationToken)
    {
        var request = _host.RequestConfirmation;
        if (request is null)
            return Task.FromResult(ConfirmationResponses.Ok);
        return request(message ?? "", cancellationToken);
    }

    public string GetUiTheme() => UiThemeSnapshot.GetJson();
    public async Task<string> SetUiThemeAsync(string themeJson) =>
        await UiThemeApply.ApplyOnUiThreadAsync(themeJson ?? "");
    public async Task<string> GetUiLayoutAsync() =>
        await IdeMcpUiAutomationOrchestrator.InvokeJsonProviderOrDefaultAsync(UiScheduler.Default, _host.GetUiLayoutProvider);
    public async Task<string> GetColorsUnderCursorAsync() =>
        await IdeMcpUiAutomationOrchestrator.InvokeJsonProviderOrDefaultAsync(UiScheduler.Default, _host.GetColorsUnderCursorProvider);
    public async Task<string> GetControlAppearanceAsync(string? name) =>
        await IdeMcpUiAutomationOrchestrator.InvokeJsonAppearanceProviderAsync(
            UiScheduler.Default,
            _host.GetControlAppearanceProvider,
            name);
    public async Task<string> SetControlLayoutAsync(string controlName, string layoutJson) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            _host.SetControlLayoutProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.NormalizeJsonInput(layoutJson),
            IdeMcpUiAutomationOrchestrator.NoLayoutProviderMessage());
    public async Task<string> AddControlAsync(string parentName, string controlType, string? content, string? name) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            _host.AddControlProvider,
            parentName,
            controlType ?? "",
            content,
            name,
            IdeMcpUiAutomationOrchestrator.AddControlDisabledMessage());
    public async Task<string> SetControlTextAsync(string controlName, string text) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            _host.SetControlTextProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.NormalizeTextInput(text),
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());
    public async Task<string> ClickControlAsync(string? controlName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            _host.ClickControlProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());
    public async Task<string> SendKeysAsync(string? controlName, string keys) =>
        await IdeMcpUiAutomationOrchestrator.InvokeSendKeysProviderOrMessageAsync(
            UiScheduler.Default,
            _host.SendKeysProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.NormalizeTextInput(keys),
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());
    public async Task<string> SelectChatMessageAsync(int index) =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            () => _host.ChatPanel.SelectMessageByIndex(index));
    public async Task<string> SelectChatMessageByOrdinalAsync(int ordinal, int endOrdinal) =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            () => _host.ChatPanel.SelectMessageByOrdinalRangeInDetailLane(ordinal, endOrdinal));
    public async Task<string> GetSelectedChatMessageAsync() =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            _host.ChatPanel.GetSelectedMessageJson);

    public async Task<string> FindIntercomMessagesForCodeAsync(
        IReadOnlyDictionary<string, JsonElement>? args) =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            () => _host.ChatPanel.FindMessagesForCodeRefFromMcp(args));

    public async Task<string> RelateIntercomMessageRangeToCodeAsync(
        IReadOnlyDictionary<string, JsonElement>? args) =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            () => _host.ChatPanel.RelateMessageRangeToCodeRefFromMcp(args));
    public async Task<string> EditChatAssistantMessageAsync(string messageId, string newContent, string? reason) =>
        await IdeMcpUiAutomationOrchestrator.EditChatAssistantMessageOnUiAsync(
            UiScheduler.Default,
            messageId,
            newContent,
            reason,
            _host.ChatPanel.EditAssistantMessageById);
    public async Task<string> ExportChatReadableAsync(bool writeFile, string? fileName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeStringResultOnUiAsync(
            UiScheduler.Default,
            () => _host.ChatPanel.ExportReadableMarkdown(writeFile, fileName));
    public async Task<string> SetFocusAsync(string? controlName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            _host.SetFocusProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());
    public async Task<string> HighlightControlAsync(string? controlName) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            _host.HighlightControlProvider,
            controlName,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());
    public async Task<string> SetPanelSizeAsync(string panel, double? width, double? height) =>
        await IdeMcpUiAutomationOrchestrator.InvokeProviderOrMessageAsync(
            UiScheduler.Default,
            _host.SetPanelSizeProvider,
            panel,
            width,
            height,
            IdeMcpUiAutomationOrchestrator.DefaultProviderMissingMessage());

    public string GetSupportedEditorLanguages() => EditorLanguageSupport.GetJson();

}
