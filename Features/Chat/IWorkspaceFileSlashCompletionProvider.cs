#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Подсказки файлов solution/workspace для slash с <see cref="SlashCompletionKind.WorkspaceFiles"/> (ADR 0125).</summary>
public interface IWorkspaceFileSlashCompletionProvider
{
    IReadOnlyList<WorkspaceFileSlashMatch> GetMatches(string pathPrefix, int limit);
}

/// <param name="InsertPath">Относительный путь для вставки в поле (от корня solution/workspace).</param>
public readonly record struct WorkspaceFileSlashMatch(string InsertPath, string Help);
