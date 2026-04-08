#nullable enable

namespace CascadeIDE.Services.Lsp;

/// <summary>file:// URI и пути для LSP (общий слой для C#, Marksman и др.).</summary>
public static class LspFileUri
{
    public static string NormalizePath(string path) =>
        Path.GetFullPath(path);

    public static string PathToFileUri(string fullPath)
    {
        var full = Path.GetFullPath(fullPath);
        return new Uri(full).AbsoluteUri;
    }

    public static bool TryUriToPath(string uri, out string path)
    {
        path = "";
        try
        {
            var u = new Uri(uri);
            path = u.LocalPath;
            if (OperatingSystem.IsWindows() && path.StartsWith('/') && path.Length > 2 && path[2] == ':')
                path = path.TrimStart('/');
            return !string.IsNullOrEmpty(path);
        }
        catch
        {
            return false;
        }
    }
}
