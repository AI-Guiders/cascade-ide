using CommunityToolkit.Mvvm.ComponentModel;

using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.ViewModels;

/// <summary>Элемент очереди задач в Power mode (уровень безопасности и состояние).</summary>
public sealed partial class PowerTaskQueueItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    /// <summary>safety.observe / safety.confirm / safety.autonomous</summary>
    [ObservableProperty]
    private string _safetyLevel = AgentSafetyLevel.Confirm;

    /// <summary>Pending, Queued, Paused, Running и т.д.</summary>
    [ObservableProperty]
    private string _state = "Pending";

    public string DisplayLine => $"{Title}  [{SafetyLevel}]  {State}";
}
