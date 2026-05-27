#nullable enable

namespace CascadeIDE.ViewModels;

/// <summary>Клик по узлу карты намерений: файл + опциональный code anchor (строки).</summary>
public sealed record CodeNavigationMapNodeNavigatePayload(
    string FullPath,
    int? LineStart,
    int? LineEnd,
    string? LegendLine,
    string Kind);
