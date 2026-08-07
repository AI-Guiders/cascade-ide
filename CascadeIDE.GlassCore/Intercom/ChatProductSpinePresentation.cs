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

    /// <summary>Glass Face strip — never paint PreCondition / ADOPTED / ADR agent jargon.</summary>
    public static string FormatFaceStrip(ChatProductSpine spine)
    {
        if (!spine.HasContent)
            return "";

        var title = HumanizeSpineTitle(ResolveLineTitle(spine));
        var focus = HumanizeSpineFocus(FormatDetailStripFocus(spine.CurrentFocus));
        if (string.IsNullOrWhiteSpace(focus))
            return title;
        return title + " · " + focus;
    }

    static string HumanizeSpineTitle(string title)
    {
        if (title.Contains("PreCondition", StringComparison.OrdinalIgnoreCase))
            return "Glass Done";
        return title;
    }

    static string HumanizeSpineFocus(string focus)
    {
        if (LooksLikeAgentSpineJargon(focus))
        {
            if (focus.Contains("message select", StringComparison.OrdinalIgnoreCase)
                || focus.Contains("0136", StringComparison.OrdinalIgnoreCase)
                || focus.Contains("0138", StringComparison.OrdinalIgnoreCase))
                return "message select ready";
            if (focus.Contains("slash", StringComparison.OrdinalIgnoreCase))
                return "Intercom slash residual";
            return "Intercom adopted";
        }

        return focus.Length > 72 ? focus[..71].TrimEnd() + "…" : focus;
    }

    static bool LooksLikeAgentSpineJargon(string focus) =>
        focus.Contains("ADOPTED", StringComparison.OrdinalIgnoreCase)
        || focus.Contains("PreCondition", StringComparison.OrdinalIgnoreCase)
        || focus.Contains("0136", StringComparison.OrdinalIgnoreCase)
        || focus.Contains("0138", StringComparison.OrdinalIgnoreCase)
        || focus.Contains("SoftFL", StringComparison.OrdinalIgnoreCase)
        || focus.Contains("DIG REJECT", StringComparison.OrdinalIgnoreCase)
        || focus.Contains("residual Intercom", StringComparison.OrdinalIgnoreCase);
}
