using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Composition;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Вычисляемые свойства разметки и видимости панелей (режимы UI).
/// Skia → <c>Presentation.Skia</c>; IDE Health → <c>Presentation.IdeHealth</c>; badges → <c>Presentation.Badges</c>.</summary>
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
    public int ChatPanelColumnPixelWidth =>
        IsCompactPresentationTier
            ? CompactRightChromeColumnPixelWidth
            : ShellSurfaceComposition.MfdColumnPixelWidthInMainGrid;

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
    public bool IsPfdColumnVisible =>
        IsCompactPresentationTier
            ? false
            : ShellSurfaceComposition.PfdSurfaceVisible;

    /// <summary>
    /// Видна ли колонка <c>MainGrid</c> под правый якорь при <see cref="ActiveAttentionLayoutSurface"/> (в этой разметке — зона MFD).
    /// Не путать с вкладками MFD или картой панелей — <see cref="AttentionZonePanelRuntime"/>; место в сетке совпадает с <see cref="IsChatPanelColumnVisible"/>.
    /// </summary>
    public bool IsMfdColumnVisible =>
        IsCompactPresentationTier
            ? IsCompactRightChromeColumnVisible
            : ShellSurfaceComposition.MfdColumnVisibleInMainGrid;

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
    public bool ShowTelemetryHiddenHint => UiModeGateSpecifications.ShowTelemetryHiddenHint.IsSatisfiedBy(
        new UiModeGateContext(UiModeFamily, AutonomousAgentTelemetry, IsTerminalVisible, HasDebugSession));

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

    public bool HasFocusPlanItems => FocusPlanItems.Count > 0;

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
