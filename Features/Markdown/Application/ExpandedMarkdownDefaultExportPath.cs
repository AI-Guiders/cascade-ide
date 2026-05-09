using CascadeIDE.Models;

namespace CascadeIDE.Features.Markdown.Application;

public static class ExpandedMarkdownDefaultExportPath
{
    public static string Resolve(string sourcePath)
    {
        try
        {
            var full = CanonicalFilePath.Normalize(sourcePath);
            var dir = Path.GetDirectoryName(full) ?? Directory.GetCurrentDirectory();
            var name = Path.GetFileNameWithoutExtension(full);
            if (string.IsNullOrWhiteSpace(name))
                name = "export";
            return Path.Combine(dir, $"{name}.expanded.md");
        }
        catch
        {
            return "export.expanded.md";
        }
    }
}
