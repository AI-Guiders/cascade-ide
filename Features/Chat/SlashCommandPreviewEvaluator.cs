#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Фасад slash-preview; правила — <see cref="SlashCommandPreviewRulePipeline"/> (ADR 0138).</summary>
internal static class SlashCommandPreviewEvaluator
{
    public static SlashCommandPreviewResult Evaluate(string? bufferText, SlashCommandAnchorPreviewResolver? resolveAnchor = null) =>
        SlashCommandPreviewRulePipeline.Evaluate(bufferText, resolveAnchor);

    public static bool TryEvaluateSummary(string? bufferText, out string? summary)
    {
        var result = Evaluate(bufferText);
        summary = result.Text;
        return result.HasText;
    }

    public static SlashCommandPreviewKind MapResolveOutcome(string? resolveOutcome)
    {
        if (string.Equals(resolveOutcome?.Trim(), IntercomAttachmentRevealPlan.OutcomeResolved, StringComparison.OrdinalIgnoreCase))
            return SlashCommandPreviewKind.Ok;

        if (string.IsNullOrWhiteSpace(resolveOutcome))
            return SlashCommandPreviewKind.Incomplete;

        return SlashCommandPreviewKind.Incomplete;
    }
}
