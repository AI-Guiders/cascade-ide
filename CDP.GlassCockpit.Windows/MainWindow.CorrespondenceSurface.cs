#nullable enable

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Correspondence — thin FS CRS (Avalonia CRS peel v1).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassCorrespondenceFeed.Item> _crsReverse = new();
    readonly ObservableCollection<GlassCorrespondenceFeed.Item> _crsForward = new();

    void RefreshMfdCorrespondenceVisibility()
    {
        if (MfdCorrespondenceHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Correspondence", StringComparison.OrdinalIgnoreCase);
        MfdCorrespondenceHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
        {
            if (CorrespondenceReverseList is not null
                && !ReferenceEquals(CorrespondenceReverseList.ItemsSource, _crsReverse))
                CorrespondenceReverseList.ItemsSource = _crsReverse;
            if (CorrespondenceForwardList is not null
                && !ReferenceEquals(CorrespondenceForwardList.ItemsSource, _crsForward))
                CorrespondenceForwardList.ItemsSource = _crsForward;
            if (_crsReverse.Count == 0 && _crsForward.Count == 0)
                RefreshCorrespondenceItems();
        }
    }

    bool IsCorrespondenceHostActive()
    {
        if (MfdCorrespondenceHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Correspondence", StringComparison.OrdinalIgnoreCase)
               && MfdCorrespondenceHost.Visibility == Visibility.Visible;
    }

    internal void CorrespondenceRefresh_OnClick(object sender, RoutedEventArgs e) =>
        RefreshCorrespondenceItems();

    internal void CorrespondenceReverse_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        OpenCorrespondenceItem(CorrespondenceReverseList?.SelectedItem as GlassCorrespondenceFeed.Item);

    internal void CorrespondenceForward_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        OpenCorrespondenceItem(CorrespondenceForwardList?.SelectedItem as GlassCorrespondenceFeed.Item);

    void OpenCorrespondenceItem(GlassCorrespondenceFeed.Item? item)
    {
        if (item is null)
            return;
        OpenCodeFile(item.FilePath);
        StatusText.Text = $"glass · crs · {item.Display}";
    }

    void RefreshCorrespondenceItems()
    {
        _crsReverse.Clear();
        _crsForward.Clear();
        var (rev, fwd) = GlassCorrespondenceFeed.Collect(_session.WorkspaceRoot, _editorPath);
        foreach (var i in rev)
            _crsReverse.Add(i);
        foreach (var i in fwd)
            _crsForward.Add(i);
        if (CorrespondenceStatusLabel is not null)
            CorrespondenceStatusLabel.Text =
                $"crs · reverse {_crsReverse.Count} · forward {_crsForward.Count}";
    }
}
