namespace CascadeIDE.Models;

/// <summary>
/// Зерно subgraph control-flow в PFD/SM (<c>[code_navigation_map].control_flow_grain</c>).
/// <see cref="Intent"/> соответствует ADR 0053 (смысл метода без микро-CFG).
/// </summary>
public static class CodeNavigationMapControlFlowGrainKind
{
    public const string Intent = "intent";

    /// <summary>Пошаговый CFG по выражениям (старое поведение).</summary>
    public const string Detailed = "detailed";

    /// <summary>Нормализует TOML/override; неизвестные значения → <see cref="Intent"/>.</summary>
    public static string Normalize(string? value)
    {
        var v = (value ?? "").Trim();
        if (string.Equals(v, Detailed, StringComparison.OrdinalIgnoreCase))
            return Detailed;
        return Intent;
    }

    /// <summary>Удобство для роутинга билдера subgraph.</summary>
    public static bool IsDetailed(string? normalized) =>
        string.Equals(normalized, Detailed, StringComparison.Ordinal);

    public static bool IsIntent(string? normalized) =>
        string.Equals(normalized, Intent, StringComparison.Ordinal);
}
