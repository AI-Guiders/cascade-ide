using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Установить текст в контрол, поддерживающий ввод (TextBox и т.п.). Вызывать из UI-потока. Для ide_set_control_text.
/// </summary>
public static class UiControlSetText
{
    public static string SetText(Visual root, string controlName, string text)
    {
        if (string.IsNullOrWhiteSpace(controlName))
            return "Missing control name.";

        var control = root is Window mw
            ? UiControlAppearance.FindControlByNameAcrossAllWindows(mw, controlName.Trim())
            : UiControlAppearance.FindControlByName(root, controlName.Trim());
        if (control is null)
            return $"Control not found: {controlName}.";

        if (control is TextBox tbx)
        {
            tbx.Text = text ?? "";
            return "OK";
        }

        var contentProp = control.GetType().GetProperty("Content");
        if (contentProp is not null && contentProp.CanWrite)
        {
            contentProp.SetValue(control, text ?? "");
            return "OK";
        }

        return $"Control does not support text input (not TextBox and no writable Content): {control.GetType().Name}.";
    }
}
