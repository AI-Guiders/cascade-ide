namespace CascadeIDE.Features.Search.Application;

/// <summary>Строка навигации палитры (файл или совпадение ripgrep) без UI-модели.</summary>
public readonly record struct CommandPaletteGoToNavRowPresentation(
    string Title,
    string SubtitleCategory,
    string FullPath,
    int Line,
    int Column,
    string PrefixHint);
