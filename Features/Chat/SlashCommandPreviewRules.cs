#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Цепочка правил slash-preview (P5); порядок = приоритет.</summary>
internal static class SlashCommandPreviewRulePipeline
{
    private static readonly ISlashCommandPreviewRule[] Rules =
    [
        new NotSlashPreviewRule(),
        new AnchorPeekPreviewRule(),
        new EditorLineSelectPreviewRule(),
        new CatalogSlashPreviewRule(),
        new UnknownSlashPreviewRule(),
    ];

    public static SlashCommandPreviewResult Evaluate(
        string? bufferText,
        SlashCommandAnchorPreviewResolver? resolveAnchor)
    {
        foreach (var rule in Rules)
        {
            if (rule.TryEvaluate(bufferText, resolveAnchor, out var result))
                return result;
        }

        return SlashCommandPreviewResult.Empty;
    }

    private interface ISlashCommandPreviewRule
    {
        bool TryEvaluate(
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result);
    }

    private sealed class NotSlashPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (ChatSlashCommandParser.IsSlashLine(bufferText))
                return TryReject(out result);

            result = SlashCommandPreviewResult.Empty;
            return true;
        }
    }

    private sealed class AnchorPeekPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!SlashLineResolver.TryResolveSlashLine((bufferText ?? "").Trim(), out var line)
                || !line.IsCatalogMatch
                || !SlashPathAliases.IsAnchorPeekPath(line.CanonicalPath))
            {
                return TryReject(out result);
            }

            result = SlashCommandPreviewRuleHelpers.BuildAnchorPeek(
                SlashPathAliases.ExtractPeekArgs(line.CanonicalPath, line.ArgTail),
                resolveAnchor);
            return true;
        }
    }

    private sealed class EditorLineSelectPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!SlashLineResolver.TryResolveSlashLine((bufferText ?? "").Trim(), out var line)
                || !line.IsCatalogMatch
                || !SlashCommandPreviewRuleHelpers.IsEditorLineSelect(line.CanonicalPath))
            {
                return TryReject(out result);
            }

            result = SlashCommandPreviewRuleHelpers.BuildParametricPreview(
                line.ArgTail,
                "Строки",
                line.CanonicalPath);
            return true;
        }
    }

    private sealed class CatalogSlashPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!ChatSlashCommandCatalog.TryResolveInput(bufferText, out var descriptor, out var resolvedArgTail))
                return TryReject(out result);

            if (SlashIntercomPreviewPolicies.TryBuild(
                    descriptor.SlashPath,
                    resolvedArgTail,
                    resolveAnchor,
                    out result))
            {
                return true;
            }

            result = SlashCommandPreviewRuleHelpers.BuildCatalogCommandPreview(descriptor.SlashPath, resolvedArgTail);
            return true;
        }
    }

    private sealed class UnknownSlashPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!ChatSlashCommandParser.IsSlashLine(bufferText))
                return TryReject(out result);

            result = new("Нет такой команды.", SlashCommandPreviewKind.Error);
            return true;
        }
    }

    private static bool TryReject(out SlashCommandPreviewResult result)
    {
        result = default;
        return false;
    }
}

internal static class SlashCommandPreviewRuleHelpers
{
    public static SlashCommandPreviewResult BuildCatalogCommandPreview(string slashPath, string? argsTail)
    {
        var tail = (argsTail ?? "").Trim();
        if (tail.Length == 0)
            return new($"Команда «{slashPath}» — Enter для выполнения.", SlashCommandPreviewKind.Ok);

        return new($"Команда «{slashPath}».", SlashCommandPreviewKind.Ok);
    }

    public static SlashCommandPreviewResult BuildAnchorPeek(string? tail, SlashCommandAnchorPreviewResolver? resolveAnchor)
    {
        var raw = (tail ?? "").Trim();
        if (raw.Length == 0)
            return new("Укажи № якоря (1…) или 8 hex: /anchor peek 1.", SlashCommandPreviewKind.Incomplete);

        if (resolveAnchor is not null && resolveAnchor(raw, out var resolved))
            return resolved;

        if (raw.Length > 0 && raw.Length < 8 && IntercomAnchorSlash.IsPartialHexAnchorId(raw))
            return new("Id якоря — 8 hex (как a:abcd1234).", SlashCommandPreviewKind.Incomplete);

        if (!IntercomAnchorSlash.TryNormalizeAnchorId(tail, out _, out var syntaxError))
            return new(syntaxError, SlashCommandPreviewKind.Error);

        return new($"Peek: {raw}", SlashCommandPreviewKind.Incomplete);
    }

    public static SlashCommandPreviewResult BuildParametricPreview(
        string tail,
        string unitLabel,
        string slashPath)
    {
        var trimmed = tail.Trim();
        if (trimmed.Length == 0)
        {
            if (!ParametricSegmentListParser.TryParse("", out _, out var needArgsError))
                needArgsError = "Укажи аргументы диапазона.";

            return new(needArgsError, SlashCommandPreviewKind.Incomplete);
        }

        if (!ParametricSegmentListParser.TryParse(trimmed, out var segments, out var error))
            return new(error, SlashCommandPreviewKind.Error);

        if (!ChatSlashCommandCatalog.TryResolveCanonical(slashPath, tail, out _))
            return new("Нет такой команды.", SlashCommandPreviewKind.Error);

        return new(
            ParametricSegmentListParser.FormatSummary(segments, unitLabel),
            SlashCommandPreviewKind.Ok);
    }

    public static bool IsEditorLineSelect(string canonicalPath) =>
        string.Equals(canonicalPath, "/editor line select", StringComparison.OrdinalIgnoreCase);
}
