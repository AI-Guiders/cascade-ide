#nullable enable

using HybridCodebaseIndex.Core.Indexing;

namespace CascadeIDE.Services.Intercom;

/// <summary>
/// Строит Intercom symbol sidecar из текста, уже прочитанного HCI reindex (ADR 0135, 0141).
/// Один проход по диску — в Core.
/// </summary>
public sealed class IntercomSymbolLineHciReindexObserver : ICodebaseIndexReindexObserver
{
    private readonly string? _solutionPath;
    private readonly string _indexDirectoryRelative;

    public IntercomSymbolLineHciReindexObserver(string? solutionPath, string indexDirectoryRelative)
    {
        _solutionPath = string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim();
        _indexDirectoryRelative = string.IsNullOrWhiteSpace(indexDirectoryRelative)
            ? ".hybrid-codebase-index"
            : indexDirectoryRelative.Trim();
    }

    public void OnFileIndexed(IndexedFileEvent file)
    {
        if (!file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            return;

        var cache = new IntercomAttachResolveCacheContext(
            file.WorkspaceRoot,
            _solutionPath,
            file.RelativePathUnix,
            _indexDirectoryRelative);

        IntercomSymbolLineIndexBuilder.IndexFile(
            cache,
            file.AbsolutePath,
            file.RelativePathUnix,
            file.Text,
            file.LastWriteUtcTicks);
    }
}
