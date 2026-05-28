using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace CascadeIDE.Views;

internal static class FeatureDocsPickDialog
{
    public static async Task<string?> ShowAsync(Window? owner, string title, IReadOnlyList<string> docPaths)
    {
        if (owner is null || docPaths.Count == 0)
            return null;

        var dialog = new Window
        {
            Title = title,
            Width = 560,
            Height = 380,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var list = new ListBox
        {
            ItemsSource = docPaths,
            SelectionMode = SelectionMode.Single,
            MinHeight = 220,
        };

        string? result = null;

        var open = new Button { Content = "Открыть", MinWidth = 110, IsDefault = true };
        var cancel = new Button { Content = "Отмена", MinWidth = 110, IsCancel = true };

        void CloseWithSelection()
        {
            if (list.SelectedItem is string s && !string.IsNullOrWhiteSpace(s))
                result = s;
            dialog.Close();
        }

        open.Click += (_, _) => CloseWithSelection();
        cancel.Click += (_, _) => dialog.Close();
        list.DoubleTapped += (_, _) => CloseWithSelection();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Документация фичи:", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                list,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { open, cancel },
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}

