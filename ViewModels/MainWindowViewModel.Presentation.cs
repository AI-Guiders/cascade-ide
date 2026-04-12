using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;

namespace CascadeIDE.ViewModels;

/// <summary>Вычисляемые свойства разметки, Workspace Health и видимости панелей (режимы UI).</summary>
public partial class MainWindowViewModel
{
    public static IReadOnlyList<string> UiModeOptions => UiModeCatalog.OrderedModeIds;
    public IReadOnlyList<string> UiModeOptionsList => UiModeOptions;

    /// <summary>Семейство текущего UI-режима (одна ось вместо булевых Is*Mode).</summary>
    public UiModeFamily UiModeFamily => UiModeFamilyResolver.FromNormalizedMode(NormalizeUiMode(UiMode));

    /// <summary>Заголовок главного окна (в Power — подпись «Autonomous Agent Cockpit»); из TOML — <c>main_window_title</c>.</summary>
    public string WindowTitle =>
        UiModeCatalog.GetWindowTitleOverride(NormalizeUiMode(UiMode))
        ?? UiModeFamily switch
        {
            UiModeFamily.Power => "CascadeIDE — Power Mode [Autonomous Agent Cockpit]",
            UiModeFamily.AgentChat => "CascadeIDE — Agent Chat",
            UiModeFamily.Debug => "CascadeIDE — Debug",
            UiModeFamily.Editor => "CascadeIDE — Editor",
            _ => "CascadeIDE",
        };

    /// <summary>Ширина колонки чата (пиксели); свёрнут — 0 (место отдаётся редактору).</summary>
    public int ChatPanelColumnPixelWidth =>
        IsChatPanelExpanded
            ? UiModeCatalog.GetChatPanelExpandedWidthPixels(NormalizeUiMode(UiMode))
            : UiWorkspaceLayoutRuntimeMetrics.ChatPanelCollapsedWidthPixels;

    /// <summary>Есть правая колонка чата и сплиттер перед ней (не свёрнуто в ноль).</summary>
    public bool IsChatPanelColumnVisible => ChatPanelColumnPixelWidth > 0;

    /// <summary>
    /// Какая топология размещения зон сейчас активна. Свойства <see cref="IsPfdColumnVisible"/> / <see cref="IsMfdColumnVisible"/>
    /// имеют смысл только для <see cref="AttentionLayoutSurfaceKind.MainWindowDockedGrid"/>; иные варианты — ADR 0021 §13, 0017.
    /// </summary>
    public AttentionLayoutSurfaceKind ActiveAttentionLayoutSurface =>
        _suppressMfdColumnForMfdHostWindow && _presentationMfdHostTopology
            ? AttentionLayoutSurfaceKind.MainWindowPlusMfdHostTopLevel
            : AttentionLayoutSurfaceKind.MainWindowDockedGrid;

    /// <summary>
    /// Видна ли колонка <c>MainGrid</c> под левый якорь при <see cref="ActiveAttentionLayoutSurface"/> (в этой разметке — зона PFD).
    /// Не путать с картой «панель → зона»: <see cref="AttentionZonePanelRuntime"/>, <c>docs/design/attention-zone-panel-playbook-v1.md</c>.
    /// Ширина колонки совпадает с «обозреватель решения».
    /// </summary>
    public bool IsPfdColumnVisible => IsSolutionExplorerVisible;

    /// <summary>
    /// Видна ли колонка <c>MainGrid</c> под правый якорь при <see cref="ActiveAttentionLayoutSurface"/> (в этой разметке — зона MFD).
    /// Не путать с вкладками MFD или картой панелей — <see cref="AttentionZonePanelRuntime"/>; место в сетке совпадает с <see cref="IsChatPanelColumnVisible"/>.
    /// </summary>
    public bool IsMfdColumnVisible =>
        !_suppressMfdColumnForMfdHostWindow && IsChatPanelColumnVisible;
    /// <summary>Полоса активной задачи / Task Cockpit — из <c>UiModes/&lt;id&gt;.toml</c> (<c>active_task_strip</c>); по умолчанию скрыто для семьи Debug.</summary>
    public bool ShowTaskBar => UiModeCatalog.GetShowTaskBar(NormalizeUiMode(UiMode));

    private UiModeCapabilities Capabilities =>
        UiModeCatalog.GetCapabilities(NormalizeUiMode(UiMode));

    public bool QuickActions => Capabilities.QuickActions;
    public bool ShowAgentOperations => true;
    /// <summary>В Focus справа показываем план и гейт, в Power — trace/safety; блок «операции» остаётся в Balanced.</summary>
    public bool AgentOperationsPanel => Capabilities.AgentOperationsPanel;
    public bool AgentTrace => Capabilities.AgentTrace;
    public bool AutonomousAgentTelemetry => Capabilities.AutonomousAgentTelemetry;
    /// <summary>Карточка уровня безопасности: в Power — крупные L1–L3; в Focus/Balanced — компактные кнопки (разметка в ChatPanelView).</summary>
    public bool ShowSafetyControls => true;
    public bool ShowTelemetryHiddenHint => AutonomousAgentTelemetry && !IsTerminalVisible;

    /// <summary>
    /// Дублирующая карточка Workspace Health на вкладке «Терминал» в Power. Пока видна полоса <see cref="WorkspaceHealthStripView"/> под редактором —
    /// false, чтобы DockPanel не отдавал высоту дублю и не схлопывал область вывода консоли.
    /// </summary>
    public bool WorkspaceHealthOnTerminalTab =>
        Capabilities.WorkspaceHealthOnTerminalTab && !ShowWorkspaceHealthStrip;

    /// <summary>Куда вести Workspace Health: нижняя полоса или страница зоны — из capabilities (<c>workspace_health_surface</c>).</summary>
    public WorkspaceHealthUiSurface WorkspaceHealthUiSurface => Capabilities.WorkspaceHealthSurface;

    /// <summary>Полоска build/tests/debug/git — при <c>workspace_health_strip</c> и <c>bottom_strip</c>; рисуется в <see cref="Views.WorkspaceChromeBandView"/> внутри MFD.</summary>
    public bool ShowWorkspaceHealthStrip =>
        Capabilities.WorkspaceHealthStripVisible && Capabilities.WorkspaceHealthSurface == WorkspaceHealthUiSurface.BottomStrip;

    /// <summary>Workspace Health на странице вторичного контура (вместо нижней полосы) — при <c>workspace_health_strip</c> и <c>workspace_health_surface = dedicated_page</c> (v1 — колонка зоны Mfd).</summary>
    public bool ShowWorkspaceHealthSecondaryPage =>
        Capabilities.WorkspaceHealthStripVisible && Capabilities.WorkspaceHealthSurface == WorkspaceHealthUiSurface.DedicatedPage;

    /// <summary>
    /// Полоса оповещений EICAS v1 (над полосой Workspace Health). Видно при <c>eicas_alerts_bar</c> и непустом списке (Dark Cockpit).
    /// Отдельный контур от build/tests/debug/git (ADR 0021 §5; словарь §1.1).
    /// </summary>
    public bool ShowEicasAlertsBar =>
        Capabilities.EicasAlertsBarEnabled && EicasMessages.Count > 0;

    /// <summary>Область разметки над нижним доком: Workspace Health и/или полоса EICAS (<see cref="Views.WorkspaceChromeBandView"/>).</summary>
    public bool ShowWorkspaceChromeBand => ShowWorkspaceHealthStrip || ShowEicasAlertsBar;

    /// <summary>Панель инструментов под меню — из capabilities (<c>main_toolbar</c> в TOML).</summary>
    public bool ShowMainToolbar => Capabilities.MainToolbarVisible;

    /// <summary>Зона под чатом в MFD: полоса EICAS / Workspace Health и/или док (терминал, сборка, Problems, Git, инструменты).</summary>
    public bool ShowWorkspaceBottomChrome =>
        ShowWorkspaceHealthStrip || ShowEicasAlertsBar || IsBottomPanelVisible;

    /// <summary>Чат в одной строке с PFD/Forward; MFD не пересекает нижнюю строку MainGrid.</summary>
    public int ChatPanelMainGridRowSpan => 1;

    public string TelemetryButtonText => IsTerminalVisible ? "Telemetry: on" : "Show telemetry";
    public bool ShowEditorGroup2 => EditorGroupCount >= 2;
    public bool ShowEditorGroup3 => EditorGroupCount >= 3;

    /// <summary>Нижние вкладки «События / Тесты / Гипотезы / Отладка» при включённом доке.</summary>
    public bool InstrumentationTabs =>
        IsInstrumentationDockVisible && Capabilities.InstrumentationTabs;

    /// <summary>Вкладка «Гипотезы» — семья Debug и capabilities (ADR 0003, ADR 0010).</summary>
    public bool HypothesesTab =>
        IsInstrumentationDockVisible
        && Capabilities.InstrumentationTabs
        && Capabilities.HypothesesTab;

    /// <summary>Пункт меню для док-панели инструментирования (можно отключить и в Focus).</summary>
    public bool ShowInstrumentationLayoutMenu => true;

    public bool IsSafetyL1 => string.Equals(SafetyLevel, "L1", StringComparison.OrdinalIgnoreCase);
    public bool IsSafetyL2 => string.Equals(SafetyLevel, "L2", StringComparison.OrdinalIgnoreCase);
    public bool IsSafetyL3 => string.Equals(SafetyLevel, "L3", StringComparison.OrdinalIgnoreCase);

    /// <summary>Подпись режима безопасности (как на мокапе Power).</summary>
    public string SafetyLevelDescription =>
        SafetyLevel switch
        {
            "L1" => Resources.Safety_Description_L1,
            "L2" => Resources.Safety_Description_L2,
            "L3" => Resources.Safety_Description_L3,
            _ => ""
        };

    public double SafetyL1Opacity => IsSafetyL1 ? 1 : 0.38;
    public double SafetyL2Opacity => IsSafetyL2 ? 1 : 0.38;
    public double SafetyL3Opacity => IsSafetyL3 ? 1 : 0.38;

    public bool HasFocusPlanItems => FocusPlanItems.Count > 0;

    public bool IsRiskSummaryVisible =>
        !string.IsNullOrWhiteSpace(RiskSummary)
        && !string.Equals(RiskSummary, "Риски не зафиксированы.", StringComparison.Ordinal);

    public bool IsResultSummaryVisible =>
        !string.IsNullOrWhiteSpace(ResultSummary)
        && !string.Equals(ResultSummary, "Результатов пока нет.", StringComparison.Ordinal);

    public bool IsRiskCardVisible =>
        Capabilities.RiskSummaryCard && IsRiskSummaryVisible;

    public bool IsResultCardVisible =>
        Capabilities.ResultSummaryCard && IsResultSummaryVisible;
    public bool IsComplexityBadgeVisible => ComplexityBadge > 0;
    public bool IsImpactedTestsBadgeVisible => ImpactedTestsBadge > 0;
    public bool IsActiveTaskProgressVisible => ActiveTaskProgress > 0;

    /// <summary>Строки из <see cref="IWorkspaceHealthProvider"/> (форматирование в <see cref="WorkspaceHealthFormat"/>).</summary>
    public string WorkspaceHealthBuildText => _workspaceHealth.GetSnapshot().Build.LineText;

    /// <summary>Короткий статус для «кольца» сборки в Power cockpit.</summary>
    public string WorkspaceHealthBuildCockpitShort => _workspaceHealth.GetSnapshot().Build.CockpitShort;

    public string WorkspaceHealthTestsText => _workspaceHealth.GetSnapshot().Tests.LineText;

    /// <summary>Компактная строка тестов для полосы Power.</summary>
    public string WorkspaceHealthTestsCockpitShort => _workspaceHealth.GetSnapshot().Tests.CockpitShort;

    /// <summary>Есть активная DAP-сессия (режим отладки, как в VS).</summary>
    public bool HasDebugSession => _dapDebug.HasActiveSession;

    /// <summary>Выполнение остановлено — доступны шаги и просмотр стека.</summary>
    public bool IsDebugExecutionPaused => _dapDebug.HasActiveSession && _dapDebug.IsExecutionStopped;

    /// <summary>Процесс запущен под отладчиком, выполнение идёт.</summary>
    public bool IsDebugExecutionRunning => _dapDebug.HasActiveSession && !_dapDebug.IsExecutionStopped;

    public string WorkspaceHealthDebugText => _workspaceHealth.GetSnapshot().Debug.LineText;

    /// <summary>Короткий статус отладки для Power.</summary>
    public string WorkspaceHealthDebugCockpitShort => _workspaceHealth.GetSnapshot().Debug.CockpitShort;

    public string ChatPanelToggleButtonText => IsChatPanelExpanded ? "◀" : "▶";
    public bool IsSolutionPanelHidden => !IsSolutionExplorerVisible;
    public bool IsBuildPanelHidden => !IsBuildOutputVisible;
    public bool IsChatPanelHidden => !IsChatPanelExpanded;
    public bool IsTerminalPanelHidden => !IsTerminalVisible;
    public bool IsProblemsPanelVisible => Capabilities.ProblemsPanelVisible;

    public bool IsBottomPanelVisible =>
        IsProblemsPanelVisible || IsTerminalVisible || IsBuildOutputVisible || InstrumentationTabs || IsGitPanelVisible;
}
