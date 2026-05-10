using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level helpers for IDE MCP UI automation actions.
/// Centralizes common provider guards and normalized defaults.
/// </summary>
[ApplicationOrchestrator]
public static class IdeMcpUiAutomationOrchestrator
{
    public static string DefaultJsonObject() => "{}";

    public static string DefaultProviderMissingMessage() => "No provider.";

    public static string NoLayoutProviderMessage() => "No layout provider.";

    public static string AddControlDisabledMessage() => "AddControl disabled.";

    /// <summary>Default window title when MCP passes no title for markdown preview.</summary>
    public static string DefaultMarkdownPreviewTitle() => "Превью";

    public static string ResolveMarkdownPreviewTitle(string? title) =>
        title ?? DefaultMarkdownPreviewTitle();

    public static string InvalidMessageIdJson() => """{"ok":false,"error":"invalid_message_id"}""";

    public static string NormalizeJsonInput(string? json) =>
        string.IsNullOrEmpty(json) ? "{}" : json;

    public static string NormalizeTextInput(string? text) =>
        text ?? "";

    public static bool ShouldSkipRemoveBreakpoint(string? filePath, int line) =>
        string.IsNullOrEmpty(filePath) || line < 1;

    /// <summary>После guard — нормализованный путь для <c>BreakpointsFileService</c> (MCP <c>remove_breakpoint</c>).</summary>
    public static bool TryGetRemoveBreakpointNormalizedPath(
        string? filePath,
        int line,
        [NotNullWhen(true)] out string? normalizedPath)
    {
        normalizedPath = null;
        if (ShouldSkipRemoveBreakpoint(filePath, line))
            return false;
        normalizedPath = NormalizeBreakpointFilePath(filePath!);
        return true;
    }

    /// <summary>Клик по gutter / toggle — нет строки или нет текущего файла.</summary>
    public static bool ShouldSkipToggleBreakpointInEditor(int line, string? currentFilePath) =>
        line < 1 || string.IsNullOrEmpty(currentFilePath);

    public static string NormalizeBreakpointFilePath(string filePath) =>
        CanonicalFilePath.Normalize(filePath);

    /// <summary>MCP/чат: результат со строкой JSON, только на UI-потоке.</summary>
    public static Task<string> InvokeStringResultOnUiAsync(IUiScheduler ui, Func<string> onUi) =>
        ui.InvokeAsync(onUi);

    /// <summary>MCP <c>edit_chat_assistant_message</c>: разбор id и нормализация текста на UI-потоке.</summary>
    public static Task<string> EditChatAssistantMessageOnUiAsync(
        IUiScheduler ui,
        string messageId,
        string newContent,
        string? reason,
        Func<Guid, string, string?, string> editOnUi) =>
        ui.InvokeAsync(() =>
        {
            if (!ChatMessageId.TryParse(messageId, out var id))
                return InvalidMessageIdJson();
            return editOnUi(id, NormalizeTextInput(newContent), reason);
        });

    public static async Task<string> InvokeJsonProviderOrDefaultAsync(
        IUiScheduler ui,
        Func<string?>? provider)
    {
        if (provider is null)
            return DefaultJsonObject();
        return await ui.InvokeAsync(() => provider() ?? DefaultJsonObject());
    }

    public static async Task<string> InvokeJsonAppearanceProviderAsync(
        IUiScheduler ui,
        Func<string?, string>? provider,
        string? name)
    {
        if (provider is null)
            return DefaultJsonObject();
        return await ui.InvokeAsync(() => provider(name) ?? DefaultJsonObject());
    }

    public static async Task<string> InvokeProviderOrMessageAsync(
        IUiScheduler ui,
        Func<string?, string>? provider,
        string? arg,
        string whenProviderMissing)
    {
        if (provider is null)
            return whenProviderMissing;
        return await ui.InvokeAsync(() => provider(arg));
    }

    public static async Task<string> InvokeProviderOrMessageAsync(
        IUiScheduler ui,
        Func<string, string, string>? provider,
        string a,
        string b,
        string whenProviderMissing)
    {
        if (provider is null)
            return whenProviderMissing;
        return await ui.InvokeAsync(() => provider(a, b));
    }

    public static async Task<string> InvokeProviderOrMessageAsync(
        IUiScheduler ui,
        Func<string, string, string?, string?, string>? provider,
        string a,
        string b,
        string? c,
        string? d,
        string whenProviderMissing)
    {
        if (provider is null)
            return whenProviderMissing;
        return await ui.InvokeAsync(() => provider(a, b, c, d));
    }

    public static async Task<string> InvokeProviderOrMessageAsync(
        IUiScheduler ui,
        Func<string, double?, double?, string>? provider,
        string panel,
        double? width,
        double? height,
        string whenProviderMissing)
    {
        if (provider is null)
            return whenProviderMissing;
        return await ui.InvokeAsync(() => provider(panel, width, height));
    }

    /// <summary>MCP <c>send_keys</c>: провайдер <c>(controlName, keys) → json</c>.</summary>
    public static async Task<string> InvokeSendKeysProviderOrMessageAsync(
        IUiScheduler ui,
        Func<string?, string, string>? provider,
        string? controlName,
        string keys,
        string whenProviderMissing)
    {
        if (provider is null)
            return whenProviderMissing;
        return await ui.InvokeAsync(() => provider(controlName, keys));
    }
}
