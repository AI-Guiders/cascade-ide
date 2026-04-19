using System.ComponentModel;
using Avalonia.Controls;
using CascadeIDE.Services.MarkdownPreview;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class MarkdownPreviewSurfaceView : UserControl
{
    private static readonly IMarkdownPreviewRenderer Renderer = new MarkdigMarkdownPreviewRenderer();
    private INotifyPropertyChanged? _boundVm;

    public MarkdownPreviewSurfaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private MarkdownPreviewSurfaceViewModel? ViewModel => DataContext as MarkdownPreviewSurfaceViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundVm is not null)
            _boundVm.PropertyChanged -= OnVmPropertyChanged;

        _boundVm = DataContext as INotifyPropertyChanged;
        if (_boundVm is not null)
            _boundVm.PropertyChanged += OnVmPropertyChanged;

        UpdateSurface();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarkdownPreviewSurfaceViewModel.Payload)
            or nameof(MarkdownPreviewSurfaceViewModel.StatusText)
            or nameof(MarkdownPreviewSurfaceViewModel.ErrorMessage)
            or nameof(MarkdownPreviewSurfaceViewModel.IsBusy))
        {
            UpdateSurface();
        }
    }

    private void UpdateSurface()
    {
        if (this.FindControl<ContentControl>("PreviewHost") is not { } host)
            return;

        var vm = ViewModel;
        host.Content = vm?.Payload is { } payload
            ? Renderer.Render(payload)
            : BuildPlaceholder(vm);

        if (this.FindControl<Border>("PreviewStatusBanner") is not { } banner
            || this.FindControl<TextBlock>("PreviewStatusText") is not { } text)
        {
            return;
        }

        var message = BuildStatusMessage(vm);
        banner.IsVisible = !string.IsNullOrWhiteSpace(message);
        text.Text = message ?? "";
    }

    private static Control BuildPlaceholder(MarkdownPreviewSurfaceViewModel? vm)
    {
        return new Border
        {
            Padding = new Avalonia.Thickness(16),
            Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(vm?.StatusText)
                    ? "Markdown preview unavailable."
                    : vm!.StatusText,
                Opacity = 0.8,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
    }

    private static string? BuildStatusMessage(MarkdownPreviewSurfaceViewModel? vm)
    {
        if (vm is null)
            return null;

        var parts = new List<string>();
        if (vm.IsBusy)
            parts.Add("Refreshing preview...");
        if (!string.IsNullOrWhiteSpace(vm.ErrorMessage))
            parts.Add(vm.ErrorMessage);
        if (!string.IsNullOrWhiteSpace(vm.StatusText))
            parts.Add(vm.StatusText);

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }
}
