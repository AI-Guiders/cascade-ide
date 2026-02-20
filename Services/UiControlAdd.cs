using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Добавить контрол в дерево UI на лету (только Debug). Родитель — Panel по имени.
/// Вызывать из UI-потока. Для ide_add_control.
/// </summary>
public static class UiControlAdd
{
    /// <summary>Добавить контрол в конец Children родителя. Возвращает "OK" или сообщение об ошибке.</summary>
    public static string AddControl(Visual root, string parentName, string controlType, string? content, string? name)
    {
        if (string.IsNullOrWhiteSpace(parentName))
            return "Missing parent_name.";
        if (string.IsNullOrWhiteSpace(controlType))
            return "Missing control_type.";

        var parent = UiControlAppearance.FindControlByName(root, parentName.Trim());
        if (parent is null)
            return $"Parent not found: {parentName}.";

        if (parent is not Panel panel)
            return $"Parent must be a Panel (Grid, StackPanel, DockPanel, etc.), got {parent.GetType().Name}.";

        var control = CreateControl(controlType.Trim(), content ?? "", name?.Trim());
        if (control is null)
            return $"Unknown control_type: {controlType}. Use Button, TextBlock, or Border.";

        panel.Children.Add(control);
        return "OK";
    }

    private static Control? CreateControl(string controlType, string content, string? name)
    {
        Control? c = controlType switch
        {
            "Button" => new Button { Content = content },
            "TextBlock" => new TextBlock { Text = content },
            "Border" => new Border { Child = new TextBlock { Text = content } },
            _ => null
        };
        if (c is null)
            return null;
        if (!string.IsNullOrEmpty(name) && c is StyledElement se)
            se.Name = name;
        return c;
    }
}
