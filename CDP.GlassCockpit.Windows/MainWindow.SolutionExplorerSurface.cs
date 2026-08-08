#nullable enable

using System.Collections;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Models;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SolutionExplorer — ItemsSource = <see cref="GlassSolutionExplorerFace"/> / SolutionItem SSOT.</summary>
public partial class MainWindow
{
    void RefreshSolutionExplorerTree()
    {
        if (MfdSolutionExplorerTree is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        if (!GlassSolutionExplorerFace.IsSePage(page))
        {
            MfdSolutionExplorerTree.ItemsSource = null;
            return;
        }

        EnsureSolutionTreeForFace();
        MfdSolutionExplorerTree.ItemsSource = GlassSolutionExplorerFace.ResolveItems(_session.SolutionRoot);
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
