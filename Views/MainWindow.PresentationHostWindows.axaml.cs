using System;
using Avalonia.Controls;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

/// <summary>Вторичные окна презентации (PFD/MFD-хосты): открытие, плейсмент, persist, автозапуск — ADR 0017.</summary>
public partial class MainWindow
{
    private MfdHostWindow? _mfdHostWindow;
    private PfdHostWindow? _pfdHostWindow;

    private static bool TryActivateSecondary(Window? window)
    {
        if (window is not { IsVisible: true })
            return false;
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
        return true;
    }

    private void CloseSecondaryHostWindow<T>(ref T? windowField, Action<ViewModels.MainWindowViewModel> setShellOpenFalse)
        where T : Window
    {
        if (windowField is null)
            return;
        if (DataContext is ViewModels.MainWindowViewModel vm)
            setShellOpenFalse(vm);
        try
        {
            windowField.Close();
        }
        catch
        {
            // окно уже уничтожено
        }

        windowField = null;
    }

    private void ToggleMfdHostWindow()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (TryActivateSecondary(_mfdHostWindow))
            return;

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
        PresentationHostWindowPlacement.PresentationHostWindowBounds? savedBounds =
            vm.TryGetSavedMfdHostWindowBounds(out var b) ? b : null;
        PresentationHostWindowPlacement.PlaceOrRestore(
            this,
            w,
            savedBounds,
            vm.MfdHostPresentationScreenIndex,
            vm.MaximizePresentationHostWindowsOnDedicatedScreens);
        vm.SetMfdHostWindowShellOpen(true);
        _mfdHostWindow = w;
        w.Show(this);
    }

    private void TogglePfdHostWindow()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (TryActivateSecondary(_pfdHostWindow))
            return;

        var w = new PfdHostWindow { DataContext = vm };
        w.Closing += (_, _) =>
        {
            vm.PersistPfdHostWindowBounds(w.Position.X, w.Position.Y, w.Width, w.Height);
        };
        w.Closed += (_, _) =>
        {
            if (ReferenceEquals(_pfdHostWindow, w))
                _pfdHostWindow = null;
        };
        PresentationHostWindowPlacement.PresentationHostWindowBounds? savedBounds =
            vm.TryGetSavedPfdHostWindowBounds(out var pb) ? pb : null;
        PresentationHostWindowPlacement.PlaceOrRestore(
            this,
            w,
            savedBounds,
            vm.PfdHostPresentationScreenIndex,
            vm.MaximizePresentationHostWindowsOnDedicatedScreens);
        vm.SetPfdHostWindowShellOpen(true);
        _pfdHostWindow = w;
        w.Show(this);
    }

    /// <summary>Минимум мониторов для автозапуска MFD-хоста (индекс из пресета или хотя бы два экрана).</summary>
    private static int MinScreensForMfdHostStartup(ViewModels.MainWindowViewModel vm) =>
        vm.MfdHostPresentationScreenIndex is int idx ? idx + 1 : 2;

    /// <summary>Минимум мониторов для автозапуска PFD-хоста при тройном пресете (учитываются индексы P и M).</summary>
    private static int MinScreensForPfdHostStartup(ViewModels.MainWindowViewModel vm)
    {
        var min = 1;
        if (vm.PfdHostPresentationScreenIndex is int p)
            min = Math.Max(min, p + 1);
        if (vm.MfdHostPresentationScreenIndex is int m)
            min = Math.Max(min, m + 1);
        return min;
    }

    private void TryOpenMfdHostWindowOnStartup()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        if (!vm.PresentationRequestsMfdHostWindow || !vm.OpenMfdHostWindowOnStartup)
            return;
        if (Screens.All.Count < MinScreensForMfdHostStartup(vm))
            return;
        ToggleMfdHostWindow();
    }

    private void TryOpenPfdHostWindowOnStartup()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        if (!vm.PresentationRequestsPfdHostWindow || !vm.OpenPfdHostWindowOnStartup)
            return;
        if (Screens.All.Count < MinScreensForPfdHostStartup(vm))
            return;
        TogglePfdHostWindow();
    }

    private void CloseMfdHostWindowIfOpen() =>
        CloseSecondaryHostWindow(ref _mfdHostWindow, static vm => vm.SetMfdHostWindowShellOpen(false));

    private void ClosePfdHostWindowIfOpen() =>
        CloseSecondaryHostWindow(ref _pfdHostWindow, static vm => vm.SetPfdHostWindowShellOpen(false));
}
