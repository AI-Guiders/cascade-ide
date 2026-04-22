using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Режим панели карты кода: список, мини-граф или оба (ADR 0039, <c>[code_navigation_map].view</c>).</summary>
public static class CodeNavigationMapPresentationKind
{
    public const string List = "list";
    public const string Graph = "graph";
    public const string Both = "both";

    public static string Normalize(string? value) => CodeNavigationMapSettings.NormalizeView(value);
}
