namespace CascadeIDE.Models.Shell;

/// <summary>
/// Полноширинный режим <b>оболочки</b> в колонке зоны внимания (ADR 0088 §2.0). На Pfd расклад задаётся отдельно — <see cref="IPfdLayout"/>, не «страница».
/// </summary>
public interface IShellPage
{
    /// <summary>Стабильный id для логов, снапшотов и MCP (например <c>mfd.chat</c>).</summary>
    string ShellSurfaceId { get; }
}
