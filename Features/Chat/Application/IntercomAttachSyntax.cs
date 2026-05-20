using CascadeIDE.Contracts;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat.Application;

/// <summary>Детектор attach-синтаксиса в тексте сообщения (⟦a:…⟧, [F:…]). ADR 0134.</summary>
[ComputingUnit]
public static class IntercomAttachSyntax
{
    public static bool HasWireOrBracketSyntax(string? text) =>
        !string.IsNullOrEmpty(text)
        && (text.Contains('\u27E6', StringComparison.Ordinal)
            || IntercomAttachmentMarkers.TryExtractBracketSpans(text, out _));
}
