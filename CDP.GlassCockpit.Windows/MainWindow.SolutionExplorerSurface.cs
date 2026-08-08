#nullable enable

using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SolutionExplorer — ItemsSource = <see cref="GlassSolutionExplorerFace"/> / SolutionItem SSOT + filter.</summary>
public partial class MainWindow
{
    readonly ObservableCollection<SolutionItem> _seSourceRoots = new();
    readonly ObservableCollection<SolutionItem> _seDisplayRoots = new();
    readonly WorkspaceFileIndex _seFileIndex = new();

    void RefreshSolutionExplorerTree()
    {
        if (MfdSolutionExplorerTree is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var on = GlassSolutionExplorerFace.IsSePage(page);
        if (MfdSolutionExplorerHost is not null)
            MfdSolutionExplorerHost.Visibility = on ? Visibility.Visible : Visibility.Collapsed;

        if (!on)
        {
            MfdSolutionExplorerTree.ItemsSource = null;
            return;
        }

        EnsureSolutionTreeForFace();
        _seSourceRoots.Clear();
        foreach (var item in GlassSolutionExplorerFace.ResolveItems(_session.SolutionRoot))
            _seSourceRoots.Add(item);

        ApplySolutionExplorerFilter();
    }

    void ApplySolutionExplorerFilter()
    {
        var filter = MfdSolutionExplorerFilter?.Text ?? "";
        _seFileIndex.Invalidate(_seSourceRoots, _session.SolutionPath, _session.WorkspaceRoot ?? "");
        SolutionExplorerTreeFilter.RebuildDisplayRoots(
            _seSourceRoots,
            _seDisplayRoots,
            filter,
            _seFileIndex);
        if (MfdSolutionExplorerTree is not null)
            MfdSolutionExplorerTree.ItemsSource = _seDisplayRoots;
    }

    void MfdSolutionExplorerFilter_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!GlassSolutionExplorerFace.IsSePage(CurrentMfdPage()))
            return;
        ApplySolutionExplorerFilter();
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
