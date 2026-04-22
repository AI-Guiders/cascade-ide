using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Сплиттеры рабочей области (MainGrid, обозреватель решения, Git и т.д.): режим «взлёт» — блокировка перетаскивания.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Когда true — IN AIR, границы зафиксированы; false — ON GND, сплиттеры можно двигать.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceSplitterLockButtonText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceSplitterLockButtonToolTip))]
    [NotifyPropertyChangedFor(nameof(WorkspaceSplittersUnlocked))]
    private bool _workspaceSplittersLocked;

    /// <summary>Для привязки <c>IsEnabled</c> сплиттеров (в т.ч. из вложенного DataContext через <c>ElementName</c>).</summary>
    public bool WorkspaceSplittersUnlocked => !WorkspaceSplittersLocked;

    /// <summary>Краткая подпись для доступности (лампа: ON GND / IN AIR; мелодия tol).</summary>
    public string WorkspaceSplitterLockButtonText => WorkspaceSplittersLocked ? "В воздухе" : "На земле";

    public string WorkspaceSplitterLockButtonToolTip =>
        WorkspaceSplittersLocked
            ? "Сейчас: IN AIR — сплиттеры зафиксированы. Клик: ON GND (снова можно двигать PFD, редактор, MFD, обозреватель, Git…)."
            : "Сейчас: ON GND — границы можно двигать. Клик: IN AIR (зафиксировать рабочую кабину).";

    [RelayCommand]
    private void ToggleWorkspaceSplittersLock() => WorkspaceSplittersLocked = !WorkspaceSplittersLocked;
}
