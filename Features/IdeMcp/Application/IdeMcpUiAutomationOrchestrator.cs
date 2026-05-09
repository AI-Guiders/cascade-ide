using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level helpers for IDE MCP UI automation actions.
/// Centralizes common provider guards and normalized defaults.
/// </summary>
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

    public static string NormalizeBreakpointFilePath(string filePath) =>
        Path.GetFullPath(filePath);

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
