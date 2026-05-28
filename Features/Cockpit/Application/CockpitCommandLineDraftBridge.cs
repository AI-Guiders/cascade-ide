#nullable enable

using CascadeIDE.Features.Chat;

namespace CascadeIDE.Features.Cockpit.Application;

/// <summary>Единая синхронизация буфера CCL между view и <see cref="ChatPanelViewModel"/>.</summary>
public static class CockpitCommandLineDraftBridge
{
    public static void ApplyDraftFromView(ChatPanelViewModel panel, string? text, int caretIndex)
    {
        var normalized = string.IsNullOrWhiteSpace(text) ? "/" : text;
        var caret = Math.Clamp(caretIndex, 0, normalized.Length);
        panel.CockpitCommandLineCaretIndex = caret;
        if (!string.Equals(panel.CockpitCommandLineText, normalized, StringComparison.Ordinal))
            panel.CockpitCommandLineText = normalized;
        else
            panel.RefreshCockpitCommandLineAutocomplete(normalized, caret);
    }

    public static void ApplyDraftFromViewModel(ChatPanelViewModel panel, Action<string> setText, Action<int> setCaret)
    {
        var text = panel.CockpitCommandLineText;
        setText(text);
        setCaret(Math.Clamp(panel.CockpitCommandLineCaretIndex, 0, text.Length));
    }
}
