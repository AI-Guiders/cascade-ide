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
            control = UiControlAppearance.FindControlByName(root, controlName.Trim());
            if (control is null)
                return $"Control not found: {controlName}.";
        }
        else
        {
            var over = (topLevel as IInputRoot)?.PointerOverElement;
            control = over as Control ?? FindAncestorControl(over as Visual);
            if (control is null)
                return "No control under cursor. Specify name from ide_get_ui_layout.";
        }

        control.Focus();
        return "OK";
    }

    private static Control? FindAncestorControl(Visual? visual)
    {
        for (var v = visual?.GetVisualParent(); v is not null; v = v.GetVisualParent())
            if (v is Control c)
                return c;
        return null;
    }
}
