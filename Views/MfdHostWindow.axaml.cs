using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Mfd: полный <see cref="SecondaryShellView"/> (все страницы), как колонка Mfd в <see cref="MainWindow"/> — ADR 0017 п. 8; отдельного «только чат» нет.</summary>
public partial class MfdHostWindow : Window
{
    public MfdHostWindow()
    {
        InitializeComponent();
    }
}
