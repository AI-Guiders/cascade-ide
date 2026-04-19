using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Клик по контролу (Button — RaiseEvent Click; иначе — фокус и пробуем поднять клик). Вызывать из UI-потока. Для ide_click_control.
/// </summary>
public static class UiControlClick
{
    public static string Click(TopLevel topLevel, Visual root, string? controlName)
    {
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
            control = UiPointerClientPosition.TryGetControlUnderPointer(topLevel);
            if (control is null)
                return "No control under cursor. Specify name from ide_get_ui_layout.";
        }

        if (control is Button btn)
        {
            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            return "OK";
        }

        return $"Control is not a Button (type: {control.GetType().Name}). Only Button click is supported.";
    }

}
