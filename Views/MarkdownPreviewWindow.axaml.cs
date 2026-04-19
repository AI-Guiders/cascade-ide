using System.ComponentModel;
using Avalonia.Controls;

namespace CascadeIDE.Views;

public partial class MarkdownPreviewWindow : Avalonia.Controls.Window
{
    private ViewModels.MarkdownPreviewWindowViewModel? _vm;
    private PropertyChangedEventHandler? _vmHandler;
    private Markdown.Avalonia.MarkdownScrollViewer? _viewer;

    public MarkdownPreviewWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_vm is not null && _vmHandler is not null)
            _vm.PropertyChanged -= _vmHandler;

        _vm = DataContext as ViewModels.MarkdownPreviewWindowViewModel;
        if (_vm is null)
            return;

        _vmHandler = (_, args) =>
        {
            if (args.PropertyName is nameof(ViewModels.MarkdownPreviewWindowViewModel.Markdown))
                SyncMarkdownFromViewModel();
        };
        _vm.PropertyChanged += _vmHandler;
        SyncMarkdownFromViewModel();
    }

    private void SyncMarkdownFromViewModel()
    {
        if (_vm is null)
            return;

        var viewer = EnsureViewer();
        if (viewer is null)
            return;

        viewer.Markdown = _vm.Markdown ?? "";
    }

    private Markdown.Avalonia.MarkdownScrollViewer? EnsureViewer()
    {
        if (_viewer is not null)
            return _viewer;

        if (this.FindControl<ContentControl>("PreviewHost") is not { } host)
            return null;

        try
        {
            _viewer = new Markdown.Avalonia.MarkdownScrollViewer();
            host.Content = _viewer;
            return _viewer;
        }
        catch
        {
            host.Content ??= new TextBlock
            {
                Text = "Markdown preview unavailable in this build.",
                Opacity = 0.7,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
            return null;
        }
    }
}
