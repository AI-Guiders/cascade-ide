using System.Collections.Concurrent;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CascadeIDE.Services;

/// <summary>
/// Resolves global usings for ad-hoc Roslyn compilations: MSBuild <c>GlobalUsings.g.cs</c>,
/// project <c>GlobalUsings.cs</c>, then SDK implicit + csproj <c>Using</c> items.
/// </summary>
public static class CSharpProjectGlobalUsingsResolver
{
    private sealed record CacheEntry(long ProjectTicks, long GeneratedTicks, SyntaxTree Tree);

    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static SyntaxTree ResolveGlobalUsingsTree(string sourceFilePath)
    {
        var projectPath = FindOwningProjectFile(sourceFilePath);
        if (projectPath is null)
            return CreateTree(CSharpSdkImplicitUsingsCatalog.ForProjectSdk("Microsoft.NET.Sdk", implicitUsingsEnabled: true), "__cascade_global_usings__.g.cs");

        var projectTicks = File.GetLastWriteTimeUtc(projectPath).Ticks;
        var generatedPath = TryFindGeneratedGlobalUsingsPath(projectPath);
        var generatedTicks = generatedPath is not null && File.Exists(generatedPath)
            ? File.GetLastWriteTimeUtc(generatedPath).Ticks
            : 0L;

        if (Cache.TryGetValue(projectPath, out var cached)
            && cached.ProjectTicks == projectTicks
            && cached.GeneratedTicks == generatedTicks)
        {
            return cached.Tree;
        }

        var tree = BuildTree(projectPath, generatedPath);
        Cache[projectPath] = new CacheEntry(projectTicks, generatedTicks, tree);
        return tree;
    }

    public static void ClearCache() => Cache.Clear();

    internal static string? FindOwningProjectFile(string sourceFilePath)
    {
        var dir = Path.GetDirectoryName(sourceFilePath);
        while (!string.IsNullOrWhiteSpace(dir))
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    var projects = Directory.EnumerateFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).ToList();
                    if (projects.Count == 1)
                        return projects[0];
                    if (projects.Count > 1)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                        var match = projects.FirstOrDefault(p =>
                            string.Equals(Path.GetFileNameWithoutExtension(p), fileName, StringComparison.OrdinalIgnoreCase));
                        if (match is not null)
                            return match;
                        return projects.OrderBy(static p => p.Length).First();
                    }
                }

                dir = Path.GetDirectoryName(dir);
            }
            catch
            {
                dir = Path.GetDirectoryName(dir);
            }
        }

        return null;
    }

    internal static string? TryFindGeneratedGlobalUsingsPath(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(projectDir))
            return null;

        var objDir = Path.Combine(projectDir, "obj");
        if (!Directory.Exists(objDir))
            return null;

        try
        {
            return Directory.EnumerateFiles(objDir, "GlobalUsings.g.cs", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .Select(info => info.FullName)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static SyntaxTree BuildTree(string projectPath, string? generatedPath)
    {
        if (!string.IsNullOrWhiteSpace(generatedPath) && File.Exists(generatedPath))
        {
            try
            {
                return CSharpSyntaxTree.ParseText(File.ReadAllText(generatedPath), path: generatedPath);
            }
            catch
            {
                // Fall through to synthesis.
            }
        }

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var authoredPath = Path.Combine(projectDir, "GlobalUsings.cs");
        if (File.Exists(authoredPath))
        {
            try
            {
                return CSharpSyntaxTree.ParseText(File.ReadAllText(authoredPath), path: authoredPath);
            }
            catch
            {
                // Fall through to synthesis.
            }
        }

        var profile = CSharpProjectUsingsProfile.TryLoad(projectPath);
        if (profile is null)
            return CreateTree([], "__cascade_global_usings__.g.cs");

        var merged = profile.ResolveNamespaces();
        var virtualPath = Path.Combine(projectDir, "obj", "__cascade_synthesized_global_usings__.g.cs");
        return CreateTree(merged, virtualPath);
    }

    private static SyntaxTree CreateTree(IReadOnlyList<string> namespaces, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated by CascadeIDE — SDK/csproj global usings />");
        foreach (var ns in namespaces)
            sb.AppendLine($"global using {ns};");

        return CSharpSyntaxTree.ParseText(sb.ToString(), path: path);
    }
}
