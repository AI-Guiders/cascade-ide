#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD RelatedFiles — WNM-shaped list feed (Avalonia RelatedFilesMfd peel v1).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassRelatedFilesFeed.Item> _relatedItems = new();

    void RefreshMfdRelatedVisibility()
    {
        if (MfdRelatedFilesHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "RelatedFiles", StringComparison.OrdinalIgnoreCase);
        MfdRelatedFilesHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && RelatedList is not null && !ReferenceEquals(RelatedList.ItemsSource, _relatedItems))
            RelatedList.ItemsSource = _relatedItems;

        if (show && _relatedItems.Count == 0)
            RefreshRelatedItems();
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
        OpenCodeFile(item.FullPath);
        StatusText.Text = $"glass · related · {Path.GetFileName(item.FullPath)}";
    }

    void RefreshRelatedItems()
    {
        _relatedItems.Clear();
        foreach (var i in GlassRelatedFilesFeed.Collect(_session.WorkspaceRoot, _editorPath))
            _relatedItems.Add(i);
        if (RelatedStatusLabel is not null)
            RelatedStatusLabel.Text =
                $"related · {_relatedItems.Count} · anchor {Path.GetFileName(_editorPath ?? "?")}";
    }
}
