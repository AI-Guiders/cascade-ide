using System.Text;

namespace CascadeIDE.Services.ChordNotation;

/// <summary>Фабрика и общая логика порядка модификаторов (Control → Alt → Shift → Meta).</summary>
public static class ChordNotationRenderer
{
    /// <summary>Пробел между шагами последовательности (как во вводе KeyGesture).</summary>
    public const char StepSeparator = ' ';

    public static IChordNotationRenderer Windows { get; } = new WindowsChordNotationRenderer();

    public static IChordNotationRenderer MacSymbols { get; } = new MacSymbolChordNotationRenderer();

    /// <summary>Третий вид: сегменты для визуальных «клавиш» (<see cref="ChordKeycapStrip"/>).</summary>
    public static ChordKeycapSequence BuildKeycapLayout(NormalizedKeySequence? sequence, ChordKeycapLabelFlavor flavor = ChordKeycapLabelFlavor.WindowsWords) =>
        ChordKeycapLayoutBuilder.Build(sequence, flavor);

    /// <summary>Один аккорд (без последовательности шагов) — для подписи к одному жесту.</summary>
    public static string FormatChord(ChordModifierKeys modifiers, string keySymbol, ChordNotationRenderFlavor flavor) =>
        flavor == ChordNotationRenderFlavor.MacSymbols
            ? MacSymbolChordNotationRenderer.RenderChord(modifiers, keySymbol)
            : WindowsChordNotationRenderer.RenderChord(modifiers, keySymbol);
}

public enum ChordNotationRenderFlavor
{
    /// <summary>Ctrl+, Alt+, Shift+, Win+ (Meta).</summary>
    Windows,

    /// <summary>⌃ ⌥ ⇧ ⌘ и ключ (Meta → ⌘).</summary>
    MacSymbols
}

/// <summary>Ctrl+, Alt+, Shift+, Win+ между частями.</summary>
public sealed class WindowsChordNotationRenderer : IChordNotationRenderer
{
    public string Render(NormalizedKeySequence? sequence)
    {
        if (sequence is null || sequence.Steps.Count == 0)
            return "";

        var sb = new StringBuilder();
        for (var i = 0; i < sequence.Steps.Count; i++)
        {
            if (i > 0)
                sb.Append(ChordNotationRenderer.StepSeparator);
            switch (sequence.Steps[i])
            {
                case NormalizedChordStep ch:
                    sb.Append(RenderChord(ch.Modifiers, ch.KeySymbol));
                    break;
                case NormalizedPlainKeyStep pl:
                    sb.Append(pl.KeySymbol);
                    break;
            }
        }

        return sb.ToString();
    }

    internal static string RenderChord(ChordModifierKeys modifiers, string keySymbol)
    {
        var sb = new StringBuilder(24);
        AppendWindowsModifiers(sb, modifiers);
        if (sb.Length > 0)
            sb.Append('+');
        sb.Append(keySymbol);
        return sb.ToString();
    }

    private static void AppendWindowsModifiers(StringBuilder sb, ChordModifierKeys m)
    {
        void Part(string label)
        {
            if (sb.Length > 0)
                sb.Append('+');
            sb.Append(label);
        }

        if (m.HasFlag(ChordModifierKeys.Control))
            Part("Ctrl");
        if (m.HasFlag(ChordModifierKeys.Alt))
            Part("Alt");
        if (m.HasFlag(ChordModifierKeys.Shift))
            Part("Shift");
        if (m.HasFlag(ChordModifierKeys.Meta))
            Part("Win");
    }
}

/// <summary>⌃⌥⇧⌘ без «+» между глифами; ключ в конце.</summary>
public sealed class MacSymbolChordNotationRenderer : IChordNotationRenderer
{
    public string Render(NormalizedKeySequence? sequence)
    {
        if (sequence is null || sequence.Steps.Count == 0)
            return "";

        var sb = new StringBuilder();
        for (var i = 0; i < sequence.Steps.Count; i++)
        {
            if (i > 0)
                sb.Append(ChordNotationRenderer.StepSeparator);
            switch (sequence.Steps[i])
            {
                case NormalizedChordStep ch:
                    sb.Append(RenderChord(ch.Modifiers, ch.KeySymbol));
                    break;
                case NormalizedPlainKeyStep pl:
                    sb.Append(pl.KeySymbol);
                    break;
            }
        }

        return sb.ToString();
    }

    internal static string RenderChord(ChordModifierKeys modifiers, string keySymbol)
    {
        var sb = new StringBuilder(16);
        if (modifiers.HasFlag(ChordModifierKeys.Control))
            sb.Append('\u2303'); // ⌃
        if (modifiers.HasFlag(ChordModifierKeys.Alt))
            sb.Append('\u2325'); // ⌥
        if (modifiers.HasFlag(ChordModifierKeys.Shift))
            sb.Append('\u21E7'); // ⇧
        if (modifiers.HasFlag(ChordModifierKeys.Meta))
            sb.Append('\u2318'); // ⌘
        sb.Append(keySymbol);
        return sb.ToString();
    }
}
