using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Mfd: <see cref="SolutionExplorerView"/> + <see cref="SecondaryShellView"/> (сетка как в <see cref="MainWindow"/>), ADR 0017 п. 8.</summary>
public partial class MfdHostWindow : Window
{
    public MfdHostWindow()
    {
        InitializeComponent();
    }
}
