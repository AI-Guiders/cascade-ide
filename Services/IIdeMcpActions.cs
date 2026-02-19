namespace CascadeIDE.Services;

/// <summary>
/// Callbacks from IDE MCP server into the application (UI/ViewModel).
/// </summary>
public interface IIdeMcpActions
{
    void OpenFile(string path);
    void SetBreakpoint(string filePath, int line, string? condition = null);
    void ShowPreview(string title, string content);
    Task<string> RequestConfirmationAsync(string message, CancellationToken cancellationToken = default);
}
