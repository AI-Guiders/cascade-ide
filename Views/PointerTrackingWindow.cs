using Avalonia.Controls;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

/// <summary>
/// Базовое окно: подписка на указатель для MCP (Avalonia 12 не даёт <c>PointerOverElement</c> на <see cref="TopLevel"/>).
/// </summary>
public class PointerTrackingWindow : Window
{
    protected PointerTrackingWindow()
    {
        UiPointerClientPosition.Attach(this);
    }
}
