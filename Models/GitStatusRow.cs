namespace CascadeIDE.Models;

/// <summary>Строка <c>git status --short</c> для списка в панели Git (путь относительно корня репозитория).</summary>
public sealed class GitStatusRow
{
    public string RawLine { get; init; } = "";

    /// <summary>Путь для <c>git diff --</c> и т.п.; пусто для служебных строк.</summary>
    public string RelativePath { get; init; } = "";

    public bool HasPath => !string.IsNullOrWhiteSpace(RelativePath);

    /// <summary>Неотслеживаемый файл (<c>??</c>).</summary>
    public bool IsUntracked { get; init; }

    /// <summary>В индексе есть staged-изменения (первая колонка статуса не пробел).</summary>
    public bool HasStagedChanges { get; init; }

    /// <summary>Путь совпадает с записью submodule в <c>.gitmodules</c> (gitlink в родителе).</summary>
    public bool IsSubmodulePath { get; init; }
}
