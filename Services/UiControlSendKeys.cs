using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Отправить сочетание клавиш в эффективный (под курсором) или указанный по имени контрол. Текст вида "Ctrl+Enter", "Alt+F4". Для ide_send_keys.
/// </summary>
public static class UiControlSendKeys
{
    public static string SendKeys(TopLevel topLevel, Visual root, string? controlName, string keysSpec)
    {
        if (string.IsNullOrWhiteSpace(keysSpec))
            return "Missing keys (e.g. Ctrl+Enter, Alt+F4).";

        Control? control;
        if (!string.IsNullOrWhiteSpace(controlName))
        {
            control = root is Window mw
                ? UiControlAppearance.FindControlByNameAcrossAllWindows(mw, controlName.Trim())
                : UiControlAppearance.FindControlByName(root, controlName.Trim());
            if (control is null)
                return $"Control not found: {controlName}.";
        }
        else
        {
            var over = (topLevel as IInputRoot)?.PointerOverElement;
            control = over as Control ?? FindAncestorControl(over as Visual);
            if (control is null)
                return "No control under cursor. Specify name or focus the target first.";
        }

        if (!TryParseKeys(keysSpec.Trim(), out var key, out var modifiers, out var parseError))
            return parseError ?? "Invalid keys.";

        control.Focus();
        var keyDown = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = control,
            Key = key,
            KeyModifiers = modifiers
        };
        var keyUp = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Source = control,
            Key = key,
            KeyModifiers = modifiers
        };
        control.RaiseEvent(keyDown);
        control.RaiseEvent(keyUp);
        return "OK";
    }

    private static bool TryParseKeys(string spec, out Key key, out KeyModifiers modifiers, out string? error)
    {
        modifiers = KeyModifiers.None;
        key = Key.None;
        error = "";

        var parts = spec.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            error = "Empty key spec.";
            return false;
        }
        error = null;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            var mod = parts[i].ToLowerInvariant();
            if (mod is "ctrl" or "control")
                modifiers |= KeyModifiers.Control;
            else if (mod is "alt")
                modifiers |= KeyModifiers.Alt;
            else if (mod is "shift")
                modifiers |= KeyModifiers.Shift;
            else if (mod is "meta" or "win" or "windows")
                modifiers |= KeyModifiers.Meta;
            else
            {
                error = $"Unknown modifier: {parts[i]}.";
                return false;
            }
        }

        var keyPart = parts[^1];
        var keyName = keyPart.Length == 1 ? keyPart.ToUpperInvariant() : keyPart.ToUpperInvariant().Replace(" ", "");
        if (!Enum.TryParse<Key>(keyName, true, out key))
        {
            error = $"Unknown key: {keyPart}. Use e.g. Enter, Tab, F4, A.";
            return false;
        }

        return true;
    }

    private static Control? FindAncestorControl(Visual? visual)
    {
        for (var v = visual?.GetVisualParent(); v is not null; v = v.GetVisualParent())
            if (v is Control c)
                return c;
        return null;
    }
}
