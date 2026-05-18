#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

public sealed record ChatSlashSuggestion(string InsertText, string SlashPath, string Help, string? Group = null);

/// <summary>Иерархические подсказки для <c>/</c> в ChatInput (ADR 0119 §6, 0125 dynamic).</summary>
public static class ChatSlashAutocomplete
{
    public const int DefaultWorkspaceFileSuggestionLimit = 30;
    public const int DefaultSessionTopicSuggestionLimit = 20;

    public static IReadOnlyList<ChatSlashSuggestion> GetSuggestions(
        string? rawInput,
        IWorkspaceFileSlashCompletionProvider? workspaceFiles = null,
        ISessionTopicSlashCompletionProvider? sessionTopics = null,
        int workspaceFileLimit = DefaultWorkspaceFileSuggestionLimit,
        int sessionTopicLimit = DefaultSessionTopicSuggestionLimit)
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

        if (TryGetDynamicSuggestions(
                body,
                SlashCompletionKind.WorkspaceFiles,
                workspaceFiles,
                null,
                workspaceFileLimit,
                sessionTopicLimit,
                out var fileSuggestions))
            return fileSuggestions;

        if (TryGetDynamicSuggestions(
                body,
                SlashCompletionKind.SessionTopics,
                null,
                sessionTopics,
                workspaceFileLimit,
                sessionTopicLimit,
                out var topicSuggestions))
            return topicSuggestions;

        var firstSpace = body.IndexOf(' ');
        if (firstSpace < 0)
        {
            return ChatSlashCommandCatalog.AllSuggestions()
                .Where(s => MatchesPathPrefix(s.SlashPath, body))
                .OrderBy(s => ChatSlashCommandCatalog.SortKeyForSuggestion(s.SlashPath))
                .ToList();
        }

        return ChatSlashCommandCatalog.AllSuggestions()
            .Where(s => MatchesTypedPathPrefix(s.SlashPath, body))
            .OrderBy(s => ChatSlashCommandCatalog.SortKeyForSuggestion(s.SlashPath))
            .ToList();
    }

    private static bool TryGetDynamicSuggestions(
        string body,
        SlashCompletionKind completionKind,
        IWorkspaceFileSlashCompletionProvider? workspaceFiles,
        ISessionTopicSlashCompletionProvider? sessionTopics,
        int workspaceFileLimit,
        int sessionTopicLimit,
        out IReadOnlyList<ChatSlashSuggestion> suggestions)
    {
        suggestions = [];
        if (!TryResolveCompletionRoute(body, completionKind, out var route, out var argPrefix))
            return false;

        var group = ChatSlashCommandCatalog.GroupFor(route.SlashPath);
        if (completionKind == SlashCompletionKind.WorkspaceFiles)
        {
            if (workspaceFiles is null)
                return false;

            var matches = workspaceFiles.GetMatches(argPrefix, workspaceFileLimit);
            if (matches.Count == 0 && argPrefix.Length > 0)
                return false;

            suggestions = matches
                .Select(m => new ChatSlashSuggestion(
                    $"{route.SlashPath} {m.InsertPath}",
                    route.SlashPath,
                    m.Help,
                    group))
                .ToList();
            return true;
        }

        if (completionKind == SlashCompletionKind.SessionTopics)
        {
            if (sessionTopics is null)
                return false;

            var matches = sessionTopics.GetMatches(argPrefix, sessionTopicLimit);
            if (matches.Count == 0 && argPrefix.Length > 0)
                return false;

            suggestions = matches
                .Select(m => new ChatSlashSuggestion(
                    $"{route.SlashPath} {m.InsertArg}",
                    route.SlashPath,
                    m.Help,
                    group))
                .ToList();
            return true;
        }

        return false;
    }

    private static bool TryResolveCompletionRoute(
        string body,
        SlashCompletionKind completionKind,
        out SlashRouteEntry route,
        out string argPrefix)
    {
        route = default;
        argPrefix = "";

        SlashRouteEntry? best = null;
        var bestLen = -1;

        foreach (var candidate in IntentSlashCatalog.SlashRoutes.Values)
        {
            if (candidate.Completion != completionKind)
                continue;

            var pathBody = candidate.SlashPath.Length >= 2 ? candidate.SlashPath[1..] : "";
            if (body.Equals(pathBody, StringComparison.OrdinalIgnoreCase))
            {
                best = candidate;
                bestLen = pathBody.Length;
                argPrefix = "";
                break;
            }

            if (!body.StartsWith(pathBody, StringComparison.OrdinalIgnoreCase))
                continue;

            if (body.Length == pathBody.Length)
                continue;

            if (body[pathBody.Length] != ' ')
                continue;

            if (pathBody.Length <= bestLen)
                continue;

            best = candidate;
            bestLen = pathBody.Length;
            argPrefix = body[(pathBody.Length + 1)..];
        }

        if (best is null)
            return false;

        route = best.Value;
        return true;
    }

    private static bool MatchesPathPrefix(string slashPath, string prefixWithoutSlash)
    {
        if (slashPath.Length < 2)
            return false;

        var pathBody = slashPath[1..];
        return pathBody.StartsWith(prefixWithoutSlash, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesTypedPathPrefix(string slashPath, string typedBody)
    {
        if (slashPath.Length < 2)
            return false;

        var pathBody = slashPath[1..];
        return pathBody.StartsWith(typedBody, StringComparison.OrdinalIgnoreCase);
    }
}
