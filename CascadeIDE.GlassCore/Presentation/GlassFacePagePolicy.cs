#nullable enable
namespace CascadeIDE.GlassCore.Presentation;

/// <summary>
/// Face invite path → MFD page (dual-HCI). Explicit <paramref name="mfdOverride"/> wins;
/// else content-kind table; else Editor. Land latch stays path+show_face — page lives here.
/// </summary>
public static class GlassFacePagePolicy
{
    public const string Editor = "Editor";
    public const string MarkdownPreview = "MarkdownPreview";

    /// <summary>
    /// Resolve Face page for an invite. Null/blank override → path kind → Editor.
    /// </summary>
    public static string Resolve(string? path, string? mfdOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(mfdOverride))
            return mfdOverride.Trim();

        return FromPath(path) ?? Editor;
    }

    /// <summary>Content-kind → page; null = default Editor caller.</summary>
    public static string? FromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var ext = Path.GetExtension(path.AsSpan()).ToString();
        if (ext.Length == 0)
            return null;

        return ext.ToLowerInvariant() switch
        {
            ".md" or ".markdown" or ".mdown" => MarkdownPreview,
            _ => null,
        };
    }

    /// <summary>Document Face pages share PreferSurface token <c>m</c> (not world).</summary>
    public static bool IsDocumentFacePage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return false;
        var p = page.Trim();
        return p.Equals(Editor, StringComparison.OrdinalIgnoreCase)
            || p.Equals(MarkdownPreview, StringComparison.OrdinalIgnoreCase);
    }
}
