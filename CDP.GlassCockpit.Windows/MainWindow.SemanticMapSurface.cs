#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SemanticMap — list v1 (Skia graph later; same heuristic as Related).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassRelatedFilesHeuristic.Item> _semanticItems = new();

    void RefreshMfdSemanticVisibility()
    {
        if (MfdSemanticMapHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "SemanticMap", StringComparison.OrdinalIgnoreCase);
        MfdSemanticMapHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && SemanticList is not null && !ReferenceEquals(SemanticList.ItemsSource, _semanticItems))
            SemanticList.ItemsSource = _semanticItems;

        if (show && _semanticItems.Count == 0)
            RefreshSemanticItems();
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
        OpenCodeFile(item.FilePath);
        StatusText.Text = $"glass · semantic · {Path.GetFileName(item.FilePath)}";
    }

    void RefreshSemanticItems()
    {
        _semanticItems.Clear();
        foreach (var i in GlassRelatedFilesHeuristic.Collect(_session.WorkspaceRoot, _editorPath, max: 96))
            _semanticItems.Add(i);
        if (SemanticStatusLabel is not null)
            SemanticStatusLabel.Text = $"semantic · list {_semanticItems.Count} · Skia later";
    }
}
