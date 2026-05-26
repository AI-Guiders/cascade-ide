#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

/// <summary>Устаревшие top-level слэши Intercom (ADR 0136).</summary>
internal static class SlashParseIntercomLegacy
{
    public static bool TryRejectRemovedHead(string head, out string reason)
    {
        reason = "";
        if (string.Equals(head, "topic", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom topic … (например /intercom topic list, /intercom topic open <id>).";
            return true;
        }

        if (string.Equals(head, "spine", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom spine … (например /intercom spine show, /intercom spine set <фокус>).";
            return true;
        }

        if (string.Equals(head, "overview", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom overview или /intercom topic cards.";
            return true;
        }

        if (string.Equals(head, "attach", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom attach … (selection, scope, file).";
            return true;
        }

        if (string.Equals(head, "card", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom topic create <название>.";
            return true;
        }

        if (string.Equals(head, "thread", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom message select|next|prev.";
            return true;
        }

        return false;
    }
}
