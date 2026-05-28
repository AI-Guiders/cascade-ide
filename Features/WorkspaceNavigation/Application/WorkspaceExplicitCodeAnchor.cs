#nullable enable

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Явная запись из workspace TOML / sidecar (ADR 0156).</summary>
public sealed record WorkspaceExplicitCodeAnchor(
    string DocPath,
    CodeAnchor CodeAnchor,
    string Kind,
    string Provenance);
