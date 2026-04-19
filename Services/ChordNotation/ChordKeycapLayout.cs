namespace CascadeIDE.Services.ChordNotation;

/// <summary>Подписи внутри «кнопок-клавиш»: слова как в Windows или глифы как в macOS.</summary>
public enum ChordKeycapLabelFlavor
{
    /// <summary>Ctrl, Alt, Shift, Win.</summary>
    WindowsWords,

    /// <summary>⌃ ⌥ ⇧ ⌘.</summary>
    MacGlyphs
}

/// <summary>Один визуальный кэп (подпись внутри Border).</summary>
public sealed record ChordKeycapSegment(string Label);

/// <summary>Один шаг последовательности (например один аккорд или одна plain-клавиша) — горизонтальный ряд кэпов.</summary>
public sealed record ChordKeycapStep(IReadOnlyList<ChordKeycapSegment> Segments);

/// <summary>Последовательность шагов для <see cref="Views.ChordKeycapStrip"/> (между шагами — отступ в разметке).</summary>
public sealed record ChordKeycapSequence(IReadOnlyList<ChordKeycapStep> Steps)
{
    public static ChordKeycapSequence Empty { get; } = new(Array.Empty<ChordKeycapStep>());
}

/// <summary>Семантика → плоская раскладка для UI «как на клавиатуре».</summary>
public static class ChordKeycapLayoutBuilder
{
    public static ChordKeycapSequence Build(NormalizedKeySequence? sequence, ChordKeycapLabelFlavor flavor = ChordKeycapLabelFlavor.WindowsWords)
    {
        if (sequence is null || sequence.Steps.Count == 0)
            return ChordKeycapSequence.Empty;

        var steps = new List<ChordKeycapStep>(sequence.Steps.Count);
        foreach (var st in sequence.Steps)
            steps.Add(BuildStep(st, flavor));

        return new ChordKeycapSequence(steps);
    }

    private static ChordKeycapStep BuildStep(NormalizedSequenceStep step, ChordKeycapLabelFlavor flavor) =>
        step switch
        {
            NormalizedChordStep ch => new ChordKeycapStep(BuildChordSegments(ch.Modifiers, ch.KeySymbol, flavor)),
            NormalizedPlainKeyStep pl => new ChordKeycapStep([new ChordKeycapSegment(PlainLabel(pl.KeySymbol, flavor))]),
            _ => throw new InvalidOperationException($"Неизвестный шаг: {step.GetType().Name}")
        };

    private static IReadOnlyList<ChordKeycapSegment> BuildChordSegments(ChordModifierKeys modifiers, string keySymbol, ChordKeycapLabelFlavor flavor)
    {
        var list = new List<ChordKeycapSegment>(5);
        if (modifiers.HasFlag(ChordModifierKeys.Control))
            list.Add(new ChordKeycapSegment(ModifierLabel(ChordModifierKeys.Control, flavor)));
        if (modifiers.HasFlag(ChordModifierKeys.Alt))
            list.Add(new ChordKeycapSegment(ModifierLabel(ChordModifierKeys.Alt, flavor)));
        if (modifiers.HasFlag(ChordModifierKeys.Shift))
            list.Add(new ChordKeycapSegment(ModifierLabel(ChordModifierKeys.Shift, flavor)));
        if (modifiers.HasFlag(ChordModifierKeys.Meta))
            list.Add(new ChordKeycapSegment(ModifierLabel(ChordModifierKeys.Meta, flavor)));
        list.Add(new ChordKeycapSegment(keySymbol));
        return list;
    }

    private static string ModifierLabel(ChordModifierKeys single, ChordKeycapLabelFlavor flavor)
    {
        if (flavor == ChordKeycapLabelFlavor.MacGlyphs)
        {
            return single switch
            {
                ChordModifierKeys.Control => "\u2303",
                ChordModifierKeys.Alt => "\u2325",
                ChordModifierKeys.Shift => "\u21E7",
                ChordModifierKeys.Meta => "\u2318",
                _ => "?"
            };
        }

        return single switch
        {
            ChordModifierKeys.Control => "Ctrl",
            ChordModifierKeys.Alt => "Alt",
            ChordModifierKeys.Shift => "Shift",
            ChordModifierKeys.Meta => "Win",
            _ => "?"
        };
    }

    private static string PlainLabel(string keySymbol, ChordKeycapLabelFlavor flavor)
    {
        _ = flavor;
        return keySymbol;
    }
}
