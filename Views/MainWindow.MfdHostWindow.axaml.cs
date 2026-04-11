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
        w.Closed += (_, _) =>
        {
            if (ReferenceEquals(_mfdHostWindow, w))
            {
                _mfdHostWindow = null;
                vm.SetMfdHostWindowShellOpen(false);
            }
        };
        Services.MfdHostWindowPlacement.PlaceNearMain(this, w);
        vm.SetMfdHostWindowShellOpen(true);
        _mfdHostWindow = w;
        w.Show(this);
    }

    private void TryOpenMfdHostWindowOnStartup()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        if (!vm.PresentationRequestsDedicatedMfdSecondScreen || !vm.OpenMfdHostWindowOnStartup)
            return;
        if (Screens.All.Count < 2)
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
