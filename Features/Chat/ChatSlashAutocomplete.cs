#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

public sealed record ChatSlashSuggestion(
    string InsertText,
    string SlashPath,
    string Help,
    string? Group = null,
    string? StepSegment = null)
{
    /// <summary>Строка в popup: только следующий сегмент (домен / объект / intent).</summary>
    public string ListTitle => StepSegment ?? "";

    /// <summary>Вторичная строка: полная команда и описание.</summary>
    public string ListSubtitle =>
        string.Equals(InsertText.TrimEnd(), SlashPath, StringComparison.OrdinalIgnoreCase)
            ? Help
            : $"{SlashPath} — {Help}";
}

/// <summary>Контекст иерархии slash для шапки popup (путь + следующий шаг).</summary>
public sealed record ChatSlashHierarchyContext(string PathPrefix, string NextStepLabel, string Breadcrumb)
{
    public bool HasHeader => !string.IsNullOrEmpty(PathPrefix) || !string.IsNullOrEmpty(NextStepLabel);
}

/// <summary>Иерархические подсказки для <c>/</c> в ChatInput (ADR 0119 §6, 0125 dynamic).</summary>
public static class ChatSlashAutocomplete
{
    public const int DefaultWorkspaceFileSuggestionLimit = 30;
    public const int DefaultSessionTopicSuggestionLimit = 20;

    public static IReadOnlyList<ChatSlashSuggestion> GetSuggestions(
        string? rawInput,
        IWorkspaceFileSlashCompletionProvider? workspaceFiles = null,
        ISessionTopicSlashCompletionProvider? sessionTopics = null,
        IMessageAnchorSlashCompletionProvider? messageAnchors = null,
        int workspaceFileLimit = DefaultWorkspaceFileSuggestionLimit,
        int sessionTopicLimit = DefaultSessionTopicSuggestionLimit,
        int messageAnchorLimit = MessageAnchorSlashCompletionProvider.DefaultLimit,
        int? caretIndex = null)
    {
        if (string.IsNullOrEmpty(rawInput))
            return [];

        if (!TryGetSlashTokenBeforeCaret(rawInput, caretIndex ?? rawInput.Length, out var body))
            return [];

        body = SlashPathAliases.NormalizeCompletionBody(body);
        if (IntercomAnchorSlash.IsAnchorPeekHexIdEntryBody(body))
            return [];

        if (body.Length == 0)
            return SlashSemanticCatalogIndex.GetSegmentSuggestions([], endsWithSpace: false, body);

        if (TryGetDynamicSuggestions(
                body,
                SlashCompletionKind.WorkspaceFiles,
                workspaceFiles,
                null,
                null,
                workspaceFileLimit,
                sessionTopicLimit,
                messageAnchorLimit,
                out var fileSuggestions))
            return fileSuggestions;

        if (TryGetDynamicSuggestions(
                body,
                SlashCompletionKind.SessionTopics,
                null,
                sessionTopics,
                null,
                workspaceFileLimit,
                sessionTopicLimit,
                messageAnchorLimit,
                out var topicSuggestions))
            return topicSuggestions;

        if (TryGetDynamicSuggestions(
                body,
                SlashCompletionKind.MessageAnchors,
                null,
                null,
                messageAnchors,
                workspaceFileLimit,
                sessionTopicLimit,
                messageAnchorLimit,
                out var anchorSuggestions))
            return anchorSuggestions;

        ParseTypedBody(body, out var tokens, out var endsWithSpace);
        return SlashSemanticCatalogIndex.GetSegmentSuggestions(tokens, endsWithSpace, body);
    }

    /// <summary>Путь и подпись шага для шапки popup (домен → объект → действие → аргумент).</summary>
    public static ChatSlashHierarchyContext? GetHierarchyContext(string? rawInput, int? caretIndex = null)
    {
        if (string.IsNullOrEmpty(rawInput))
            return null;

        if (!TryGetSlashTokenBeforeCaret(rawInput, caretIndex ?? rawInput.Length, out var body))
            return null;

        ParseTypedBody(body, out var tokens, out var endsWithSpace);
        var pathPrefix = body.Length == 0 ? "/" : "/" + body.TrimEnd();
        SlashSemanticFields fields = default;
        var matchedPath = "";
        var hasSemantics = SlashSemanticCatalogIndex.TryResolveHierarchy(
            tokens,
            endsWithSpace,
            out fields,
            out matchedPath);

        var nextStep = !string.IsNullOrEmpty(fields.Domain)
            ? SlashRouteSemantics.GetNextStepLabel(tokens, endsWithSpace, fields, matchedPath)
            : ResolveHierarchyDepth(tokens, endsWithSpace) switch
            {
                0 => "домен",
                1 => "объект",
                2 => "действие",
                _ => "аргумент",
            };

        var breadcrumb = !string.IsNullOrEmpty(fields.Domain)
            ? SlashRouteSemantics.BuildSemanticBreadcrumb(tokens, fields, matchedPath)
            : BuildBreadcrumb(tokens, nextStep);

        return new ChatSlashHierarchyContext(pathPrefix, nextStep, breadcrumb);
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

    public static bool IsRunnableSlashLineAtCaret(string? rawInput, int caretIndex) =>
        SlashLineResolver.TryResolveLine(rawInput, caretIndex, out var line) && line.IsRunnable;

    internal static void ParseTypedBodyForResolver(string body, out List<string> tokens, out bool endsWithSpace) =>
        ParseTypedBody(body, out tokens, out endsWithSpace);

    private static void ParseTypedBody(string body, out List<string> tokens, out bool endsWithSpace)
    {
        endsWithSpace = body.Length > 0 && body[^1] == ' ';
        var trimmed = endsWithSpace ? body.TrimEnd() : body;
        tokens = trimmed.Length == 0
            ? []
            : trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static bool TryGetDynamicSuggestions(
        string body,
        SlashCompletionKind completionKind,
        IWorkspaceFileSlashCompletionProvider? workspaceFiles,
        ISessionTopicSlashCompletionProvider? sessionTopics,
        IMessageAnchorSlashCompletionProvider? messageAnchors,
        int workspaceFileLimit,
        int sessionTopicLimit,
        int messageAnchorLimit,
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
                    group,
                    FormatDynamicStepSegment(m.InsertPath)))
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
                    group,
                    FormatDynamicStepSegment(m.InsertArg)))
                .ToList();
            return true;
        }

        if (completionKind == SlashCompletionKind.MessageAnchors)
        {
            if (messageAnchors is null)
                return false;

            var matches = messageAnchors.GetMatches(argPrefix, messageAnchorLimit);
            if (matches.Count == 0)
                return false;

            suggestions = matches
                .Select(m => new ChatSlashSuggestion(
                    $"{route.SlashPath} {m.InsertArg}",
                    m.Label,
                    m.Help,
                    group,
                    FormatDynamicStepSegment(m.InsertArg)))
                .ToList();
            return true;
        }

        return false;
    }

    internal static string NormalizeSlashCompletionBody(string body) =>
        SlashPathAliases.NormalizeCompletionBody(body);

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

    /// <summary>Полная slash-строка на линии каретки (для preview/валидации в composer).</summary>
    public static bool TryGetSlashLineAtCaret(string? rawInput, int caretIndex, out string slashLine)
    {
        slashLine = "";
        if (string.IsNullOrEmpty(rawInput))
            return false;

        if (!TryGetSlashLineRange(rawInput, caretIndex, out var lineStart, out var lineEnd))
            return false;

        slashLine = rawInput[lineStart..lineEnd].TrimEnd();
        return slashLine.Length > 0;
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

    private static int ResolveHierarchyDepth(IReadOnlyList<string> tokens, bool endsWithSpace)
    {
        if (tokens.Count == 0)
            return 0;

        return endsWithSpace ? tokens.Count : Math.Max(0, tokens.Count - 1);
    }

    private static string BuildBreadcrumb(IReadOnlyList<string> tokens, string nextStepLabel)
    {
        if (tokens.Count == 0)
            return $"/ → {nextStepLabel}";

        var parts = new List<string>(tokens.Count + 2) { "/" };
        foreach (var token in tokens)
            parts.Add(token);

        parts.Add("…");
        return string.Join(" › ", parts);
    }

    private static string FormatDynamicStepSegment(string insertTail)
    {
        if (string.IsNullOrWhiteSpace(insertTail))
            return insertTail;

        var trimmed = insertTail.Trim();
        var lastSlash = trimmed.LastIndexOf('/');
        var leaf = lastSlash >= 0 ? trimmed[(lastSlash + 1)..] : trimmed;
        var space = leaf.IndexOf(' ');
        return space >= 0 ? leaf[..space] : leaf;
    }
}
