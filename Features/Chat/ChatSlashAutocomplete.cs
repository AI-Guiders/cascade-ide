#nullable enable

namespace CascadeIDE.Features.Chat;

public sealed record ChatSlashSuggestion(string InsertText, string SlashPath, string Help);

/// <summary>Иерархические подсказки для <c>/</c> в ChatInput (ADR 0119 §6).</summary>
public static class ChatSlashAutocomplete
{
    public static IReadOnlyList<ChatSlashSuggestion> GetSuggestions(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return [];

        var trimmed = rawInput.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '/')
            return [];

        if (trimmed.Contains('\n') || trimmed.Contains('\r'))
            return [];

        var body = trimmed[1..];
        if (body.Length == 0)
            return ChatSlashCommandCatalog.AllSuggestions();

        var firstSpace = body.IndexOf(' ');
        if (firstSpace < 0)
        {
            return ChatSlashCommandCatalog.AllSuggestions()
                .Where(s => MatchesPrefix(s.SlashPath, body))
                .ToList();
        }

        var head = body[..firstSpace];
        var actionPrefix = body[(firstSpace + 1)..];
        return ChatSlashCommandCatalog.AllSuggestions()
            .Where(s => MatchesNamespaceActionPrefix(s.SlashPath, head, actionPrefix))
            .ToList();
    }

    private static bool MatchesPrefix(string slashPath, string prefixWithoutSlash)
    {
        if (slashPath.Length < 2)
            return false;

        var pathBody = slashPath[1..];
        return pathBody.StartsWith(prefixWithoutSlash, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesNamespaceActionPrefix(string slashPath, string namespaceHead, string actionPrefix)
    {
        var parts = slashPath.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        if (!parts[0].Equals("/" + namespaceHead, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrEmpty(actionPrefix))
            return true;

        return parts[1].StartsWith(actionPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
