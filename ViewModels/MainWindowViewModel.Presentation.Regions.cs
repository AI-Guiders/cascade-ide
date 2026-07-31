using CascadeIDE.Features.Shell.Application;

namespace CascadeIDE.ViewModels;

/// <summary>PFD/MFD region collapse, panel-hidden flags, MFD contour + legacy MfdRegion aliases.</summary>
public partial class MainWindowViewModel
{
    public string ChatPanelToggleButtonText =>
        MainWindowPresentationSurfaceProjection.MfdRegionToggleCaption(IsMfdRegionExpanded);

    public bool IsPfdRegionCollapsed => !IsPfdRegionExpanded;

    public bool IsMfdRegionCollapsed => !IsMfdRegionExpanded;

    public bool IsSolutionPanelHidden => !IsPfdRegionExpanded;
    public bool IsBuildPanelHidden => !IsBuildOutputVisible;
    public bool IsChatPanelHidden => !IsMfdRegionExpanded;
    public bool IsTerminalPanelHidden => !IsTerminalVisible;
    public bool IsProblemsPanelVisible => Capabilities.ProblemsPanelVisible;

    /// <summary>
    /// Хотя бы один элемент контента вторичного контура колонки MFD (стек <c>MfdShellPageStack</c>) включён через «Вид»:
    /// терминал, вывод сборки, Git, вкладки инструментации или страница Problems (если разрешена возможностями режима).
    /// </summary>
    public bool IsMfdContourContentVisible =>
        MainWindowPresentationSurfaceProjection.IsMfdContourContentVisible(
            IsProblemsPanelVisible,
            IsTerminalVisible,
            IsBuildOutputVisible,
            InstrumentationTabs,
            IsGitPanelVisible);

    /// <summary>Совместимость: старые имена региона MFD в main grid (см. <see cref="ChatPanelColumnPixelWidth"/> и т.д.).</summary>
    public int MfdRegionPixelWidth => ChatPanelColumnPixelWidth;

    public bool IsMfdRegionVisible => IsChatPanelColumnVisible;

    public string MfdRegionToggleButtonText => ChatPanelToggleButtonText;
}
