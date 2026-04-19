using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Один пункт чеклиста плана в режиме Focus.</summary>
public partial class FocusPlanItemViewModel : ObservableObject
{
    [ObservableProperty] private string _text = "";

    [ObservableProperty] private bool _isDone;
}
