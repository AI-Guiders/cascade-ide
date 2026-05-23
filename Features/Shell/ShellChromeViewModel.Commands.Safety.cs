using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Shell;

/// <summary>Relay: уровень безопасности автономного агента (поле на <see cref="ViewModels.MainWindowViewModel"/>).</summary>
public sealed partial class ShellChromeViewModel
{
    [RelayCommand]
    private void SetSafetyL1() => _host.SafetyLevel = "L1";

    [RelayCommand]
    private void SetSafetyL2() => _host.SafetyLevel = "L2";

    [RelayCommand]
    private void SetSafetyL3() => _host.SafetyLevel = "L3";
}
