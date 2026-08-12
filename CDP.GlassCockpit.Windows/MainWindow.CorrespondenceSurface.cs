#nullable enable

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Correspondence — instrument cards + thread timeline (not dual FS dump).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassCorrespondenceFeed.TimelineRow> _crsTimeline = new();

    void RefreshMfdCorrespondenceVisibility()
    {
        if (MfdCorrespondenceHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Correspondence", StringComparison.OrdinalIgnoreCase);
        MfdCorrespondenceHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
        {
            if (CorrespondenceTimelineList is not null
                && !ReferenceEquals(CorrespondenceTimelineList.ItemsSource, _crsTimeline))
                CorrespondenceTimelineList.ItemsSource = _crsTimeline;
            if (_crsTimeline.Count == 0 || (CorrespondenceCardsPanel?.Items.Count ?? 0) == 0)
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

    internal void CorrespondenceTimeline_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        OpenCorrespondenceItem(
            (CorrespondenceTimelineList?.SelectedItem as GlassCorrespondenceFeed.TimelineRow)?.Item);

    void OpenCorrespondenceItem(GlassCorrespondenceFeed.Item? item)
    {
        if (item is null)
            return;

        OpenCodeFile(item.FilePath, item.LineHint);
        StatusText.Text = $"glass · crs · {item.Display}";
    }

    void RefreshCorrespondenceItems()
    {
        var snap = GlassCorrespondenceFeed.Collect(_session.WorkspaceRoot, _editorPath);
        if (CorrespondenceCardsPanel is not null)
        {
            CorrespondenceCardsPanel.Items.Clear();
            foreach (var chip in GlassCorrespondenceFeed.BuildInstrument(snap, _editorPath))
                CorrespondenceCardsPanel.Items.Add(CreateDeckCard(chip));
        }

        _crsTimeline.Clear();
        foreach (var row in GlassCorrespondenceFeed.BuildTimeline(snap, _editorPath))
            _crsTimeline.Add(row);

        if (CorrespondenceStatusLabel is not null)
            CorrespondenceStatusLabel.Text =
                $"crs · timeline {_crsTimeline.Count} · {snap.StatusLine}";
        if (!string.IsNullOrWhiteSpace(snap.FeatureLine) || !string.IsNullOrWhiteSpace(snap.AdrLine))
            StatusText.Text =
                $"glass · crs · {snap.FeatureLine}"
                + (string.IsNullOrWhiteSpace(snap.AdrLine) ? "" : $" · {snap.AdrLine}");
    }
}
