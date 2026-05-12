namespace CascadeIDE.Features.Shell.Application;

/// <summary>Канон списка префиксов подсказки палитры (ADR 0112 §4).</summary>
internal static class CommandPaletteChromeModeHints
{
    /// <summary>Сегменты вида «<c>f:</c> файл» без разделителя.</summary>
    internal static IReadOnlyList<(string Prefix, string Label)> Entries { get; } =
    [
        ("f:", "файл"),
        ("t:", "тип"),
        ("m:", "член"),
        ("x:", "текст"),
        ("c:", "melody"),
    ];

    internal static string SeparatorLineJoin => string.Join(" · ", Entries.Select(static e => $"{e.Prefix} {e.Label}"));
}
