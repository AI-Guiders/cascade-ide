#nullable enable

using System.Text.Json.Serialization;

namespace CascadeIDE.Features.Workspace;

/// <summary>CASA agent field store paths (<c>[workspace.casa_field]</c>, IDE bridge v1).</summary>
public sealed class RepositoryCasaFieldToml
{
    /// <summary>Repo-relative or absolute path to agent store (contains field_state.json).</summary>
    public string? StoreDir { get; set; }

    /// <summary>Repo-relative path to casa-ontology-payload root (optional).</summary>
    public string? PayloadRepo { get; set; }
}
