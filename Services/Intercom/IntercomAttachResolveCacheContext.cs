#nullable enable

namespace CascadeIDE.Services.Intercom;

/// <summary>Контекст для L1/L2 кэша attach-resolve (ADR 0135).</summary>
public readonly record struct IntercomAttachResolveCacheContext(
    string? WorkspaceRoot,
    string? SolutionPath,
    string? RelativePath,
    string IndexDirectoryRelative = ".hybrid-codebase-index")
{
    public string ScopeKey => IntercomAttachResolveScopeKey.From(WorkspaceRoot, SolutionPath);

    public static IntercomAttachResolveCacheContext From(
        string? workspaceRoot,
        string? solutionPath,
        string? relativePath,
        string? indexDirectoryRelative = null) =>
        new(
            workspaceRoot,
            solutionPath,
            relativePath,
            string.IsNullOrWhiteSpace(indexDirectoryRelative)
                ? ".hybrid-codebase-index"
                : indexDirectoryRelative.Trim());
}
