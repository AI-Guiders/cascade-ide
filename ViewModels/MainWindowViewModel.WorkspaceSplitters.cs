using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Сплиттеры рабочей области (MainGrid, обозреватель решения, Git и т.д.): режим «взлёт» — блокировка перетаскивания.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Когда true — границы не двигаются (Take Off); false — Landing, сплиттеры активны.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceSplitterLockButtonText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceSplitterLockButtonToolTip))]
    [NotifyPropertyChangedFor(nameof(WorkspaceSplitterLockButtonBackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(WorkspaceSplittersUnlocked))]
    private bool _workspaceSplittersLocked;

    private static readonly IBrush SplitterLockTakeOffBackground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
    private static readonly IBrush SplitterLockLandingBackground = new SolidColorBrush(Color.FromRgb(46, 125, 50));

    /// <summary>Для привязки <c>IsEnabled</c> сплиттеров (в т.ч. из вложенного DataContext через <c>ElementName</c>).</summary>
    public bool WorkspaceSplittersUnlocked => !WorkspaceSplittersLocked;

    /// <summary>Надпись на кнопке: Take Off (заблокировать) или Landing (разблокировать).</summary>
    public string WorkspaceSplitterLockButtonText => WorkspaceSplittersLocked ? "Landing" : "Take Off";

    /// <summary>Take Off — красный (сплиттеры можно двигать); Landing — зелёный (заблокировано).</summary>
    public IBrush WorkspaceSplitterLockButtonBackgroundBrush =>
        WorkspaceSplittersLocked ? SplitterLockLandingBackground : SplitterLockTakeOffBackground;

    public string WorkspaceSplitterLockButtonToolTip =>
        WorkspaceSplittersLocked
            ? "Разрешить перемещение всех сплиттеров (колонки PFD | редактор | MFD, обозреватель решения, Git и т.д.)."
            : "Заблокировать сплиттеры рабочей области — в полёте кабина не меняется.";

    [RelayCommand]
    private void ToggleWorkspaceSplittersLock() => WorkspaceSplittersLocked = !WorkspaceSplittersLocked;
}
