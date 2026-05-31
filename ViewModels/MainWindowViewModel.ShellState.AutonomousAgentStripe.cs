using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Shell.Application;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Часть <see cref="MainWindowViewModel.ShellState"/>: полоса/карточки автономной задачи агента, безопасности, LOC и сводки тестов для IDE Health.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    private string _activeTaskTitle = "Нет активной задачи";

    [ObservableProperty]
    private string _activeTaskStatus = "Ожидание";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActiveTaskProgressVisible))]
    private int _activeTaskProgress;

    [ObservableProperty]
    private string _activeObjective = "Нет активной операции агента.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRiskSummaryVisible))]
    [NotifyPropertyChangedFor(nameof(IsRiskCardVisible))]
    private string _riskSummary = MainWindowPresentationSurfaceProjection.DefaultRiskSummaryPlaceholder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResultSummaryVisible))]
    [NotifyPropertyChangedFor(nameof(IsResultCardVisible))]
    private string _resultSummary = MainWindowPresentationSurfaceProjection.DefaultResultSummaryPlaceholder;

    [ObservableProperty]
    private string _nextActionSummary = "Ожидание следующего шага.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSafetyObserve))]
    [NotifyPropertyChangedFor(nameof(IsSafetyConfirm))]
    [NotifyPropertyChangedFor(nameof(IsSafetyAutonomous))]
    [NotifyPropertyChangedFor(nameof(SafetyLevelDescription))]
    [NotifyPropertyChangedFor(nameof(SafetyObserveOpacity))]
    [NotifyPropertyChangedFor(nameof(SafetyConfirmOpacity))]
    [NotifyPropertyChangedFor(nameof(SafetyAutonomousOpacity))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(IsPfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private string _safetyLevel = AgentSafetyLevel.Confirm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocBadgeVisible))]
    [NotifyPropertyChangedFor(nameof(LocBadgeSummary))]
    private int _locBadge;

    /// <summary>Подпись уровня Low/Medium/High для <see cref="LocBadgeSummary"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocBadgeSummary))]
    private string _locTierLabel = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImpactedTestsBadgeVisible))]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsText))]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsCockpitShort))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private int _impactedTestsBadge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsText))]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsCockpitShort))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private string _lastTestSummary = "";
}
