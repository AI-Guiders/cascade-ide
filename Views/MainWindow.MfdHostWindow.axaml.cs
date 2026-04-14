using Avalonia.Controls;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private MfdHostWindow? _mfdHostWindow;

    private void ToggleMfdHostWindow()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (_mfdHostWindow is { IsVisible: true })
        {
            if (_mfdHostWindow.WindowState == WindowState.Minimized)
                _mfdHostWindow.WindowState = WindowState.Normal;
            _mfdHostWindow.Activate();
            return;
        }

        var w = new MfdHostWindow { DataContext = vm };
        w.Closing += (_, _) =>
        {
            vm.PersistMfdHostWindowBounds(w.Position.X, w.Position.Y, w.Width, w.Height);
        };
        w.Closed += (_, _) =>
        {
            if (ReferenceEquals(_mfdHostWindow, w))
            {
                _mfdHostWindow = null;
                // Закрытие второго TopLevel не должно менять раскладку main-window:
                // сохраняем suppress-флаг MFD-колонки до явного смены пресета/режима.
            }
        };
        Services.MfdHostWindowPlacement.MfdHostWindowBounds? savedBounds =
            vm.TryGetSavedMfdHostWindowBounds(out var b) ? b : null;
        Services.MfdHostWindowPlacement.PlaceOrRestore(this, w, savedBounds, vm.MfdHostPresentationScreenIndex);
        vm.SetMfdHostWindowShellOpen(true);
        _mfdHostWindow = w;
        w.Show(this);
    }

    private void TryOpenMfdHostWindowOnStartup()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        if (!vm.PresentationRequestsMfdHostWindow || !vm.OpenMfdHostWindowOnStartup)
            return;
        var minScreens = vm.MfdHostPresentationScreenIndex is int idx ? idx + 1 : 2;
        if (Screens.All.Count < minScreens)
            return;
        ToggleMfdHostWindow();
    }

    private void CloseMfdHostWindowIfOpen()
    {
        if (_mfdHostWindow is null)
            return;
        if (DataContext is ViewModels.MainWindowViewModel vm)
            vm.SetMfdHostWindowShellOpen(false);
        try
        {
            _mfdHostWindow.Close();
        }
        catch
        {
            // окно уже уничтожено
        }

        _mfdHostWindow = null;
    }
}
