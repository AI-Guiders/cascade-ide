namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Снимок проекции по файлу после стабилизированного ввода (hi-freq → throttler, ADR 0103).
/// Граница между <see cref="EditorDocumentHudLayer"/> и <see cref="ViewModels.MainWindowViewModel"/> для HUD banner.
/// </summary>
public readonly record struct EditorHudStabilizedContext(
    string FilePath,
    EditorSemanticSnapshot Snapshot);
