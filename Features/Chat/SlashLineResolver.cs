#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Канонический путь + хвост args из текста slash-строки (ADR 0150).
/// Единая точка для autocomplete, Enter и runner.
/// </summary>
public static class SlashLineResolver
{
    public readonly record struct SlashLineResolution(
        string CanonicalPath,
        string ArgTail,
        SlashArgTailKind ArgTailKind,
        bool IsCatalogMatch,
        bool IsExactPathMatch,
        bool EndsWithSpaceAfterPath,
        bool HasArgTailContent)
    {
        public bool ShouldHideSegmentSuggestions =>
            IsCatalogMatch && (
                (ArgTailKind == SlashArgTailKind.None && IsExactPathMatch)
                || ArgTailKind == SlashArgTailKind.Optional && (
                    IsExactPathMatch || EndsWithSpaceAfterPath || HasArgTailContent)
                || (ArgTailKind == SlashArgTailKind.Required && HasArgTailContent));

        public bool InsertsTrailingSpaceOnCommit => ArgTailKind != SlashArgTailKind.None;

        public bool IsRunnable =>
            IsCatalogMatch
            && (ArgTailKind != SlashArgTailKind.Required || !string.IsNullOrWhiteSpace(ArgTail))
            && ChatSlashCommandCatalog.TryResolveCanonical(CanonicalPath, ArgTail, out _);
    }

    public static bool TryResolveLine(string? rawInput, int caretIndex, out SlashLineResolution resolution)
    {
        resolution = default;
        if (!ChatSlashAutocomplete.TryGetSlashLineAtCaret(rawInput, caretIndex, out var slashLine))
            return false;

        return TryResolveSlashLine(slashLine, out resolution);
    }

    public static bool TryResolveSlashLine(string slashLine, out SlashLineResolution resolution)
    {
        resolution = default;
        if (string.IsNullOrWhiteSpace(slashLine) || slashLine[0] != '/')
            return false;

        var body = slashLine[1..].TrimEnd();
        return TryResolveBody(body, out resolution);
    }

    internal static bool TryResolveBody(string body, out SlashLineResolution resolution)
    {
        resolution = default;
        ChatSlashAutocomplete.ParseTypedBodyForResolver(body, out var tokens, out var endsWithSpace);
        if (tokens.Count == 0)
            return false;

        if (TryResolveCatalogLongestPrefix(tokens, endsWithSpace, out resolution))
            return true;

        if (TryResolveIntercomMessageInlineRangePath(tokens, endsWithSpace, out resolution))
            return true;

        return false;
    }

    private static bool TryResolveCatalogLongestPrefix(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out SlashLineResolution resolution)
    {
        resolution = default;
        if (!SlashRouteCatalogPathsGenerated.TryResolveLongestPrefix(
                tokens,
                endsWithSpace,
                out var path,
                out var argTail,
                out var isExactPath,
                out var endsWithSpaceAfterPath)
            && !CascadeIDE.Features.Forge.Infrastructure.ForgeSlashCatalogOverlay.TryResolveLongestPrefix(
                tokens,
                endsWithSpace,
                out path,
                out argTail,
                out isExactPath,
                out endsWithSpaceAfterPath))
        {
            return false;
        }

        var hasArgTail = argTail.Length > 0;
        resolution = new SlashLineResolution(
            path,
            argTail,
            SlashRouteCatalogIndex.GetArgTailKind(path),
            IsCatalogMatch: true,
            IsExactPathMatch: isExactPath,
            EndsWithSpaceAfterPath: endsWithSpaceAfterPath,
            HasArgTailContent: hasArgTail);
        return true;
    }

    /// <summary>
    /// <c>/intercom message 3:5 relate selection</c> — диапазон gutter между <c>message</c> и action-токеном.
    /// </summary>
    private static bool TryResolveIntercomMessageInlineRangePath(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out SlashLineResolution resolution)
    {
        resolution = default;
        if (tokens.Count < 4)
            return false;

        if (!string.Equals(tokens[0], "intercom", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(tokens[1], "message", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        ReadOnlySpan<string> inlineActions = ["relate", "select", "find"];
        for (var actionIdx = 2; actionIdx < tokens.Count; actionIdx++)
        {
            var token = tokens[actionIdx];
            var matchedAction = false;
            foreach (var action in inlineActions)
            {
                if (string.Equals(token, action, StringComparison.OrdinalIgnoreCase))
                {
                    matchedAction = true;
                    break;
                }
            }

            if (!matchedAction || actionIdx <= 2)
                continue;

            var path = $"/intercom message {token.ToLowerInvariant()}";
            if (!SlashRouteCatalogPathsGenerated.ContainsPath(path))
                continue;

            var inlineArg = string.Join(' ', tokens.Skip(2).Take(actionIdx - 2));
            var tailAfterAction = string.Join(' ', tokens.Skip(actionIdx + 1));
            var argTail = string.IsNullOrEmpty(tailAfterAction)
                ? inlineArg
                : string.IsNullOrEmpty(inlineArg)
                    ? tailAfterAction
                    : $"{inlineArg} {tailAfterAction}";

            var hasArgTail = argTail.Length > 0;
            resolution = new SlashLineResolution(
                path,
                argTail,
                SlashRouteCatalogIndex.GetArgTailKind(path),
                IsCatalogMatch: true,
                IsExactPathMatch: false,
                EndsWithSpaceAfterPath: endsWithSpace && !hasArgTail && actionIdx + 1 == tokens.Count,
                HasArgTailContent: hasArgTail);
            return true;
        }

        return false;
    }
}
