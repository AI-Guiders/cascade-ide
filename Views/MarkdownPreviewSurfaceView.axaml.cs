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
        if (vm?.Payload is not { } payload)
        {
            host.Content = BuildPlaceholder(vm);
        }
        else
        {
            try
            {
                var anchors = new MarkdownPreviewAnchorRegistry();
                var ctx = new MarkdownPreviewRenderContext(
                    payload.SourcePath,
                    vm.TryGetWorkspaceRoot(),
                    url => vm.TryOpenPreviewLink(url, anchors),
                    anchors);
                host.Content = Renderer.Render(payload, ctx);

                var scrollLine = vm.ConsumePendingScrollLine();
                if (scrollLine is > 0)
                    anchors.ScrollToLine(scrollLine.Value);

                var scrollFragment = vm.ConsumePendingScrollFragment();
                if (!string.IsNullOrWhiteSpace(scrollFragment))
                    anchors.ScrollToFragment(scrollFragment);
            }
            catch (Exception ex)
            {
                host.Content = BuildRenderErrorPlaceholder(vm, ex);
            }
        }

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

    private static Control BuildRenderErrorPlaceholder(MarkdownPreviewSurfaceViewModel? vm, Exception ex)
    {
        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock
                {
                    Text = "Markdown preview failed to render.",
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = ex.Message,
                    Opacity = 0.85,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            }
        };
        if (!string.IsNullOrWhiteSpace(vm?.StatusText))
        {
            panel.Children.Add(new TextBlock
            {
                Text = vm!.StatusText,
                Opacity = 0.7,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = panel
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
