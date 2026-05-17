#nullable enable

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Перенос текста для Skia-примитивов (без Avalonia TextLayout).</summary>
internal static class SkiaTextLayout
{
    public static List<string> Wrap(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [""];

        var words = text.Replace("\r", "").Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            if (current.Length == 0)
            {
                current = word;
                continue;
            }

            if (current.Length + 1 + word.Length <= maxChars)
            {
                current += " " + word;
                continue;
            }

            lines.Add(current);
            current = word;
        }

        if (current.Length > 0)
            lines.Add(current);
        return lines;
    }
}
