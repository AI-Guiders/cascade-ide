using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Build;

/// <summary>
/// Вкладка «Build output» нижней панели: текст вывода сборки и связанных операций.
/// </summary>
public partial class BuildOutputPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _buildOutput = "";
}
