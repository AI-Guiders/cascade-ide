#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SemanticMap — Skia multi-hop graph + list.</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassSemanticMapGraph.Node> _semanticItems = new();
    GlassSemanticMapGraph.Graph _semanticGraph = new(null, [], []);
    bool _semanticSkiaWired;

    void RefreshMfdSemanticVisibility()
    {
        if (MfdSemanticMapHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "SemanticMap", StringComparison.OrdinalIgnoreCase);
        MfdSemanticMapHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        EnsureSemanticSkiaWired();

        if (show && SemanticList is not null && !ReferenceEquals(SemanticList.ItemsSource, _semanticItems))
            SemanticList.ItemsSource = _semanticItems;

        if (show && _semanticItems.Count == 0)
            RefreshSemanticItems();
        else if (show)
            PushSemanticGraph();
    }

    bool IsSemanticHostActive()
    {
        if (MfdSemanticMapHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "SemanticMap", StringComparison.OrdinalIgnoreCase)
               && MfdSemanticMapHost.Visibility == Visibility.Visible;
    }

    internal void SemanticRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshSemanticItems();

    internal void SemanticList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SemanticList?.SelectedItem is not GlassSemanticMapGraph.Node item)
            return;
        OpenSemanticNode(item.FilePath);
    }

    void EnsureSemanticSkiaWired()
    {
        if (_semanticSkiaWired || SemanticSkia is null)
            return;
        SemanticSkia.NodeActivated += OpenSemanticNode;
        _semanticSkiaWired = true;
    }

    void OpenSemanticNode(string path)
    {
        OpenCodeFile(path);
        StatusText.Text = $"glass · semantic · {Path.GetFileName(path)}";
    }

    void RefreshSemanticItems()
    {
        _semanticGraph = GlassSemanticMapGraph.Collect(_session.WorkspaceRoot, _editorPath, maxNodes: 96);
        _semanticItems.Clear();
        foreach (var n in _semanticGraph.Nodes)
            _semanticItems.Add(n);
        PushSemanticGraph();
        if (SemanticStatusLabel is not null)
        {
            var hops = _semanticGraph.Nodes.Count > 0
                ? $"h{_semanticGraph.Nodes.Max(n => n.Hop)}"
                : "h0";
            SemanticStatusLabel.Text = $"semantic · skia {_semanticGraph.Nodes.Count} · {hops} · {_semanticGraph.Edges.Count}e";
        }
    }

    void PushSemanticGraph()
    {
        EnsureSemanticSkiaWired();
        SemanticSkia?.SetGraph(_semanticGraph);
    }
}
