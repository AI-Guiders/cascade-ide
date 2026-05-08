namespace CascadeIDE.Models;

public sealed class HybridIndexSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Index directory name under workspace root (same as external MCP). Default: <c>.hybrid-codebase-index</c>.</summary>
    public string IndexDir { get; set; } = ".hybrid-codebase-index";

    public int DebounceMs { get; set; } = 750;

    public bool AutoReindexOnSolutionOpen { get; set; } = true;

    public bool WatchFiles { get; set; } = true;

    /// <summary>
    /// Controls how the index scope key is computed.
    /// Values: <c>workspace</c> or <c>workspace+solution</c>.
    /// </summary>
    public string ScopeMode { get; set; } = "workspace+solution";

    public bool PauseWhenMcpStdioHost { get; set; }
}

