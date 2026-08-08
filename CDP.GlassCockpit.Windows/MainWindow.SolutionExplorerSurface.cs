#nullable enable

using System.Collections;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Models;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SolutionExplorer — ItemsSource = CIDE <see cref="SolutionItem"/> tree (SolutionParser SSOT).</summary>
public partial class MainWindow
{
    void RefreshSolutionExplorerTree()
    {
        if (MfdSolutionExplorerTree is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        if (!string.Equals(page, "SolutionExplorer", StringComparison.OrdinalIgnoreCase))
        {
            MfdSolutionExplorerTree.ItemsSource = null;
            return;
        }

        EnsureSolutionTreeForFace();

        var root = _session.SolutionRoot;
        if (root is null)
        {
            // Face empty — still ItemsSource, never FormatMfdStub Avalonia peel.
            MfdSolutionExplorerTree.ItemsSource = new[]
            {
                SolutionItem.CreateFolder("no solution · Ctrl+O → open .sln / folder")
            };
            return;
        }

        ExpandProjectRootsForFace(root);

        // Standalone: single file node with no Children — bind the root itself.
        if (root.Children.Count == 0
            && !string.IsNullOrWhiteSpace(root.FullPath)
            && File.Exists(root.FullPath))
        {
            MfdSolutionExplorerTree.ItemsSource = new[] { root };
            return;
        }

        MfdSolutionExplorerTree.ItemsSource = root.Children.Count > 0
            ? root.Children
            : new[] { root };
    }

    /// <summary>If session has workspace but no tree yet — load .sln/.csproj under it (same SSOT as open).</summary>
    void EnsureSolutionTreeForFace()
    {
        if (_session.SolutionRoot is not null)
            return;

        var hint = GlassSolutionExplorerGlance.TryResolveSlnPath(_session.WorkspaceRoot);
        if (hint is null)
            return;

        _ = _session.SetSolutionOrProjectPath(hint);
    }

    static void ExpandProjectRootsForFace(SolutionItem root)
    {
        foreach (var child in root.Children)
        {
            if (child.Children.Count > 0)
                child.IsExpanded = true;
        }
    }

    static bool SolutionExplorerHasRows(TreeView? tree)
    {
        if (tree?.ItemsSource is not IEnumerable src)
            return tree is { Items.Count: > 0 };

        var e = src.GetEnumerator();
        try
        {
            return e.MoveNext();
        }
        finally
        {
            (e as IDisposable)?.Dispose();
        }
    }

    void MfdSolutionExplorerTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (MfdSolutionExplorerTree?.SelectedItem is not SolutionItem item
            || string.IsNullOrWhiteSpace(item.FullPath)
            || !File.Exists(item.FullPath))
            return;

        OpenCodeFile(item.FullPath);
        e.Handled = true;
    }
}
