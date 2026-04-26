namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level helpers for IDE MCP UI automation actions.
/// Centralizes common provider guards and normalized defaults.
/// </summary>
public static class IdeMcpUiAutomationOrchestrator
{
    public static string DefaultJsonObject() => "{}";

    public static string DefaultProviderMissingMessage() => "No provider.";

    public static string InvalidMessageIdJson() => """{"ok":false,"error":"invalid_message_id"}""";

    public static string NormalizeJsonInput(string? json) =>
        string.IsNullOrEmpty(json) ? "{}" : json;

    public static string NormalizeTextInput(string? text) =>
        text ?? "";
}
