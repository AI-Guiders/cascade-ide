using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Элемент очереди задач в Power mode (уровень безопасности и состояние).</summary>
public sealed partial class PowerTaskQueueItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    /// <summary>L1 / L2 / L3</summary>
    [ObservableProperty]
    private string _safetyLevel = "L2";

    /// <summary>Pending, Queued, Paused, Running и т.д.</summary>
    [ObservableProperty]
    private string _state = "Pending";

    public string DisplayLine => $"{Title}  [{SafetyLevel}]  {State}";
}
