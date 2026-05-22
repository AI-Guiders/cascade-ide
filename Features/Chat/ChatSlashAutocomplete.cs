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
        int sessionTopicLimit = DefaultSessionTopicSuggestionLimit,
        int? caretIndex = null)
    {
        if (string.IsNullOrEmpty(rawInput))
            return [];

        if (!TryGetSlashTokenBeforeCaret(rawInput, caretIndex ?? rawInput.Length, out var body))
            return [];
        if (body.Length == 0)
            return BuildRootSegmentSuggestions();

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

        return BuildStaticSegmentSuggestions(body);
    }

    /// <summary>Заменить slash-токен на текущей строке; вернуть полный текст и каретку.</summary>
    public static bool TryReplaceSlashLineAtCaret(
        string rawInput,
        int caretIndex,
        string slashLineInsert,
        out string newText,
        out int newCaret)
    {
        newText = rawInput;
        newCaret = caretIndex;
        if (!TryGetSlashLineRange(rawInput, caretIndex, out var lineStart, out var lineEnd))
            return false;

        newText = rawInput[..lineStart] + slashLineInsert + rawInput[lineEnd..];
        newCaret = lineStart + slashLineInsert.Length;
        return true;
    }

    private static IReadOnlyList<ChatSlashSuggestion> BuildRootSegmentSuggestions()
    {
        var buckets = new Dictionary<string, ChatSlashSuggestion>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ChatSlashCommandCatalog.AllSuggestions())
        {
            if (!TrySplitPathBody(entry.SlashPath, out var segments) || segments.Count == 0)
                continue;

            var first = segments[0];
            if (buckets.ContainsKey(first))
                continue;

            var insert = "/" + first + " ";
            buckets[first] = new ChatSlashSuggestion(insert, "/" + first, entry.Help, entry.Group);
        }

        return buckets.Values
            .OrderBy(s => ChatSlashCommandCatalog.SortKeyForSuggestion(s.SlashPath))
            .ToList();
    }

    private static IReadOnlyList<ChatSlashSuggestion> BuildStaticSegmentSuggestions(string body)
    {
        ParseTypedBody(body, out var typedTokens, out var endsWithSpace);
        var buckets = new Dictionary<string, (string Insert, string Path, string Help, string? Group)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var entry in ChatSlashCommandCatalog.AllSuggestions())
        {
            if (!TrySplitPathBody(entry.SlashPath, out var pathSegments))
                continue;

            if (!TryGetCompletionSegmentIndex(pathSegments, typedTokens, endsWithSpace, out var segmentIndex))
                continue;

            if (segmentIndex >= pathSegments.Count)
                continue;

            var segmentValue = pathSegments[segmentIndex];
            if (!buckets.TryGetValue(segmentValue, out var existing)
                || entry.SlashPath.Length > existing.Path.Length)
            {
                var insert = BuildSlashLineInsert(body, pathSegments, segmentIndex, segmentValue);
                buckets[segmentValue] = (insert, entry.SlashPath, entry.Help, entry.Group);
            }
        }

        return buckets.Values
            .Select(v => new ChatSlashSuggestion(v.Insert, v.Path, v.Help, v.Group))
            .OrderBy(s => ChatSlashCommandCatalog.SortKeyForSuggestion(s.SlashPath))
            .ToList();
    }

    private static string BuildSlashLineInsert(
        string typedBody,
        IReadOnlyList<string> pathSegments,
        int completeSegmentIndex,
        string segmentValue)
    {
        ParseTypedBody(typedBody, out var typedTokens, out _);
        var resultSegs = new List<string>(completeSegmentIndex + 1);
        for (var i = 0; i < completeSegmentIndex; i++)
            resultSegs.Add(i < typedTokens.Count ? typedTokens[i] : pathSegments[i]);

        resultSegs.Add(segmentValue);
        var slashPath = "/" + string.Join(" ", resultSegs);
        if (completeSegmentIndex + 1 < pathSegments.Count
            || SegmentNeedsUserArgTail(slashPath))
            slashPath += " ";

        return slashPath;
    }

    private static bool SegmentNeedsUserArgTail(string slashPath)
    {
        if (ChatSlashCommandParser.ShouldAutoExecuteAfterAutocompleteCommit(slashPath))
            return false;

        if (IntentSlashCatalog.TryGetRoute(slashPath, out var route)
            && route.Completion != SlashCompletionKind.None)
            return true;

        ReadOnlySpan<string> suffixes =
        [
            " rename", " create", " set", " load", " find", " relate", " select", " file",
        ];
        foreach (var suffix in suffixes)
        {
            if (slashPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return slashPath.Contains(" open", StringComparison.OrdinalIgnoreCase)
               && !slashPath.Contains("dialog", StringComparison.OrdinalIgnoreCase);
    }

    private static void ParseTypedBody(string body, out List<string> tokens, out bool endsWithSpace)
    {
        endsWithSpace = body.Length > 0 && body[^1] == ' ';
        var trimmed = endsWithSpace ? body.TrimEnd() : body;
        tokens = trimmed.Length == 0
            ? []
            : trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static bool TrySplitPathBody(string slashPath, out List<string> segments)
    {
        segments = [];
        if (slashPath.Length < 2 || slashPath[0] != '/')
            return false;

        var body = slashPath[1..];
        if (body.Length == 0)
            return false;

        segments = body.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        return segments.Count > 0;
    }

    private static bool TryGetCompletionSegmentIndex(
        IReadOnlyList<string> pathSegments,
        IReadOnlyList<string> typedTokens,
        bool endsWithSpace,
        out int segmentIndex)
    {
        segmentIndex = 0;
        if (endsWithSpace)
        {
            if (typedTokens.Count > pathSegments.Count)
                return false;

            for (var i = 0; i < typedTokens.Count; i++)
            {
                if (!pathSegments[i].Equals(typedTokens[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            segmentIndex = typedTokens.Count;
            return segmentIndex < pathSegments.Count;
        }

        if (typedTokens.Count == 0)
        {
            segmentIndex = 0;
            return pathSegments.Count > 0;
        }

        for (var i = 0; i < typedTokens.Count - 1; i++)
        {
            if (i >= pathSegments.Count
                || !pathSegments[i].Equals(typedTokens[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var lastIdx = typedTokens.Count - 1;
        if (lastIdx >= pathSegments.Count)
            return false;

        var lastTyped = typedTokens[lastIdx];
        var pathSeg = pathSegments[lastIdx];
        if (!pathSeg.StartsWith(lastTyped, StringComparison.OrdinalIgnoreCase))
            return false;

        if (lastTyped.Equals(pathSeg, StringComparison.OrdinalIgnoreCase)
            && typedTokens.Count < pathSegments.Count)
        {
            segmentIndex = typedTokens.Count;
            return true;
        }

        segmentIndex = lastIdx;
        return true;
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
                    m.Label,
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

    /// <summary>Строка до каретки на текущей линии должна начинаться с <c>/</c> (после пробелов).</summary>
    internal static bool TryGetSlashTokenBeforeCaret(string rawInput, int caretIndex, out string body)
    {
        body = "";
        if (!TryGetSlashLineRange(rawInput, caretIndex, out var lineStart, out _))
            return false;

        var linePrefix = rawInput[lineStart..caretIndex];
        if (linePrefix.Contains('\r'))
            return false;

        var trimmed = linePrefix.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '/')
            return false;

        body = trimmed[1..];
        return true;
    }

    internal static bool TryGetSlashLineRange(string rawInput, int caretIndex, out int lineStart, out int lineEnd)
    {
        lineStart = 0;
        lineEnd = rawInput.Length;
        caretIndex = Math.Clamp(caretIndex, 0, rawInput.Length);
        lineStart = rawInput.LastIndexOf('\n', Math.Max(0, caretIndex - 1)) + 1;
        lineEnd = rawInput.IndexOf('\n', caretIndex);
        if (lineEnd < 0)
            lineEnd = rawInput.Length;

        var linePrefix = rawInput[lineStart..caretIndex];
        if (linePrefix.Contains('\r'))
            return false;

        var trimmed = linePrefix.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '/')
            return false;

        lineStart += linePrefix.Length - trimmed.Length;
        return true;
    }
}
