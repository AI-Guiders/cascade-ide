using Avalonia;
using Avalonia.Controls;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private AuxiliaryWorkspaceWindow? _auxiliaryWorkspaceWindow;

    private void ToggleAuxiliaryWorkspaceWindow()
    {
        if (_auxiliaryWorkspaceWindow is { IsVisible: true })
        {
            if (_auxiliaryWorkspaceWindow.WindowState == WindowState.Minimized)
                _auxiliaryWorkspaceWindow.WindowState = WindowState.Normal;
            _auxiliaryWorkspaceWindow.Activate();
            return;
        }

        var w = new AuxiliaryWorkspaceWindow();
        w.Closed += (_, _) =>
        {
            if (ReferenceEquals(_auxiliaryWorkspaceWindow, w))
                _auxiliaryWorkspaceWindow = null;
        };
        w.Position = new PixelPoint(Position.X + 48, Position.Y + 48);
        _auxiliaryWorkspaceWindow = w;
        w.Show(this);
    }

    private void CloseAuxiliaryWorkspaceWindowIfOpen()
    {
        if (_auxiliaryWorkspaceWindow is null)
            return;
        try
        {
            _auxiliaryWorkspaceWindow.Close();
        }
        catch
        {
            // окно уже уничтожено
        }

        _auxiliaryWorkspaceWindow = null;
    }
}
