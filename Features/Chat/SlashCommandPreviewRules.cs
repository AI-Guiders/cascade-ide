#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Цепочка правил slash-preview (P5); порядок = приоритет.</summary>
internal static class SlashCommandPreviewRulePipeline
{
    private static readonly ISlashCommandPreviewRule[] Rules =
    [
        new NotSlashPreviewRule(),
        new RejectedSlashPreviewRule(),
        new AnchorPeekPreviewRule(),
        new EditorLineSelectPreviewRule(),
        new IntercomPathPreviewRule(),
        new CatalogSlashPreviewRule(),
        new UnknownSlashPreviewRule(),
    ];

    public static SlashCommandPreviewResult Evaluate(
        string? bufferText,
        SlashCommandAnchorPreviewResolver? resolveAnchor)
    {
        var parse = ChatSlashCommandParser.TryParse(bufferText);
        foreach (var rule in Rules)
        {
            if (rule.TryEvaluate(parse, bufferText, resolveAnchor, out var result))
                return result;
        }

        return SlashCommandPreviewResult.Empty;
    }

    private interface ISlashCommandPreviewRule
    {
        bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result);
    }

    private sealed class NotSlashPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (parse.IsSlashLine)
                return TryReject(out result);

            result = SlashCommandPreviewResult.Empty;
            return true;
        }
    }

    private sealed class RejectedSlashPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!parse.IsRejected)
                return TryReject(out result);

            result = new(parse.RejectReason ?? "Некорректная команда.", SlashCommandPreviewKind.Error);
            return true;
        }
    }

    private sealed class AnchorPeekPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!SlashPathAliases.IsAnchorPeekCommand(parse))
                return TryReject(out result);

            result = SlashCommandPreviewRuleHelpers.BuildAnchorPeek(
                SlashPathAliases.ExtractPeekArgs(parse),
                resolveAnchor);
            return true;
        }
    }

    private sealed class EditorLineSelectPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!SlashCommandPreviewRuleHelpers.IsEditorLineSelect(parse))
                return TryReject(out result);

            result = SlashCommandPreviewRuleHelpers.BuildParametricPreview(parse.ArgsTail, "Строки", parse);
            return true;
        }
    }

    private sealed class IntercomPathPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!IntercomSlashPathBuilder.TryBuildPath(parse, out var intercomPath))
                return TryReject(out result);

            result = SlashCommandPreviewRuleHelpers.BuildIntercomPathPreview(intercomPath, parse, resolveAnchor);
            return true;
        }
    }

    private sealed class CatalogSlashPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!ChatSlashCommandCatalog.TryResolve(parse, out var descriptor))
                return TryReject(out result);

            result = SlashCommandPreviewRuleHelpers.BuildCatalogCommandPreview(descriptor.SlashPath, parse.ArgsTail);
            return true;
        }
    }

    private sealed class UnknownSlashPreviewRule : ISlashCommandPreviewRule
    {
        public bool TryEvaluate(
            ChatSlashCommandParseResult parse,
            string? bufferText,
            SlashCommandAnchorPreviewResolver? resolveAnchor,
            out SlashCommandPreviewResult result)
        {
            if (!parse.IsSlashLine)
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
    public static SlashCommandPreviewResult BuildIntercomPathPreview(
        string intercomPath,
        ChatSlashCommandParseResult parse,
        SlashCommandAnchorPreviewResolver? resolveAnchor)
    {
        if (!ChatSlashCommandCatalog.TryResolve(parse, out _))
            return new($"Нет такой команды «{intercomPath}».", SlashCommandPreviewKind.Error);

        if (SlashIntercomPreviewPolicies.TryBuild(intercomPath, parse, resolveAnchor, out var policyResult))
            return policyResult;

        if (string.Equals(intercomPath, SlashPathAliases.AnchorPeekPath, StringComparison.OrdinalIgnoreCase))
            return SlashCommandPreviewRuleHelpers.BuildAnchorPeek(SlashPathAliases.ExtractPeekArgs(parse), resolveAnchor);

        return SlashCommandPreviewRuleHelpers.BuildCatalogCommandPreview(intercomPath, parse.ArgsTail);
    }

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
        ChatSlashCommandParseResult parse)
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

        if (!ChatSlashCommandCatalog.TryResolve(parse, out _))
            return new("Нет такой команды.", SlashCommandPreviewKind.Error);

        return new(
            ParametricSegmentListParser.FormatSummary(segments, unitLabel),
            SlashCommandPreviewKind.Ok);
    }

    public static bool IsEditorLineSelect(in ChatSlashCommandParseResult parse) =>
        string.Equals(parse.Head, "editor", StringComparison.OrdinalIgnoreCase)
        && string.Equals(parse.Action, "line", StringComparison.OrdinalIgnoreCase)
        && string.Equals(parse.SubAction, "select", StringComparison.OrdinalIgnoreCase);

}
