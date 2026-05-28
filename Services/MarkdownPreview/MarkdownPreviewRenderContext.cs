#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Контекст рендера preview: источник, workspace, навигация, якоря.</summary>
public sealed class MarkdownPreviewRenderContext
{
    public MarkdownPreviewRenderContext(
        string? sourceFilePath,
        string? workspaceRoot,
        Action<string>? openLink = null,
        MarkdownPreviewAnchorRegistry? anchors = null)
    {
        SourceFilePath = string.IsNullOrWhiteSpace(sourceFilePath) ? null : sourceFilePath.Trim();
        WorkspaceRoot = string.IsNullOrWhiteSpace(workspaceRoot) ? null : workspaceRoot.Trim();
        OpenLink = openLink;
        Anchors = anchors ?? new MarkdownPreviewAnchorRegistry();
    }

    public string? SourceFilePath { get; }

    public string? WorkspaceRoot { get; }

    /// <summary>Единый обработчик ссылок: http(s), .md, #fragment, cascade-code-anchor.</summary>
    public Action<string>? OpenLink { get; }

    public MarkdownPreviewAnchorRegistry Anchors { get; }

    /// <summary>Legacy alias для открытия документов без fragment.</summary>
    public Action<string>? OpenDocument => OpenLink is null
        ? null
        : url =>
        {
            var (path, _) = SplitUrl(url);
            if (!string.IsNullOrWhiteSpace(path))
                OpenLink(path);
        };

    /// <summary>Путь для <see cref="WorkspaceMarkdownPreviewOpener"/> (repo-relative или absolute).</summary>
    public string? ResolveNavigateTarget(string? url)
    {
        var (path, _) = SplitUrl(url);
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var trimmed = path.Trim();
        if (trimmed.StartsWith(MarkdownCodeAnchorPreviewExpander.UriScheme, StringComparison.OrdinalIgnoreCase))
            return null;

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        string? absolute;
        if (Path.IsPathRooted(trimmed))
        {
            absolute = trimmed;
        }
        else if (SourceFilePath is not null)
        {
            var dir = Path.GetDirectoryName(SourceFilePath);
            if (string.IsNullOrWhiteSpace(dir))
                absolute = null;
            else
                absolute = Path.GetFullPath(Path.Combine(dir, trimmed.Replace('/', Path.DirectorySeparatorChar)));
        }
        else
        {
            absolute = null;
        }

        if (absolute is not null && File.Exists(absolute) && WorkspaceRoot is not null)
        {
            var rel = WorkspaceAdrMapResolver.TryComputeRepoRelativePath(WorkspaceRoot, absolute);
            if (rel is not null)
                return rel;
        }

        if (WorkspaceRoot is not null
            && WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(WorkspaceRoot, trimmed) is not null)
        {
            return trimmed;
        }

        return absolute is not null && File.Exists(absolute) ? absolute : null;
    }

    public static (string? Path, string? Fragment) SplitUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (null, null);

        var trimmed = url.Trim();
        if (trimmed.StartsWith('#'))
            return (null, trimmed[1..]);

        var hash = trimmed.IndexOf('#');
        if (hash < 0)
            return (trimmed, null);

        var path = hash == 0 ? null : trimmed[..hash];
        var fragment = trimmed[(hash + 1)..];
        return (string.IsNullOrWhiteSpace(path) ? null : path, string.IsNullOrWhiteSpace(fragment) ? null : fragment);
    }
}
