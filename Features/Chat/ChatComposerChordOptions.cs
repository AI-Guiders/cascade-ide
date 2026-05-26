namespace CascadeIDE.Features.Chat;

/// <summary>Три допустимых сочетания для отправки сообщения или переноса строки в composer (Skia).</summary>
public static class ChatComposerChordOptions
{
    public static readonly string[] Ordered = ["Enter", "Ctrl+Enter", "Shift+Enter"];

    /// <summary>Дефолт «как в мессенджерах»: не совпадает с выбранным send chord.</summary>
    public static string ComplementaryChord(string send)
    {
        if (string.Equals(send, "Enter", StringComparison.Ordinal))
            return "Ctrl+Enter";
        if (string.Equals(send, "Ctrl+Enter", StringComparison.Ordinal))
            return "Enter";
        if (string.Equals(send, "Shift+Enter", StringComparison.Ordinal))
            return "Ctrl+Enter";
        return "Ctrl+Enter";
    }

    public static bool IsDefined(string chord) =>
        Ordered.Any(o => string.Equals(o, chord, StringComparison.Ordinal));
}
