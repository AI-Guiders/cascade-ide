namespace CascadeIDE.Features.Editor.Application.Monaco;

public enum EditorNavigationPresentation
{
    RevealTransient,
    RevealPersistent,
    SelectAndReveal,
    ScrollOnly,
}

public enum EditorNavigationSource
{
    Mcp,
    NavigationMap,
    Crs,
    Intercom,
    Markdown,
    ChatDraft,
    Forge,
    MagicLink,
    Debug,
    Other,
}

/// <summary>Resolved navigation into Forward editor buffer (ADR 0163 §2.4).</summary>
public sealed record EditorNavigationTarget(
    string FilePath,
    int StartLine,
    int EndLine,
    int? StartColumn = null,
    int? EndColumn = null,
    string? MemberKey = null,
    EditorNavigationPresentation Presentation = EditorNavigationPresentation.SelectAndReveal,
    int? DurationMs = null,
    EditorNavigationSource Source = EditorNavigationSource.Other);
