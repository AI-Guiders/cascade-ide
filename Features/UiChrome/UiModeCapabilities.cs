namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Продуктовые флаги UI для режима после мержа TOML и <see cref="UiModeFamily"/> (ADR 0010 capabilities).
/// Значения по умолчанию повторяют прежнюю логику VM до выноса в данные.
/// </summary>
public sealed record UiModeCapabilities(
    bool QuickActions,
    bool AgentOperationsPanel,
    bool AgentTrace,
    bool AutonomousAgentTelemetry,
    /// <summary>Дубль Workspace Health на вкладке «Терминал» в Power, когда полоска под редактором скрыта.</summary>
    bool WorkspaceHealthOnTerminalTab,
    /// <summary>Column span нижней зоны Workspace Health в MainGrid при Power и видимой полоске под редактором.</summary>
    int WorkspaceHealthMainColumnSpan,
    /// <summary>Вкладки инструментирования (события/тесты/…), когда док включён.</summary>
    bool InstrumentationTabs,
    /// <summary>Вкладка «Гипотезы» (семья Debug и док).</summary>
    bool HypothesesTab,
    bool RiskSummaryCard,
    bool ResultSummaryCard,
    /// <summary>Полоса build/tests/debug/git под редактором.</summary>
    bool WorkspaceHealthStripVisible,
    /// <summary>Нижняя полоса vs страница зоны; TOML: <c>workspace_health_surface</c>.</summary>
    WorkspaceHealthUiSurface WorkspaceHealthSurface,
    /// <summary>Панель инструментов под меню.</summary>
    bool MainToolbarVisible,
    /// <summary>Вкладка Problems и учёт в <see cref="MainWindowViewModel.IsBottomPanelVisible"/>.</summary>
    bool ProblemsPanelVisible,
    /// <summary>Разрешить полосу оповещений EICAS при наличии сообщений; TOML: <c>eicas_alerts_bar</c>. См. ADR 0021 §5, §1.1.</summary>
    bool EicasAlertsBarEnabled)
{
    /// <summary>Дефолты по семье, если в TOML нет переопределений и нет наследуемого родителя.</summary>
    public static UiModeCapabilities DefaultsForFamily(UiModeFamily family)
    {
        if (family.IsEditorFamily())
        {
            return new UiModeCapabilities(
                QuickActions: false,
                AgentOperationsPanel: false,
                AgentTrace: false,
                AutonomousAgentTelemetry: false,
                WorkspaceHealthOnTerminalTab: false,
                WorkspaceHealthMainColumnSpan: 5,
                InstrumentationTabs: false,
                HypothesesTab: false,
                RiskSummaryCard: false,
                ResultSummaryCard: false,
                WorkspaceHealthStripVisible: false,
                WorkspaceHealthSurface: WorkspaceHealthUiSurface.BottomStrip,
                MainToolbarVisible: false,
                ProblemsPanelVisible: false,
                EicasAlertsBarEnabled: false);
        }

        var balanced = family.IsBalancedFamily();
        var flight = family.IsFlightFamily();
        var balancedOrFlight = balanced || flight;
        var power = family.IsPowerFamily();
        var debug = family.IsDebugFamily();
        var focus = family.IsFocusFamily();
        var agentChat = family.IsAgentChatFamily();

        return new UiModeCapabilities(
            QuickActions: balancedOrFlight,
            AgentOperationsPanel: balancedOrFlight,
            AgentTrace: power,
            AutonomousAgentTelemetry: power,
            WorkspaceHealthOnTerminalTab: false,
            WorkspaceHealthMainColumnSpan: power ? 3 : 5,
            InstrumentationTabs: focus || balanced || flight || power || agentChat || debug,
            HypothesesTab: debug,
            RiskSummaryCard: !focus && !agentChat,
            ResultSummaryCard: !focus && !agentChat,
            WorkspaceHealthStripVisible: true,
            WorkspaceHealthSurface: WorkspaceHealthUiSurface.BottomStrip,
            MainToolbarVisible: true,
            ProblemsPanelVisible: true,
            EicasAlertsBarEnabled: true);
    }
}
