using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private IInputElement? _focusBeforeCommandPalette;

    /// <summary>Сохранение и восстановление фокуса при открытии/закрытии палитры команд (UX §6).</summary>
    internal void HandleCommandPaletteOpenStateChanged(bool open)
    {
        if (open)
        {
            _focusBeforeCommandPalette = FocusManager?.GetFocusedElement();
            return;
        }

        var prev = _focusBeforeCommandPalette;
        _focusBeforeCommandPalette = null;
        Dispatcher.UIThread.Post(() =>
        {
            if (prev is Control c)
                c.Focus();
        }, DispatcherPriority.Background);
    }
}
