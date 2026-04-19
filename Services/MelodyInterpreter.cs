namespace CascadeIDE.Services;

/// <summary>
/// Интерпретация режима палитры <c>c:</c> (Command Melody): какие строки показать и какой индекс выделить.
/// Словарь alias → command_id — <see cref="IntentMelodyAliases"/>.
/// </summary>
public static class MelodyInterpreter
{
    public const string EmptyTailHintTitle =
        "Command Melody: введи хвост после c: (например gs, br, so). Док: docs/intent-melody-language-v1.md";

    public const string EmptyTailHintCategory = "c:";

    public const string NoMatchHintTitle = "Нет alias для этого хвоста";

    /// <summary>
    /// Построить план отображения для уже нормализованного хвоста после <c>c:</c> (см. <see cref="IntentMelodyAliases.TryGetTail"/>).
    /// </summary>
    public static MelodyPalettePlan BuildPalette(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
        {
            MelodyPaletteLine[] lines =
            [
                new MelodyPaletteHint(EmptyTailHintTitle, EmptyTailHintCategory),
                ..IntentMelodyAliases.AllPairs().Select(p => new MelodyPaletteCommand(p.Alias, p.CommandId)),
            ];
            var selected = lines.Length > 1 ? 1 : (lines.Length > 0 ? 0 : -1);
            return new MelodyPalettePlan(lines, selected);
        }

        var matches = IntentMelodyAliases.FilterByTailPrefix(tailNormalized);
        if (matches.Count == 0)
        {
            return new MelodyPalettePlan(
                [new MelodyPaletteHint(NoMatchHintTitle, EmptyTailHintCategory)],
                0);
        }

        MelodyPaletteLine[] cmdLines = [..matches.Select(m => new MelodyPaletteCommand(m.Alias, m.CommandId))];
        return new MelodyPalettePlan(cmdLines, cmdLines.Length > 0 ? 0 : -1);
    }
}

public sealed record MelodyPalettePlan(IReadOnlyList<MelodyPaletteLine> Lines, int SelectedIndex);

public abstract record MelodyPaletteLine;

public sealed record MelodyPaletteHint(string Title, string Category) : MelodyPaletteLine;

public sealed record MelodyPaletteCommand(string Alias, string CommandId) : MelodyPaletteLine;
