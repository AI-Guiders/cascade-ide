#nullable enable

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Профиль отрисовки graph-backed surface (CFG vs звезда связанных файлов). ADR 0067.</summary>
public enum GraphLayoutPresentation
{
    CodeControlFlow = 0,
    WorkspaceRelatedFiles = 1
}
