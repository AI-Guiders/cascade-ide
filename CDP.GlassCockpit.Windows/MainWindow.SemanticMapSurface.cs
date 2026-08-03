#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SemanticMap — Skia radial graph + list (RelatedFiles heuristic).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassRelatedFilesHeuristic.Item> _semanticItems = new();
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
        if (SemanticList?.SelectedItem is not GlassRelatedFilesHeuristic.Item item)
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
        _semanticItems.Clear();
        foreach (var i in GlassRelatedFilesHeuristic.Collect(_session.WorkspaceRoot, _editorPath, max: 96))
            _semanticItems.Add(i);
        PushSemanticGraph();
        if (SemanticStatusLabel is not null)
            SemanticStatusLabel.Text = $"semantic · skia {_semanticItems.Count} · click node";
    }

    void PushSemanticGraph()
    {
        EnsureSemanticSkiaWired();
        SemanticSkia?.SetGraph(_editorPath, _semanticItems);
    }
}
