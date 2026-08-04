#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Glass MFD RelatedFiles — companions instrument: Skia graph + WNM list (Shared-SSOT Q2 blast).
/// </summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassRelatedFilesFeed.Item> _relatedItems = new();
    GlassSemanticMapGraph.Graph _relatedGraph = new(null, [], []);
    bool _relatedSkiaWired;

    void RefreshMfdRelatedVisibility()
    {
        if (MfdRelatedFilesHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "RelatedFiles", StringComparison.OrdinalIgnoreCase);
        MfdRelatedFilesHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        EnsureRelatedSkiaWired();

        if (show && RelatedList is not null && !ReferenceEquals(RelatedList.ItemsSource, _relatedItems))
            RelatedList.ItemsSource = _relatedItems;

        if (show && _relatedItems.Count == 0)
            RefreshRelatedItems();
        else if (show)
            PushRelatedGraph();
    }

    bool IsRelatedHostActive()
    {
        if (MfdRelatedFilesHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "RelatedFiles", StringComparison.OrdinalIgnoreCase)
               && MfdRelatedFilesHost.Visibility == Visibility.Visible;
    }

    internal void RelatedRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshRelatedItems();

    internal void RelatedList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RelatedList?.SelectedItem is not GlassRelatedFilesFeed.Item item)
            return;
        OpenRelatedNode(item.FullPath);
    }

    void EnsureRelatedSkiaWired()
    {
        if (_relatedSkiaWired || RelatedSkia is null)
            return;
        RelatedSkia.NodeActivated += OpenRelatedNode;
        _relatedSkiaWired = true;
    }

    void OpenRelatedNode(string path)
    {
        OpenCodeFile(path);
        StatusText.Text = $"glass · related · {Path.GetFileName(path)}";
    }

    void RefreshRelatedItems()
    {
        _relatedItems.Clear();
        foreach (var i in GlassRelatedFilesFeed.Collect(_session.WorkspaceRoot, _editorPath))
            _relatedItems.Add(i);

        _relatedGraph = GlassSemanticMapGraph.Collect(_session.WorkspaceRoot, _editorPath, maxNodes: 64);
        PushRelatedGraph();

        if (RelatedStatusLabel is not null)
        {
            var hops = _relatedGraph.Nodes.Count > 0
                ? $"h{_relatedGraph.Nodes.Max(n => n.Hop)}"
                : "h0";
            RelatedStatusLabel.Text =
                $"related · list {_relatedItems.Count} · graph {_relatedGraph.Nodes.Count}/{_relatedGraph.Edges.Count}e · {hops} · {Path.GetFileName(_editorPath ?? "?")}";
        }
    }

    void PushRelatedGraph()
    {
        EnsureRelatedSkiaWired();
        RelatedSkia?.SetGraph(_relatedGraph);
    }
}
