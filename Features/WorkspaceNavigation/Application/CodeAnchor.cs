#nullable enable

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Минимальный контракт привязки док↔код (ADR 0128 / 0156), subset <c>AttachmentAnchor</c>.</summary>
public sealed record CodeAnchor(
    string File,
    int? LineStart = null,
    int? LineEnd = null,
    string? MemberKey = null,
    string? SyntaxScope = null);
