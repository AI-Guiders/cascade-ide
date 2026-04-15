using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Mfd: только <see cref="SecondaryShellView"/> (вторичный контур). Дерево и прочий UI — контент страниц внутри shell, не дублирование колонки главного окна в этом хосте — ADR 0017 п. 8.</summary>
public partial class MfdHostWindow : Window
{
    public MfdHostWindow()
    {
        InitializeComponent();
    }
}
