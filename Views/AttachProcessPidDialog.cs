using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CascadeIDE.Views;

/// <summary>Модальное окно ввода PID для attach (без XAML — как остальные простые диалоги MainWindow).</summary>
internal static class AttachProcessPidDialog
{
    public static async Task<int?> ShowAsync(Window owner)
    {
        var dialog = new Window
        {
            Title = "Присоединиться к процессу",
            Width = 420,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var box = new TextBox { PlaceholderText = "PID (целое число)", Margin = new Thickness(0, 0, 0, 8) };
        int? result = null;

        var ok = new Button { Content = "OK", MinWidth = 90 };
        var cancel = new Button { Content = "Отмена", MinWidth = 90 };
        ok.Click += (_, _) =>
        {
            if (int.TryParse(box.Text?.Trim(), out var pid) && pid > 0)
                result = pid;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Введите PID процесса .NET:", TextWrapping = TextWrapping.Wrap },
                box,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { ok, cancel }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
