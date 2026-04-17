using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Режим панели Semantic Map: список, мини-граф или оба (ADR 0039, <c>[semantic_map].presentation</c>).</summary>
public static class SemanticMapPresentationKind
{
    public const string List = "list";
    public const string Graph = "graph";
    public const string Both = "both";

    public static string Normalize(string? value) => SemanticMapSettings.NormalizePresentation(value);
}
