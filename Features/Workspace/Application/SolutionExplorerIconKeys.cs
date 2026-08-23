namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Канонические имена SVG для SE (ADR 0167 §2.10; assets из vscode-icons, MIT).</summary>
public static class SolutionExplorerIconKeys
{
    private static readonly HashSet<string> NodeKeys =
        new(StringComparer.OrdinalIgnoreCase) { "solution", "folder", "file" };

    public static string ResolveAssetName(string iconKey) =>
        ResolveAssetName(iconKey, powerMonochrome: false);

    public static string ResolveAssetName(string iconKey, bool powerMonochrome)
    {
        _ = powerMonochrome;
        if (string.IsNullOrWhiteSpace(iconKey))
            return "file";

        if (NodeKeys.Contains(iconKey))
            return iconKey;

        if (iconKey.StartsWith("file_", StringComparison.OrdinalIgnoreCase))
        {
            var ext = iconKey[5..];
            return ext switch
            {
                "csproj" => "csproj",
                "fsproj" => "fsproj",
                "vbproj" => "vbproj",
                "cs" => "cs",
                "axaml" => "axaml",
                "json" => "json",
                "md" => "md",
                "xml" => "xml",
                "txt" => "txt",
                "toml" => "toml",
                "sln" or "slnx" => "solution",
                _ => KnownExtensionOrFile(ext),
            };
        }

        return iconKey.Equals("project", StringComparison.OrdinalIgnoreCase) ? "csproj" : "file";
    }

    private static string KnownExtensionOrFile(string ext) =>
        ext is "cs" or "axaml" or "json" or "md" or "xml" or "txt" or "toml"
            or "js" or "ts" or "css" or "html" or "py" or "yaml" or "sh" or "ps1"
            or "sql" or "go" or "scss" or "less" or "bat" or "svg" or "png"
            ? ext
            : "file";
}
