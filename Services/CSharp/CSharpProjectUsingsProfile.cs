using System.Text;
using System.Xml.Linq;

namespace CascadeIDE.Services;

/// <summary>Reads <c>ImplicitUsings</c>, <c>Using Include/Remove</c> from a <c>.csproj</c>.</summary>
public sealed record CSharpProjectUsingsProfile(
    string ProjectPath,
    string? SdkAttribute,
    bool ImplicitUsingsEnabled,
    IReadOnlyList<string> UsingIncludes,
    IReadOnlyList<string> UsingRemoves)
{
    public static CSharpProjectUsingsProfile? TryLoad(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            return null;

        try
        {
            var doc = XDocument.Load(projectPath);
            var root = doc.Root;
            if (root is null)
                return null;

            var sdk = root.Attribute("Sdk")?.Value;
            var implicitUsings = ReadImplicitUsings(root);
            var includes = ReadUsingItems(root, "Include");
            var removes = ReadUsingItems(root, "Remove");
            return new CSharpProjectUsingsProfile(
                projectPath,
                sdk,
                implicitUsings,
                includes,
                removes);
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<string> ResolveNamespaces()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ns in CSharpSdkImplicitUsingsCatalog.ForProjectSdk(SdkAttribute, ImplicitUsingsEnabled))
            set.Add(ns);
        foreach (var ns in UsingIncludes)
            set.Add(ns);
        foreach (var ns in UsingRemoves)
            set.Remove(ns);
        return set.OrderBy(static n => n, StringComparer.Ordinal).ToList();
    }

    private static bool ReadImplicitUsings(XElement root)
    {
        var value = ReadProperty(root, "ImplicitUsings");
        if (string.IsNullOrWhiteSpace(value))
            return SdkPresent(root);
        return value.Equals("enable", StringComparison.OrdinalIgnoreCase)
               || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SdkPresent(XElement root) =>
        !string.IsNullOrWhiteSpace(root.Attribute("Sdk")?.Value);

    private static string? ReadProperty(XElement root, string name)
    {
        foreach (var pg in root.Elements("PropertyGroup"))
        {
            var el = pg.Element(name);
            if (el is not null)
                return el.Value.Trim();
        }

        return null;
    }

    private static List<string> ReadUsingItems(XElement root, string attributeName)
    {
        var list = new List<string>();
        foreach (var item in root.Elements("ItemGroup").SelectMany(g => g.Elements("Using")))
        {
            var value = item.Attribute(attributeName)?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                list.Add(value);
        }

        return list;
    }
}
