#nullable enable
using System.Collections.Generic;
using System.IO;
using CascadeIDE.Models;

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Glass SE Face policy: ItemsSource rows from <see cref="SolutionItem"/> SSOT.
/// Never Avalonia FormatMfdStub peel — empty session still binds a Face placeholder.
/// </summary>
public static class GlassSolutionExplorerFace
{
    public const string EmptyTitle = "no solution · Ctrl+O → open .sln / folder";
    public const string MfdPage = "SolutionExplorer";

    public static bool IsSePage(string? mfdPage) =>
        string.Equals(mfdPage, MfdPage, StringComparison.OrdinalIgnoreCase);

    /// <summary>True → host TreeView; false → may use MfdBody stub (never for SE).</summary>
    public static bool PreferTreeHost(string? mfdPage) => IsSePage(mfdPage);

    /// <summary>Rows for TreeView.ItemsSource. Mutates <see cref="SolutionItem.IsExpanded"/> on project roots.</summary>
    public static IReadOnlyList<SolutionItem> ResolveItems(SolutionItem? root)
    {
        if (root is null)
            return [SolutionItem.CreateFolder(EmptyTitle)];

        ExpandProjectRoots(root);

        // Standalone file node with no Children — bind the root itself.
        if (root.Children.Count == 0
            && !string.IsNullOrWhiteSpace(root.FullPath)
            && File.Exists(root.FullPath))
            return [root];

        if (root.Children.Count > 0)
            return root.Children;

        return [root];
    }

    static void ExpandProjectRoots(SolutionItem root)
    {
        foreach (var child in root.Children)
        {
            if (child.Children.Count > 0)
                child.IsExpanded = true;
        }
    }
}
