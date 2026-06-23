namespace CascadeIDE.Features.Editor.Application.Monaco;

public interface IEditorNavigationService
{
    Task<bool> NavigateAsync(EditorNavigationTarget target, CancellationToken cancellationToken = default);

    bool TryNavigateReveal(
        string? filePath,
        int startLine,
        int endLine,
        int? durationMs,
        EditorNavigationSource source = EditorNavigationSource.Mcp);

    bool TryNavigateGoTo(
        string? filePath,
        int line,
        int column,
        int? endLine = null,
        int? endColumn = null,
        EditorNavigationSource source = EditorNavigationSource.Mcp);
}
