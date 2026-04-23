#nullable enable
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CascadeIDE.Views;

/// <summary>
/// Вторичный контур оболочки: полоса EICAS + нижняя панель с <see cref="MfdShellPageStack"/> (набор страниц Mfd, оверлей без TabControl).
/// Имя Border#BottomPanelShell на корне сохранено для снимков UI / MCP.
/// </summary>
public partial class MfdShellView : UserControl
{
    private bool _xamlInitialized;

    public MfdShellView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        if (_xamlInitialized)
            return;

        _xamlInitialized = true;
        AvaloniaXamlLoader.Load(this);
    }
}
