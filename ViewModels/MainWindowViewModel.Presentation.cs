using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.Composition;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Вычисляемые свойства разметки, Workspace Health и видимости панелей (режимы UI).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Семейство текущего UI-режима (одна ось вместо булевых Is*Mode).</summary>
    public UiModeFamily UiModeFamily => UiModeFamilyResolver.FromNormalizedMode(NormalizeUiMode(UiMode));

    /// <summary>Настройки отображения для композиторов кабины (mount, Skia, instrument routing).</summary>
    public DisplaySettings DisplaySettings => _settings.Display;

    /// <summary>Заголовок главного окна (в Power — подпись «Autonomous Agent Cockpit»); из TOML — <c>main_window_title</c>.</summary>
    public string WindowTitle =>
        MainWindowPresentationSurfaceProjection.ResolveWindowTitle(NormalizeUiMode(UiMode));

    /// <summary>Композитор: intent + CDS style → кадр хоста (колонки + инструменты слотов; ADR 0036 п.3, 0047).</summary>
    private MainWindowHostSurfaceFrame HostSurfaceFrame =>
        MainWindowPresentationSurfaceProjection.ComposeHostSurfaceFrame(
            this,
            NormalizeUiMode(UiMode),
            CurrentMfdShellPage,
            PrimaryWorkSurface);

    private MainWindowShellSurfaceComposition ShellSurfaceComposition => HostSurfaceFrame.Shell;

    /// <summary>Логические инструменты по слотам для главного окна; хост (Avalonia/Skia) сопоставляет <c>instrument_id</c> разметке.</summary>
    public IReadOnlyList<CockpitInstrumentDescriptor> MainWindowHostSurfaceInstruments => HostSurfaceFrame.Instruments;

    /// <summary>Ширина региона MFD в main grid (пиксели); 0 если колонка не выделяется (хост MFD и т.п.).</summary>
    public int ChatPanelColumnPixelWidth => ShellSurfaceComposition.MfdColumnPixelWidthInMainGrid;

    /// <summary>Есть правая колонка MFD и сплиттер перед ней (ширина &gt; 0 в main).</summary>
    public bool IsChatPanelColumnVisible =>
        MainWindowPresentationSurfaceProjection.IsMainGridSplitColumnVisible(ChatPanelColumnPixelWidth);

    /// <summary>
    /// Какая топология размещения зон сейчас активна. Свойства <see cref="IsPfdColumnVisible"/> / <see cref="IsMfdColumnVisible"/>
    /// имеют смысл только для <see cref="AttentionLayoutSurfaceKind.MainWindowDockedGrid"/>; иные варианты — ADR 0021 §13, 0017.
    /// </summary>
    public AttentionLayoutSurfaceKind ActiveAttentionLayoutSurface =>
        AttentionLayoutSurfaceResolver.Resolve(
            _suppressPfdColumnForPfdHostWindow,
            _suppressMfdColumnForMfdHostWindow,
            PresentationRequestsPfdHostWindow,
            _presentationMfdHostTopology);

    /// <summary>
    /// Видна ли колонка <c>MainGrid</c> под левый якорь при <see cref="ActiveAttentionLayoutSurface"/> (в этой разметке — зона PFD).
    /// Не путать с картой «панель → зона»: <see cref="AttentionZonePanelRuntime"/>, <c>docs/design/attention-zone-panel-playbook-v1.md</c>.
    /// Ширина колонки совпадает с поверхностью PFD в main grid.
    /// </summary>
    public bool IsPfdColumnVisible => ShellSurfaceComposition.PfdSurfaceVisible;

    /// <summary>
    /// Видна ли колонка <c>MainGrid</c> под правый якорь при <see cref="ActiveAttentionLayoutSurface"/> (в этой разметке — зона MFD).
    /// Не путать с вкладками MFD или картой панелей — <see cref="AttentionZonePanelRuntime"/>; место в сетке совпадает с <see cref="IsChatPanelColumnVisible"/>.
    /// </summary>
    public bool IsMfdColumnVisible => ShellSurfaceComposition.MfdColumnVisibleInMainGrid;

    /// <summary>Включён debug-overlay контуров зон (ручная валидация геометрии W2).</summary>
    public bool ShowSkiaZoneGeometryOverlay => _settings.Display.Skia.ZoneGeometryOverlay;

    public bool IsSkiaZoneGeometryOverlayPfdVisible =>
        MainWindowPresentationCapabilitiesProjection.IsSkiaZoneGeometryOverlayPfdVisible(
            ShowSkiaZoneGeometryOverlay,
            IsPfdColumnVisible);

    public bool IsSkiaZoneGeometryOverlayForwardVisible =>
        MainWindowPresentationCapabilitiesProjection.IsSkiaZoneGeometryOverlayForwardVisible(
            ShowSkiaZoneGeometryOverlay);

    public bool IsSkiaZoneGeometryOverlayMfdVisible =>
        MainWindowPresentationCapabilitiesProjection.IsSkiaZoneGeometryOverlayMfdVisible(
            ShowSkiaZoneGeometryOverlay,
            IsMfdColumnVisible);

    /// <summary>Wave 3: включить отрисовку инструмента в Skia mount-слое зон P/F/M.</summary>
    public bool UseSkiaInstrumentMount => _settings.Display.Skia.InstrumentMount;

    /// <summary>Декларативный mount-style mount-инструмента (идёт из <c>[display.mount]</c>).</summary>
    public string InstrumentMountStyle =>
        MainWindowPresentationSurfaceProjection.InstrumentMountDisplayStyle(_settings.Display);

    /// <summary>Резолв style для mount в слоте PFD с учётом registry-правил.</summary>
    public string PfdInstrumentMountStyle =>
        MainWindowPresentationSurfaceProjection.ResolveInstrumentMountStyleForSlot(
            _instrumentMountPolicyResolver,
            _settings.Display,
            ActiveAttentionLayoutSurface,
            "pfd",
            CockpitStandardInstrumentIds.IdeHealthStatusV1);

    /// <summary>Резолв style для mount в слоте MFD с учётом registry-правил.</summary>
    public string MfdInstrumentMountStyle =>
        MainWindowPresentationSurfaceProjection.ResolveInstrumentMountStyleForSlot(
            _instrumentMountPolicyResolver,
            _settings.Display,
            ActiveAttentionLayoutSurface,
            "mfd",
            CockpitStandardInstrumentIds.IdeHealthStatusV1);
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
    /// <summary>Карточка уровня безопасности: в Power — safety.observe/confirm/autonomous; в Focus/Balanced — компактные кнопки (разметка в ChatPanelView).</summary>
    public bool ShowSafetyControls => true;
    public bool ShowTelemetryHiddenHint => UiModeGateSpecifications.ShowTelemetryHiddenHint.IsSatisfiedBy(
        new UiModeGateContext(UiModeFamily, AutonomousAgentTelemetry, IsTerminalVisible, HasDebugSession));

    /// <summary>
    /// Дублирующая карточка IDE Health на вкладке «Терминал» в Power. Пока видна полоса <see cref="WorkspaceHealthStripView"/> под редактором —
    /// false, чтобы DockPanel не отдавал высоту дублю и не схлопывал область вывода консоли.
    /// </summary>
    public bool IdeHealthOnTerminalTab =>
        MainWindowPresentationCapabilitiesProjection.IdeHealthOnTerminalTab(Capabilities, ShowIdeHealthStrip);

    /// <summary>Куда вести полосу IDE Health: нижняя полоса или страница зоны — из capabilities (<c>ide_health_surface</c>).</summary>
    public IdeHealthUiSurface IdeHealthStripSurface => Capabilities.IdeHealthSurface;

    /// <summary>Форма представления канала IDE Health на оси <see cref="ContentRepresentation"/> (ADR 0063).</summary>
    public ContentRepresentation IdeHealthContentRepresentation => Capabilities.IdeHealthContentRepresentation;

    /// <summary>Полоска build/tests/debug/git — при <c>ide_health_strip</c> и <c>bottom_strip</c>; рисуется в <see cref="Views.WorkspaceChromeBandView"/> внутри MFD.</summary>
    public bool ShowIdeHealthStrip =>
        MainWindowPresentationCapabilitiesProjection.ShowIdeHealthStrip(Capabilities);

    /// <summary>IDE Health на странице оболочки Mfd (вместо нижней полосы) — при <c>ide_health_strip</c> и <c>ide_health_surface = dedicated_page</c> (v1 — колонка зоны Mfd).</summary>
    public bool ShowIdeHealthMfdPage =>
        MainWindowPresentationCapabilitiesProjection.ShowIdeHealthMfdPage(Capabilities);

    /// <summary>
    /// Полоса оповещений EICAS v1 (над полосой Workspace Health). Видно при <c>eicas_alerts_bar</c> и непустом списке (Dark Cockpit).
    /// Отдельный контур от build/tests/debug/git (ADR 0021 §5; словарь §1.1).
    /// </summary>
    public bool ShowEicasAlertsBar =>
        MainWindowPresentationCapabilitiesProjection.ShowEicasAlertsBar(Capabilities, EicasMessages.Count);

    /// <summary>Область разметки над нижним доком: Workspace Health и/или полоса EICAS (<see cref="Views.WorkspaceChromeBandView"/>).</summary>
    public bool ShowWorkspaceChromeBand =>
        MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(
            ShowIdeHealthStrip,
            ShowEicasAlertsBar);

    /// <summary>Зона под чатом в MFD: полоса EICAS / IDE Health и/или док (терминал, сборка, Problems, Git, инструменты).</summary>
    public bool ShowWorkspaceBottomChrome =>
        MainWindowPresentationCapabilitiesProjection.ShowWorkspaceBottomChrome(
            ShowIdeHealthStrip,
            ShowEicasAlertsBar,
            IsMfdContourContentVisible);

    /// <summary>Чат в одной строке с PFD/Forward; MFD не пересекает нижнюю строку MainGrid.</summary>
    public int ChatPanelMainGridRowSpan => 1;

    public string TelemetryButtonText =>
        MainWindowPresentationSurfaceProjection.TelemetryButtonCaption(IsTerminalVisible);
    public bool ShowEditorGroup2 => EditorGroupCount >= 2;
    public bool ShowEditorGroup3 => EditorGroupCount >= 3;

    /// <summary>Нижние вкладки «События / Тесты / Гипотезы / Отладка» при включённом доке.</summary>
    public bool InstrumentationTabs =>
        MainWindowPresentationCapabilitiesProjection.InstrumentationTabs(IsInstrumentationDockVisible, Capabilities);

    /// <summary>Вкладка «Гипотезы» — семья Debug и capabilities (ADR 0003, ADR 0010).</summary>
    public bool HypothesesTab =>
        MainWindowPresentationCapabilitiesProjection.HypothesesTab(IsInstrumentationDockVisible, Capabilities);

    /// <summary>Пункт меню для док-панели инструментирования (можно отключить и в Focus).</summary>
    public bool ShowInstrumentationLayoutMenu => true;

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

    public bool HasFocusPlanItems => FocusPlanItems.Count > 0;

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

    /// <summary>Строки из канала IDE Health (один снимок на <see cref="MainWindowViewModel.RebuildIdeHealth"/>, без повторного <c>Build()</c> в геттерах).</summary>
    public string IdeHealthBuildText =>
        IdeHealthStripPresentationProjection.SolutionBuildLineText(_lastIdeHealthInputSnapshot);

    /// <summary>Короткий статус для «кольца» сборки в Power cockpit.</summary>
    public string IdeHealthBuildCockpitShort =>
        IdeHealthStripPresentationProjection.SolutionBuildCockpitShort(_lastIdeHealthInputSnapshot);

    public string IdeHealthTestsText =>
        IdeHealthStripPresentationProjection.SolutionTestsLineText(_lastIdeHealthInputSnapshot);

    /// <summary>Компактная строка тестов для полосы Power.</summary>
    public string IdeHealthTestsCockpitShort =>
        IdeHealthStripPresentationProjection.SolutionTestsCockpitShort(_lastIdeHealthInputSnapshot);

    /// <summary>Есть активная DAP-сессия (режим отладки, как в VS).</summary>
    public bool HasDebugSession => _dapDebug.HasActiveSession;

    /// <summary>Выполнение остановлено — доступны шаги и просмотр стека.</summary>
    public bool IsDebugExecutionPaused =>
        MainWindowPresentationDapProjection.IsDebugExecutionPaused(
            _dapDebug.HasActiveSession,
            _dapDebug.IsExecutionStopped);

    /// <summary>Процесс запущен под отладчиком, выполнение идёт.</summary>
    public bool IsDebugExecutionRunning =>
        MainWindowPresentationDapProjection.IsDebugExecutionRunning(
            _dapDebug.HasActiveSession,
            _dapDebug.IsExecutionStopped);

    public string IdeHealthDebugText =>
        IdeHealthStripPresentationProjection.SolutionDebugLineText(_lastIdeHealthInputSnapshot);

    /// <summary>Короткий статус отладки для Power.</summary>
    public string IdeHealthDebugCockpitShort =>
        IdeHealthStripPresentationProjection.SolutionDebugCockpitShort(_lastIdeHealthInputSnapshot);

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

    /// <summary>Снимок для Skia mount — тот же тик, что <see cref="IdeHealthBuildCockpitShort"/>; обновляется в <see cref="MainWindowViewModel.RebuildIdeHealth"/>.</summary>
    public IdeHealthStatusMountPayload IdeHealthMountPayload =>
        _lastIdeHealthMountPayload ?? new IdeHealthStatusMountPayload("", "", "", SafetyLevel);

    public bool IsPfdIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleInDockedColumn(
            UseSkiaInstrumentMount,
            IsPfdColumnVisible);

    public bool IsMfdIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleInDockedColumn(
            UseSkiaInstrumentMount,
            IsMfdColumnVisible);

    public bool IsMfdHostWindowIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleForHostWindow(
            UseSkiaInstrumentMount,
            IsMfdHostWindowShellOpen);

    public bool IsPfdHostWindowIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleForHostWindow(
            UseSkiaInstrumentMount,
            IsPfdHostWindowShellOpen);

    public IdeHealthStatusMountContext? PfdIdeHealthMountContext =>
        MainWindowPresentationSurfaceProjection.ResolvePfdIdeHealthMountContext(
            UseSkiaInstrumentMount,
            IsPfdHostWindowShellOpen,
            IsPfdColumnVisible,
            _instrumentMountPolicyResolver,
            _settings.Display,
            MainWindowPresentationSurfaceProjection.MountPolicySurfaceId(ActiveAttentionLayoutSurface),
            IdeHealthMountPayload);

    public IdeHealthStatusMountContext? MfdIdeHealthMountContext =>
        MainWindowPresentationSurfaceProjection.ResolveMfdIdeHealthMountContext(
            UseSkiaInstrumentMount,
            IsMfdHostWindowShellOpen,
            IsMfdColumnVisible,
            _instrumentMountPolicyResolver,
            _settings.Display,
            MainWindowPresentationSurfaceProjection.MountPolicySurfaceId(ActiveAttentionLayoutSurface),
            IdeHealthMountPayload);
}
