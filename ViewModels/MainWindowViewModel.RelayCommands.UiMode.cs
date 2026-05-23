using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Relay: уровень безопасности (остаётся на MWVM — autonomous stripe).</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private void SetSafetyL1() => SafetyLevel = "L1";

    [RelayCommand]
    private void SetSafetyL2() => SafetyLevel = "L2";

    [RelayCommand]
    private void SetSafetyL3() => SafetyLevel = "L3";
}
