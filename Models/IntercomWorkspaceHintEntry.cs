namespace CascadeIDE.Models;

/// <summary>Local workspace hint per normalized repo URL (ADR 0144 §2.3.1).</summary>
public sealed class IntercomWorkspaceHintEntry
{
    public string TeamId { get; set; } = "";

    public string ProjectId { get; set; } = "";

    public string UpdatedAtUtc { get; set; } = "";

    /// <summary>resolve | manual | manifest_strangler</summary>
    public string Source { get; set; } = "";
}
