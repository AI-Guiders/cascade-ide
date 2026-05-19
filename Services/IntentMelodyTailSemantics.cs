using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CascadeIDE.Models.Editor;

namespace CascadeIDE.Services;

/// <summary>Разбор минимальных полей метанотации ADR в <see cref="MelodyRootEntry.TailSignature"/> и согласованность с <see cref="TailWireKind"/>.</summary>
internal static class IntentMelodyTailSemantics
{
    /// <summary>Номера строк в параметрике и в args команд — 1-based (как в UI редактора). См. <see cref="LineNumber.MinimumOneBasedInclusive"/>.</summary>
    internal const int MinEditorLineNumber = LineNumber.MinimumOneBasedInclusive;

    private static readonly Regex IntSlots = new(
        @"<[^>]+?:int>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Номер строки (1-based в пользовательском вводе); в TOML — <c>:ln</c> или <c>:linenumber</c>.</summary>
    private static readonly Regex LineNumberSlots = new(
        @"<[^>]+?:(?:ln|linenumber)>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static int CountIntSlots(string? tailSignature) =>
        string.IsNullOrEmpty(tailSignature) ? 0 : IntSlots.Count(tailSignature);

    internal static int CountLineNumberSlots(string? tailSignature) =>
        string.IsNullOrEmpty(tailSignature) ? 0 : LineNumberSlots.Count(tailSignature);

    /// <summary>Слоты цепочки «два числа в хвосте» (разделители из wire): <c>:int</c> и/или <c>:ln</c>.</summary>
    internal static int CountDelimitedNumericSlots(string? tailSignature) =>
        CountIntSlots(tailSignature) + CountLineNumberSlots(tailSignature);

    internal static bool HasUrlSlot([NotNullWhen(true)] string? tailSignature)
    {
        if (string.IsNullOrEmpty(tailSignature))
            return false;
        return tailSignature.Contains("<url:url>", StringComparison.OrdinalIgnoreCase)
               || tailSignature.Contains(":url>", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool HasBracketCodeRefSlot([NotNullWhen(true)] string? tailSignature)
    {
        if (string.IsNullOrEmpty(tailSignature))
            return false;
        return tailSignature.Contains("<code_ref:bracket>", StringComparison.OrdinalIgnoreCase)
               || tailSignature.Contains(":bracket>", StringComparison.OrdinalIgnoreCase);
    }

    internal static void ValidateMelodyAgainstWireClass(in MelodyRootEntry e, in TailWireClassEntry wire)
    {
        var numericSlots = CountDelimitedNumericSlots(e.TailSignature);
        var url = HasUrlSlot(e.TailSignature);
        var bracket = HasBracketCodeRefSlot(e.TailSignature);
        if (numericSlots > 0 && (url || bracket))
        {
            throw new InvalidOperationException(
                $"{IntentMelodyAliases.BundledRelativePath}: [[melody_root]] slug '{e.Slug}' — tail_signature смешивает url/bracket и числовые слоты (int/ln), не поддерживается.");
        }

        switch (wire.Kind)
        {
            case TailWireKind.SingleRemainder when (!url && !bracket) || numericSlots != 0:
                throw new InvalidOperationException(
                    $"{IntentMelodyAliases.BundledRelativePath}: wire_class '{wire.Id}' ({nameof(TailWireKind.SingleRemainder)}) несогласован с tail_signature корня '{e.Slug}'.");
            case TailWireKind.DelimitedSlots when url || numericSlots < 2:
                throw new InvalidOperationException(
                    $"{IntentMelodyAliases.BundledRelativePath}: wire_class '{wire.Id}' ({nameof(TailWireKind.DelimitedSlots)}) ожидает ≥ двух слотов <:int> или <:ln>; корень '{e.Slug}'.");
        }
    }
}
