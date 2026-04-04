namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Продуктовые флаги UI для режима после мержа TOML и <see cref="UiModeFamily"/> (ADR 0010 capabilities).
/// Значения по умолчанию повторяют прежнюю логику VM до выноса в данные.
/// </summary>
public sealed record UiModeCapabilities(
    bool ShowQuickActions,
    bool ShowAgentOperationsBlock,
    bool ShowAgentTrace,
    bool ShowPowerTelemetry,
    /// <summary>Дубль телеметрии на вкладке «Терминал» в Power, когда полоска под редактором скрыта.</summary>
    bool ShowPowerTelemetryOnTerminalTab,
    /// <summary>Column span нижней телеметрии в MainGrid при Power и видимой полоске телеметрии под редактором.</summary>
    int PowerTelemetryMainGridColumnSpan,
    /// <summary>Вкладки инструментирования (события/тесты/…), когда док включён.</summary>
    bool ShowInstrumentationTabs,
    /// <summary>Вкладка «Гипотезы» (семья Debug и док).</summary>
    bool ShowHypothesesTab,
    bool ShowRiskSummaryCard,
    bool ShowResultSummaryCard)
{
    /// <summary>Дефолты по семье, если в TOML нет переопределений и нет наследуемого родителя.</summary>
    public static UiModeCapabilities DefaultsForFamily(UiModeFamily family)
    {
        var balanced = family.IsBalancedFamily();
        var power = family.IsPowerFamily();
        var debug = family.IsDebugFamily();
        var focus = family.IsFocusFamily();
        var agentChat = family.IsAgentChatFamily();

        return new UiModeCapabilities(
            ShowQuickActions: balanced,
            ShowAgentOperationsBlock: balanced,
            ShowAgentTrace: power,
            ShowPowerTelemetry: power,
            ShowPowerTelemetryOnTerminalTab: false,
            PowerTelemetryMainGridColumnSpan: power ? 3 : 5,
            ShowInstrumentationTabs: focus || balanced || power || agentChat || debug,
            ShowHypothesesTab: debug,
            ShowRiskSummaryCard: !focus && !agentChat,
            ShowResultSummaryCard: !focus && !agentChat);
    }
}
