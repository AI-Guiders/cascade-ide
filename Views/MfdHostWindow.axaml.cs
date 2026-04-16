using Avalonia.Controls;
using System.ComponentModel;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Mfd: только <see cref="SecondaryShellView"/> (вторичный контур). Дерево и прочий UI — контент страниц внутри shell, не дублирование колонки главного окна в этом хосте — ADR 0017 п. 8.</summary>
public partial class MfdHostWindow : Window
{
    private INotifyPropertyChanged? _boundVm;
    private CockpitSkiaSceneRenderer? _renderer;

    public MfdHostWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += (_, _) =>
        {
            if (_boundVm is not null)
                _boundVm.PropertyChanged -= OnVmPropertyChanged;
            _boundVm = null;
        };
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundVm is not null)
            _boundVm.PropertyChanged -= OnVmPropertyChanged;

        _boundVm = DataContext as INotifyPropertyChanged;
        if (_boundVm is not null)
            _boundVm.PropertyChanged += OnVmPropertyChanged;

        if (this.FindControl<SkiaHost>("MfdHostSkiaHost") is { } host)
        {
            _renderer ??= new CockpitSkiaSceneRenderer(
                () => DataContext as ViewModels.MainWindowViewModel,
                SkiaHostSurface.MfdHostWindow);
            host.Renderer = _renderer;
            host.InvalidateVisual();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!MainWindow.IsSkiaHostRelatedProperty(e.PropertyName))
            return;

        if (this.FindControl<SkiaHost>("MfdHostSkiaHost") is { } host)
            host.InvalidateVisual();
    }
}
