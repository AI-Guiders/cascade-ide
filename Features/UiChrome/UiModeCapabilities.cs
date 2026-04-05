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
    /// <summary>Дубль телеметрии на вкладке «Терминал» в Power, когда полоска под редактором скрыта.</summary>
    bool TelemetryOnTerminalTab,
    /// <summary>Column span нижней телеметрии в MainGrid при Power и видимой полоске телеметрии под редактором.</summary>
    int TelemetryMainColumnSpan,
    /// <summary>Вкладки инструментирования (события/тесты/…), когда док включён.</summary>
    bool InstrumentationTabs,
    /// <summary>Вкладка «Гипотезы» (семья Debug и док).</summary>
    bool HypothesesTab,
    bool RiskSummaryCard,
    bool ResultSummaryCard)
{
    /// <summary>Дефолты по семье, если в TOML нет переопределений и нет наследуемого родителя.</summary>
    public static UiModeCapabilities DefaultsForFamily(UiModeFamily family)
    {
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
            TelemetryOnTerminalTab: false,
            TelemetryMainColumnSpan: power ? 3 : 5,
            InstrumentationTabs: focus || balanced || flight || power || agentChat || debug,
            HypothesesTab: debug,
            RiskSummaryCard: !focus && !agentChat,
            ResultSummaryCard: !focus && !agentChat);
    }
}
