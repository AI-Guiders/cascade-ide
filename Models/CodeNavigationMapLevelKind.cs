namespace CascadeIDE.Models;

/// <summary>Уровень карты в PFD (<c>[code_navigation_map].depth</c>).</summary>
public static class CodeNavigationMapLevelKind
{
    public const string File = "file";
    public const string ControlFlow = "controlFlow";

    public static string Normalize(string? value) => CodeNavigationMapSettings.NormalizeDepth(value);
}
