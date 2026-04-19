using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Установить фокус на контрол по имени или на элемент под курсором. Для ide_set_focus.
/// </summary>
public static class UiControlSetFocus
{
    public static string SetFocus(TopLevel topLevel, Visual root, string? controlName)
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

        control.Focus();
        return "OK";
    }

}
