#nullable enable

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Документ ссылается на участок кода (reverse anchor, ADR 0156).</summary>
public sealed record DocReverseAnchorMatch(
    string DocPath,
    string DocTitle,
    CodeAnchor CodeAnchor,
    string Excerpt,
    string Provenance,
    int? DocLineHint,
    string Kind = WorkspaceCorrespondenceCodeAnchorsLoader.DefaultKind);
