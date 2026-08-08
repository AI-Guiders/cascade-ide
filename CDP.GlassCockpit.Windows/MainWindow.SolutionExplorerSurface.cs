#nullable enable

using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Models;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SolutionExplorer — paints CIDE <see cref="SolutionItem"/> tree (SolutionParser SSOT).</summary>
public partial class MainWindow
{
    const int MaxTreeNodes = 400;

    void RefreshSolutionExplorerTree()
    {
        if (MfdSolutionExplorerTree is null)
            return;

        MfdSolutionExplorerTree.Items.Clear();

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        if (!string.Equals(page, "SolutionExplorer", StringComparison.OrdinalIgnoreCase))
            return;

        var root = _session.SolutionRoot;
        if (root is null)
            return;

        var budget = MaxTreeNodes;
        foreach (var child in root.Children)
        {
            if (budget <= 0)
                break;
            MfdSolutionExplorerTree.Items.Add(ToTreeItem(child, ref budget));
        }

        // Standalone / flat: if root itself is the only useful node (rare), still show children of root.
        if (MfdSolutionExplorerTree.Items.Count == 0 && root.Children.Count == 0
            && !string.IsNullOrWhiteSpace(root.FullPath) && File.Exists(root.FullPath))
        {
            MfdSolutionExplorerTree.Items.Add(new TreeViewItem
            {
                Header = root.Title,
                Tag = root.FullPath,
            });
        }
    }

    static TreeViewItem ToTreeItem(SolutionItem item, ref int budget)
    {
        budget--;
        var node = new TreeViewItem
        {
            Header = item.Title,
            Tag = item.FullPath,
            IsExpanded = item.IsExpanded,
        };

        foreach (var child in item.Children)
        {
            if (budget <= 0)
                break;
            node.Items.Add(ToTreeItem(child, ref budget));
        }

        return node;
    }

    void MfdSolutionExplorerTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (MfdSolutionExplorerTree?.SelectedItem is not TreeViewItem { Tag: string path }
            || string.IsNullOrWhiteSpace(path)
            || !File.Exists(path))
            return;

        OpenCodeFile(path);
        e.Handled = true;
    }
}
