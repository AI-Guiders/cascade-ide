using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Shell.Application;

namespace CascadeIDE.ViewModels;

/// <summary>Safety level badges + risk/result/LOC/progress cards (Presentation slice).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Карточка уровня безопасности: в Power — safety.observe/confirm/autonomous; в Focus/Balanced — компактные кнопки (разметка в ChatPanelView).</summary>
    public bool ShowSafetyControls => true;

    public bool IsSafetyObserve =>
        MainWindowPresentationCapabilitiesProjection.IsSafetyLevel(SafetyLevel, AgentSafetyLevel.Observe);
    public bool IsSafetyConfirm =>
        MainWindowPresentationCapabilitiesProjection.IsSafetyLevel(SafetyLevel, AgentSafetyLevel.Confirm);
    public bool IsSafetyAutonomous =>
        MainWindowPresentationCapabilitiesProjection.IsSafetyLevel(SafetyLevel, AgentSafetyLevel.Autonomous);

    /// <summary>Подпись режима безопасности (как на мокапе Power).</summary>
    public string SafetyLevelDescription =>
        MainWindowPresentationSurfaceProjection.SafetyLevelDescription(SafetyLevel);

    public double SafetyObserveOpacity =>
        MainWindowPresentationSurfaceProjection.SafetyBadgeOpacity(IsSafetyObserve);
    public double SafetyConfirmOpacity =>
        MainWindowPresentationSurfaceProjection.SafetyBadgeOpacity(IsSafetyConfirm);
    public double SafetyAutonomousOpacity =>
        MainWindowPresentationSurfaceProjection.SafetyBadgeOpacity(IsSafetyAutonomous);

    public bool IsRiskSummaryVisible =>
        MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder(
            RiskSummary,
            MainWindowPresentationSurfaceProjection.DefaultRiskSummaryPlaceholder);

    public bool IsResultSummaryVisible =>
        MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder(
            ResultSummary,
            MainWindowPresentationSurfaceProjection.DefaultResultSummaryPlaceholder);

    public bool IsRiskCardVisible =>
        MainWindowPresentationCapabilitiesProjection.IsRiskCardVisible(Capabilities, IsRiskSummaryVisible);

    public bool IsResultCardVisible =>
        MainWindowPresentationCapabilitiesProjection.IsResultCardVisible(Capabilities, IsResultSummaryVisible);
    public bool IsLocBadgeVisible => LocBadge > 0;

    /// <summary>Строка бейджа LOC: число непустых строк и ось Low/Medium/High (пороги из <c>[loc_limits]</c>).</summary>
    public string LocBadgeSummary =>
        MainWindowPresentationCapabilitiesProjection.LocBadgeSummary(LocBadge, LocTierLabel);
    public bool IsImpactedTestsBadgeVisible => ImpactedTestsBadge > 0;
    public bool IsActiveTaskProgressVisible => ActiveTaskProgress > 0;
}
