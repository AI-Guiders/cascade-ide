using CommunityToolkit.Mvvm.Input;
using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Features.Shell;

/// <summary>Relay: уровень безопасности автономного агента (поле на <see cref="ViewModels.MainWindowViewModel"/>).</summary>
public sealed partial class ShellChromeViewModel
{
    [RelayCommand]
    private void SetSafetyObserve() => _host.SafetyLevel = AgentSafetyLevel.Observe;

    [RelayCommand]
    private void SetSafetyConfirm() => _host.SafetyLevel = AgentSafetyLevel.Confirm;

    [RelayCommand]
    private void SetSafetyAutonomous() => _host.SafetyLevel = AgentSafetyLevel.Autonomous;
}
