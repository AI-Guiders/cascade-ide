namespace CascadeIDE.Features.Chat;

/// <summary>UI-строки product spine (presentation), без Skia.</summary>
public static class ChatProductSpinePresentation
{
    public const string DefaultLineTitle = "Продуктовая линия";

    public static string ResolveLineTitle(ChatProductSpine spine) =>
        string.IsNullOrWhiteSpace(spine.LineTitle) ? DefaultLineTitle : spine.LineTitle.Trim();

    public static string FormatAgentContextFooter(bool includeInAgentContext) =>
        includeInAgentContext ? "в контексте агента" : "не в контексте агента";

    public static string FormatDetailStripFocus(string? currentFocus) =>
        string.IsNullOrWhiteSpace(currentFocus) ? "См. overview (atb)" : currentFocus.Trim();
}
