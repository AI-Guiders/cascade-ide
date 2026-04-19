namespace CascadeIDE.Models;

/// <summary>Уровень Semantic Map в PFD (<c>[semantic_map].depth</c>).</summary>
public static class SemanticMapLevelKind
{
    public const string File = "file";
    public const string ControlFlow = "controlFlow";

    public static string Normalize(string? value) => SemanticMapSettings.NormalizeDepth(value);
}
