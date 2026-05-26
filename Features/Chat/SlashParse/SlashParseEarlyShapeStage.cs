#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseEarlyShapeStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (work.InlineAction is null && !string.IsNullOrEmpty(work.Tail) && isFlatVerbWithArgTail(work.Head, work.Tail))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.Flat,
                work.Head,
                null,
                null,
                work.Tail,
                null);
        }

        if (work.InlineAction is not null)
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                work.Head,
                work.InlineAction,
                null,
                work.Tail,
                null);
        }

        if (string.IsNullOrEmpty(work.Tail))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.Flat,
                work.Head,
                null,
                null,
                "",
                null);
        }

        return null;
    }

    private static bool isFlatVerbWithArgTail(string head, string tail)
    {
        if (!string.Equals(head, "spine", StringComparison.OrdinalIgnoreCase))
            return false;

        var first = tail.IndexOf(' ') < 0 ? tail : tail[..tail.IndexOf(' ')];
        return !isSpineNamespaceActionToken(first);
    }

    private static bool isSpineNamespaceActionToken(string token) =>
        string.Equals(token, "set", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "show", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "toggle", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "list", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "tree", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "open", StringComparison.OrdinalIgnoreCase);
}
