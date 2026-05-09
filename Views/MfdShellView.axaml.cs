#nullable enable
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CascadeIDE.Views;

/// <summary>
/// Вторичный контур колонки MFD: полоса EICAS/IDE Health + хост <see cref="MfdShellPageStack"/> (страницы терминала/сборки/Git/… без TabControl главного окна).
/// Элемент разметки <c>Border#MfdContourStackHost</c> — граница и фон области стека; ключ в <c>layout_regions</c> снимка темы / MCP.
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
