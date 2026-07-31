using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

/// <summary>UiMode capabilities + instrumentation dock visibility flags.</summary>
public partial class MainWindowViewModel
{
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

    /// <summary>Нижние вкладки «События / Тесты / Гипотезы / Отладка» при включённом доке.</summary>
    public bool InstrumentationTabs =>
        MainWindowPresentationCapabilitiesProjection.InstrumentationTabs(IsInstrumentationDockVisible, Capabilities);

    /// <summary>Вкладка «Гипотезы» — семья Debug и capabilities (ADR 0003, ADR 0010).</summary>
    public bool HypothesesTab =>
        MainWindowPresentationCapabilitiesProjection.HypothesesTab(IsInstrumentationDockVisible, Capabilities);

    /// <summary>Пункт меню для док-панели инструментирования (можно отключить и в Focus).</summary>
    public bool ShowInstrumentationLayoutMenu => true;
}
