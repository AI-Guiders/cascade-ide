namespace CascadeIDE.Models;

/// <summary>TOML <c>[code_navigation_map].control_flow_main_axis</c>: главная ось потока на мини-карте control-flow.</summary>
public static class CodeNavigationMapControlFlowMainAxisKind
{
    /// <summary>Выбор оси по вьюпорту и форме графа.</summary>
    public const string Auto = "auto";

    /// <summary>Всегда главный поток сверху вниз.</summary>
    public const string Vertical = "vertical";

    /// <summary>Всегда главный поток слева направо.</summary>
    public const string Horizontal = "horizontal";

    /// <inheritdoc cref="Normalize" />
    public static string Normalize(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v is Vertical or Horizontal ? v : Auto;
    }
}
