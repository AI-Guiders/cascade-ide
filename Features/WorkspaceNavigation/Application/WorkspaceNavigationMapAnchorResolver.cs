#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Services;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Выбор якорного .cs для карты намерений, когда в редакторе нет активного файла (Intercom / чат на Forward).
/// </summary>
[ComputingUnit]
public static class WorkspaceNavigationMapAnchorResolver
{
    private static readonly string[] PreferredEntryFileNames =
    [
        "Program.cs",
        "App.axaml.cs",
        "Startup.cs",
        "MainWindow.axaml.cs"
    ];

    public static string? Resolve(
        string? currentPath,
        IReadOnlyList<string> openDocumentPaths,
        IReadOnlyList<string> solutionFilePaths)
    {
        var known = BuildKnownCsSet(solutionFilePaths);
        if (known.Count == 0)
            return null;

        if (TryResolveKnownCs(currentPath, known, out var fromCurrent))
            return fromCurrent;

        foreach (var open in openDocumentPaths)
        {
            if (TryResolveKnownCs(open, known, out var fromOpen))
                return fromOpen;
        }

        var csFiles = known.OrderBy(static p => p, StringComparer.OrdinalIgnoreCase).ToList();
        return PreferEntryPoint(csFiles) ?? csFiles[0];
    }

    private static HashSet<string> BuildKnownCsSet(IReadOnlyList<string> solutionFilePaths)
    {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in solutionFilePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;
            if (McpSolutionTree.IsBuildArtifactPath(path))
                continue;

            try
            {
                known.Add(CanonicalFilePath.Normalize(path));
            }
            catch
            {
                // skip invalid paths from tree
            }
        }

        return known;
    }

    private static bool TryResolveKnownCs(string? path, HashSet<string> known, out string? resolved)
    {
        resolved = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;
        if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var normalized = CanonicalFilePath.Normalize(path);
            if (!known.Contains(normalized))
                return false;
            resolved = normalized;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? PreferEntryPoint(IReadOnlyList<string> csFiles)
    {
        foreach (var name in PreferredEntryFileNames)
        {
            var hit = csFiles.FirstOrDefault(p =>
                string.Equals(Path.GetFileName(p), name, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
                return hit;
        }

        return null;
    }
}
