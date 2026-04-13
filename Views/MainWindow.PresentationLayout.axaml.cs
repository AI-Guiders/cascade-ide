using Avalonia.Controls;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    /// <summary>Применяет строку колонок из <see cref="ViewModels.MainWindowViewModel.MainGridColumnDefinitions"/> (веса из <c>presentation</c>, ADR 0017).</summary>
    private void ApplyMainGridColumnDefinitions(ViewModels.MainWindowViewModel vm)
    {
        try
        {
            MainGrid.ColumnDefinitions = ColumnDefinitions.Parse(vm.MainGridColumnDefinitions);
        }
        catch
        {
            MainGrid.ColumnDefinitions = ColumnDefinitions.Parse(PresentationMainGridColumnDefinitions.Default);
        }
    }
}
