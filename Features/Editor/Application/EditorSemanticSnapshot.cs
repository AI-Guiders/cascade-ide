namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Нормализованные счётчики диагностик по открытому файлу (DAL → проекция HUD, ADR 0103).
/// </summary>
public readonly record struct EditorSemanticSnapshot(
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    int HintCount);
