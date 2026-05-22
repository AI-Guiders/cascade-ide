using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CascadeIDE.Views;

/// <summary>Диалог переименования темы Intercom.</summary>
internal static class TopicRenameDialog
{
    public static async Task<string?> ShowAsync(Window? owner, string currentTitle)
    {
        if (owner is null)
            return null;

        var dialog = new Window
        {
            Title = "Переименовать тему",
            Width = 440,
            Height = 168,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var box = new TextBox
        {
            Text = currentTitle ?? "",
            PlaceholderText = "Название темы",
            Margin = new Thickness(0, 0, 0, 8),
        };
        string? result = null;

        var ok = new Button { Content = "OK", MinWidth = 90, IsDefault = true };
        var cancel = new Button { Content = "Отмена", MinWidth = 90, IsCancel = true };
        ok.Click += (_, _) =>
        {
            var text = box.Text?.Trim() ?? "";
            if (text.Length > 0)
                result = text;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Новое название темы:",
                    TextWrapping = TextWrapping.Wrap,
                },
                box,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { ok, cancel },
                },
            },
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
