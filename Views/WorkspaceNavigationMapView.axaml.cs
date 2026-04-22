using Avalonia.Controls;
using Avalonia.VisualTree;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class WorkspaceNavigationMapView : UserControl
{
    public WorkspaceNavigationMapView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (CodeNavigationMapMiniMap is null)
            return;
        CodeNavigationMapMiniMap.SizeChanged += OnCodeNavigationMapMiniMapSizeChanged;
        PushViewportWidth(CodeNavigationMapMiniMap);
    }

    private void OnCodeNavigationMapMiniMapSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is Control c)
            PushViewportWidth(c);
    }

    private void PushViewportWidth(Control miniMap)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var w = miniMap.Bounds.Width;
        if (w <= 0)
            return;
        vm.NotifyCodeNavigationMapGraphViewportWidthChanged(w);
    }
}
