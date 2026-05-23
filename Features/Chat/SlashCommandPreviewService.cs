#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Разрешение якоря для slash-preview (например /anchor peek).</summary>
public delegate bool SlashCommandAnchorPreviewResolver(string argsTail, out SlashCommandPreviewResult result);

/// <summary>
/// Единая точка slash-preview для TCI (CCL и composer). ADR 0138, playbook-tci-v1.
/// </summary>
public sealed class SlashCommandPreviewService
{
    private readonly SlashCommandAnchorPreviewResolver? _resolveAnchor;

    public SlashCommandPreviewService(SlashCommandAnchorPreviewResolver? resolveAnchor = null) =>
        _resolveAnchor = resolveAnchor;

    /// <summary>Валидация одной slash-строки (буфер CCL или извлечённая линия composer).</summary>
    public SlashCommandPreviewResult Evaluate(string? slashBuffer) =>
        SlashCommandPreviewEvaluator.Evaluate(slashBuffer, _resolveAnchor);

    /// <summary>Slash-строка на линии каретки в многострочном composer.</summary>
    public SlashCommandPreviewResult EvaluateComposerAtCaret(string? chatInput, int caretIndex)
    {
        if (!ChatSlashAutocomplete.TryGetSlashLineAtCaret(chatInput, caretIndex, out var slashLine))
            return SlashCommandPreviewResult.Empty;

        return Evaluate(slashLine);
    }

    public static SlashCommandPreviewKind MapResolveOutcome(string? resolveOutcome) =>
        SlashCommandPreviewEvaluator.MapResolveOutcome(resolveOutcome);
}
