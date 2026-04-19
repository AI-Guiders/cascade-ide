namespace CascadeIDE.Services.ChordNotation;

/// <summary>
/// Модификаторы одновременного аккорда (семантика, без привязки к строке hotkeys / платформе).
/// </summary>
[Flags]
public enum ChordModifierKeys
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
    /// <summary>Meta / Super / Win / ⌘ — единый флаг; рендер в UI может отличаться по ОС.</summary>
    Meta = 8
}

/// <summary>
/// Нормализованная последовательность шагов: аккорды (модификаторы+клавиша) и «голые» клавиши (CascadeChord, FMS-токены).
/// </summary>
public sealed record NormalizedKeySequence(IReadOnlyList<NormalizedSequenceStep> Steps)
{
    public static NormalizedKeySequence Empty { get; } = new(Array.Empty<NormalizedSequenceStep>());
}

public abstract record NormalizedSequenceStep;

/// <summary>Одновременное нажатие: Ctrl+K, ⌘K, &lt;C-m&gt; после нормализации.</summary>
public sealed record NormalizedChordStep(ChordModifierKeys Modifiers, string KeySymbol) : NormalizedSequenceStep;

/// <summary>Шаг без модификаторов: буква аккорда, Esc, L1, …</summary>
public sealed record NormalizedPlainKeyStep(string KeySymbol) : NormalizedSequenceStep;
