#nullable enable

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

        for (var len = tokens.Count; len >= 1; len--)
        {
            var path = "/" + string.Join(' ', tokens.Take(len));
            if (!SlashRouteCatalogIndex.TryGetRoute(path, out _))
                continue;

            var argTail = string.Join(' ', tokens.Skip(len));
            var hasArgTail = argTail.Length > 0;
            var isExactPath = len == tokens.Count && !endsWithSpace && !hasArgTail;
            var endsWithSpaceAfterPath = endsWithSpace && !hasArgTail && len == tokens.Count;

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

        return false;
    }
}
