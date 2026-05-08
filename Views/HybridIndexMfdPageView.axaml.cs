using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CascadeIDE.Views;

public partial class HybridIndexMfdPageView : UserControl
{
    public HybridIndexMfdPageView()
    {
        InitializeComponent();
    }

    private async void OnCopyErrorClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        var text = vm.HybridIndexLastErrorText;
        if (string.IsNullOrWhiteSpace(text) || text == "—")
            return;
        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard is { } clip)
            await clip.SetTextAsync(text);
    }
}

