using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Contracts;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>
/// Статические проекции для вычисляемых свойств главного окна (видимость, подписи, mount-контекст без логики на VM).
/// </summary>
[PresentationProjection]
public static class MainWindowPresentationSurfaceProjection
{
    /// <summary>Кадр хоста: intent + CDS style → shell + инструменты (ADR 0036 п.3, 0047).</summary>
    public static MainWindowHostSurfaceFrame ComposeHostSurfaceFrame(
        IMainWindowHostSurfaceInput host,
        string normalizedUiMode,
        MfdShellPage currentMfdShellPage,
        PrimaryWorkSurfaceKind primaryWorkSurface) =>
        MainWindowHostSurfaceProjection.ComposeFrame(
            host,
            ResolveExpandedMfdWidthPixels(normalizedUiMode, currentMfdShellPage, primaryWorkSurface),
            UiWorkspaceLayoutRuntimeMetrics.MfdRegionCollapsedWidthPixels);

    /// <summary>
    /// Ширина колонки MFD: для страницы «Чат» — не уже <see cref="UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedAgentChatWidthPixels"/> (520).
    /// </summary>
    public static int ResolveExpandedMfdWidthPixels(
        string normalizedUiMode,
        MfdShellPage currentMfdShellPage,
        PrimaryWorkSurfaceKind primaryWorkSurface)
    {
        _ = primaryWorkSurface;
        var modeDefault = UiModeCatalog.GetMfdRegionExpandedWidthPixels(normalizedUiMode);
        if (currentMfdShellPage != MfdShellPage.Chat)
            return modeDefault;

        return Math.Max(modeDefault, UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedAgentChatWidthPixels);
    }

    public const string DefaultRiskSummaryPlaceholder = "Риски не зафиксированы.";
    public const string DefaultResultSummaryPlaceholder = "Результатов пока нет.";

    public static string ResolveWindowTitle(string normalizedUiMode)
    {
        var o = UiModeCatalog.GetWindowTitleOverride(normalizedUiMode);
        if (o is not null)
            return o;
        return UiModeFamilyResolver.FromNormalizedMode(normalizedUiMode) switch
        {
            UiModeFamily.Power => "CascadeIDE — Power Mode [Autonomous Agent Cockpit]",
            UiModeFamily.AgentChat => "CascadeIDE — Agent Chat",
            UiModeFamily.Debug => "CascadeIDE — Debug",
            UiModeFamily.Editor => "CascadeIDE — Editor",
            _ => "CascadeIDE",
        };
    }

    public static string InstrumentMountDisplayStyle(DisplaySettings display) =>
        string.IsNullOrWhiteSpace(display.Mount.DefaultStyle)
            ? InstrumentMountPolicyIds.V1
            : display.Mount.DefaultStyle.Trim();

    public static string MountPolicySurfaceId(AttentionLayoutSurfaceKind surface) =>
        surface switch
        {
            AttentionLayoutSurfaceKind.MainWindowDockedGrid => "main_window_docked_grid",
            AttentionLayoutSurfaceKind.MainWindowPlusMfdHostTopLevel => "main_window_plus_mfd_host_top_level",
            AttentionLayoutSurfaceKind.MainWindowPlusPfdHostTopLevel => "main_window_plus_pfd_host_top_level",
            AttentionLayoutSurfaceKind.MainWindowPlusPfdMfdHostTopLevel => "main_window_plus_pfd_mfd_host_top_level",
            _ => "main_window_docked_grid",
        };

    /// <summary>Сплиттер и колонка main grid: ширина слота &gt; 0.</summary>
    public static bool IsMainGridSplitColumnVisible(int columnWidthPixels) => columnWidthPixels > 0;

    public static bool IsIdeHealthSkiaMountVisibleInDockedColumn(bool useSkiaInstrumentMount, bool columnVisible) =>
        useSkiaInstrumentMount && columnVisible;

    public static bool IsIdeHealthSkiaMountVisibleForHostWindow(bool useSkiaInstrumentMount, bool hostShellOpen) =>
        useSkiaInstrumentMount && hostShellOpen;

    public static string ResolveInstrumentMountStyleForSlot(
        IInstrumentMountPolicyResolver resolver,
        DisplaySettings displaySettings,
        AttentionLayoutSurfaceKind attentionSurface,
        string slotId,
        string instrumentId)
    {
        var surfaceId = MountPolicySurfaceId(attentionSurface);
        return resolver.Resolve(displaySettings, surfaceId, slotId, instrumentId);
    }

    public static bool IsMfdContourContentVisible(
        bool problemsPanelVisible,
        bool isTerminalVisible,
        bool isBuildOutputVisible,
        bool instrumentationTabs,
        bool isGitPanelVisible) =>
        problemsPanelVisible || isTerminalVisible || isBuildOutputVisible || instrumentationTabs || isGitPanelVisible;

    public static string TelemetryButtonCaption(bool terminalVisible) =>
        terminalVisible ? "Telemetry: on" : "Show telemetry";

    public static string MfdRegionToggleCaption(bool isMfdRegionExpanded) =>
        isMfdRegionExpanded ? "◀" : "▶";

    public static string SafetyLevelDescription(string safetyLevel) =>
        safetyLevel switch
        {
            "L1" => Resources.Safety_Description_L1,
            "L2" => Resources.Safety_Description_L2,
            "L3" => Resources.Safety_Description_L3,
            _ => "",
        };

    public static double SafetyBadgeOpacity(bool isActiveLevel) => isActiveLevel ? 1 : 0.38;

    /// <summary>Текст задан и отличается от плейсхолдера «нет данных».</summary>
    public static bool IsAgentSummaryVisibleComparedToPlaceholder(string? text, string placeholder) =>
        !string.IsNullOrWhiteSpace(text)
        && !string.Equals(text, placeholder, StringComparison.Ordinal);

    /// <remarks>Повторяет ветвление прежних геттеров <c>PfdIdeHealthMountContext</c>/<c>MfdIdeHealthMountContext</c>.</remarks>
    public static IdeHealthStatusMountContext? ResolvePfdIdeHealthMountContext(
        bool useSkiaInstrumentMount,
        bool isPfdHostWindowShellOpen,
        bool isPfdColumnVisible,
        IInstrumentMountPolicyResolver resolver,
        DisplaySettings displaySettings,
        string mountPolicySurfaceIdForMainDockedGrid,
        IdeHealthStatusMountPayload payload)
    {
        if (!useSkiaInstrumentMount)
            return null;
        if (isPfdHostWindowShellOpen)
            return IdeHealthMountContextFactory.Create(
                resolver,
                displaySettings,
                "main_window_plus_pfd_host_top_level",
                CockpitSlotIds.Pfd,
                payload);
        if (isPfdColumnVisible)
            return IdeHealthMountContextFactory.Create(
                resolver,
                displaySettings,
                mountPolicySurfaceIdForMainDockedGrid,
                CockpitSlotIds.Pfd,
                payload);
        return null;
    }

    public static IdeHealthStatusMountContext? ResolveMfdIdeHealthMountContext(
        bool useSkiaInstrumentMount,
        bool isMfdHostWindowShellOpen,
        bool isMfdColumnVisible,
        IInstrumentMountPolicyResolver resolver,
        DisplaySettings displaySettings,
        string mountPolicySurfaceIdForMainDockedGrid,
        IdeHealthStatusMountPayload payload)
    {
        if (!useSkiaInstrumentMount)
            return null;
        if (isMfdHostWindowShellOpen)
            return IdeHealthMountContextFactory.Create(
                resolver,
                displaySettings,
                "main_window_plus_mfd_host_top_level",
                CockpitSlotIds.Mfd,
                payload);
        if (isMfdColumnVisible)
            return IdeHealthMountContextFactory.Create(
                resolver,
                displaySettings,
                mountPolicySurfaceIdForMainDockedGrid,
                CockpitSlotIds.Mfd,
                payload);
        return null;
    }
}
