namespace CascadeIDE.Models;

/// <summary>Укладка графа связанных файлов (уровень <c>file</c>). TOML: <c>[code_navigation_map].related_graph_layout</c>.</summary>
public static class CodeNavigationMapRelatedGraphLayoutKind
{
    public const string Radial = "radial";
    public const string TopDown = "top_down";
    public const string BottomUp = "bottom_up";

    public static string Normalize(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v switch
        {
            TopDown => TopDown,
            BottomUp => BottomUp,
            Radial => Radial,
            _ => Radial
        };
    }

    public static bool IsHierarchy(string? normalized) =>
        normalized is TopDown or BottomUp;
}
