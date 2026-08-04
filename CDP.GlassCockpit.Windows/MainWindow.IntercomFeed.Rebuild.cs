#nullable enable

using CascadeIDE.Intercom;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>Intercom feed rebuild, topic filter, scroll pin, new-msg cue.</summary>
public partial class MainWindow
{
    /// <summary>Journal window for 30m topic clustering (wider than on-screen feed when filtered).</summary>
    public const int TopicClusterTail = 240;

    void RebuildIntercomFeedFromJournal(bool stickEnd = false)
    {
        var wasPinned = CascadeIDE.Intercom.GlassIntercomFeedScroll.IsPinnedToEnd(
            FeedScroll.VerticalOffset,
            FeedScroll.ExtentHeight,
            FeedScroll.ViewportHeight);
        var priorOffset = FeedScroll.VerticalOffset;

        var entries = GlassIntercomJournal.LoadTail(TopicClusterTail);
        foreach (var e in entries)
        {
            if (e.Id.Length > 0)
                _seenIntercomIds.Add(e.Id);
        }

        var clustered = GlassIntercomTopics.Cluster(entries);
        _selectedTopicId = CascadeIDE.Intercom.GlassIntercomTopicSelection.Survive(
            _selectedTopicId, clustered, _selectedTopicEntryIds);

        if (stickEnd && entries.Count > 0)
        {
            _selectedTopicId = CascadeIDE.Intercom.GlassIntercomTopicFollow.AfterStickEnd(
                _selectedTopicId, clustered, entries[^1].Id);
        }

        _topics.Clear();
        foreach (var t in clustered)
        {
            var selected = _selectedTopicId is { Length: > 0 } sid
                && string.Equals(t.Id, sid, StringComparison.OrdinalIgnoreCase);
            if (selected)
                _selectedTopicEntryIds = t.EntryIds.ToArray();
            _topics.Add(new TopicCard(t.Id, t.Title, selected, t.EntryIds));
        }

        TopicsEmptyHint.Visibility = _topics.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_selectedTopicId is null)
            _selectedTopicEntryIds = [];

        SyncTopicAllChrome();

        _feed.Clear();
        _feed.Add(new ChatBubble(
            "system",
            "MVP: Forward respects primary_work_surface. Intercom→editor on M; dark AvalonEdit theme. Virtual History + topic cards (30m gap).",
            DateTime.Now.ToString("HH:mm")));

        IEnumerable<GlassIntercomJournal.Entry> shown = entries;
        if (_selectedTopicId is { Length: > 0 } topicId)
        {
            var topic = clustered.FirstOrDefault(t =>
                string.Equals(t.Id, topicId, StringComparison.OrdinalIgnoreCase));
            if (topic is not null)
            {
                var allow = topic.EntryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                shown = entries.Where(e => allow.Contains(e.Id));
            }
        }

        var ws = _session.WorkspaceRoot;
        foreach (var e in shown)
        {
            var chips = GlassAttachChipPeel.ResolveAgainstDisk(e.Chips, ws);
            chips = GlassMessageCodePeel.MergeWithAttach(chips, e.Body);
            var body = chips.Count > 0
                ? GlassAttachChipPeel.StripBracketsForDisplay(e.Body)
                : e.Body;
            body = LatchPaint.CompactIntercomBody(body);
            if (string.IsNullOrWhiteSpace(body) && chips.Count > 0)
                body = "(attach)";
            _feed.Add(new ChatBubble(e.RoleLabel, body, e.WhenLabel, chips));
        }

        while (_feed.Count > 81)
            _feed.RemoveAt(1);

        ApplyFeedScrollAfterRebuild(stickEnd, wasPinned, priorOffset);
    }

    void ApplyFeedScrollAfterRebuild(bool stickEnd, bool wasPinned, double priorOffset)
    {
        if (stickEnd || wasPinned)
            _pendingNewBelow = CascadeIDE.Intercom.GlassIntercomNewMessageCue.AfterPinnedOrStickEnd(_pendingNewBelow);

        var target = CascadeIDE.Intercom.GlassIntercomFeedScroll.ResolveOffsetAfterRebuild(
            stickEnd, wasPinned, priorOffset);
        // Layout after ItemsControl mutate — apply on next pass.
        Dispatcher.BeginInvoke(() =>
        {
            if (double.IsPositiveInfinity(target))
                FeedScroll.ScrollToEnd();
            else
                FeedScroll.ScrollToVerticalOffset(target);
            SyncNewMsgCue();
        }, DispatcherPriority.Loaded);
    }

    void SyncNewMsgCue()
    {
        var show = CascadeIDE.Intercom.GlassIntercomNewMessageCue.ShouldShow(_pendingNewBelow);
        NewMsgCueBtn.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        NewMsgCueBtn.Content = CascadeIDE.Intercom.GlassIntercomNewMessageCue.FormatLabel(_pendingNewBelow);
    }

    void NoteArrivalWhileReading(bool wasPinnedToEnd)
    {
        _pendingNewBelow = CascadeIDE.Intercom.GlassIntercomNewMessageCue.AfterArrival(
            _pendingNewBelow, wasPinnedToEnd);
        SyncNewMsgCue();
    }

    void FeedScroll_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!CascadeIDE.Intercom.GlassIntercomFeedScroll.IsPinnedToEnd(
                FeedScroll.VerticalOffset,
                FeedScroll.ExtentHeight,
                FeedScroll.ViewportHeight))
            return;

        _pendingNewBelow = CascadeIDE.Intercom.GlassIntercomNewMessageCue.AfterPinnedOrStickEnd(_pendingNewBelow);
        SyncNewMsgCue();
    }

    void NewMsgCueBtn_OnClick(object sender, RoutedEventArgs e)
    {
        _pendingNewBelow = CascadeIDE.Intercom.GlassIntercomNewMessageCue.AfterPinnedOrStickEnd(_pendingNewBelow);
        FeedScroll.ScrollToEnd();
        SyncNewMsgCue();
    }

    void SyncTopicAllChrome()
    {
        TopicAllBtn.Tag = _selectedTopicId is null ? "selected" : "";
    }

    void TopicAllBtn_OnClick(object sender, RoutedEventArgs e)
    {
        _selectedTopicId = null;
        _selectedTopicEntryIds = [];
        RebuildIntercomFeedFromJournal(stickEnd: true);
    }

    void TopicCard_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id } || id.Length == 0)
            return;
        ApplyIntercomTopicSelection(id);
    }

    void SelectIntercomTopicNext()
    {
        var ids = _topics.Select(t => t.Id).ToArray();
        var next = CascadeIDE.Intercom.GlassIntercomTopicNav.Next(_selectedTopicId, ids);
        if (next is null || next.Length == 0)
        {
            StatusText.Text = "glass · topic next · empty";
            return;
        }

        ApplyIntercomTopicSelection(next);
        StatusText.Text = $"glass · topic next · {ShortTopicLabel(next)}";
    }

    void SelectIntercomTopicPrev()
    {
        var ids = _topics.Select(t => t.Id).ToArray();
        var prev = CascadeIDE.Intercom.GlassIntercomTopicNav.Prev(_selectedTopicId, ids);
        if (prev is null || prev.Length == 0)
        {
            StatusText.Text = "glass · topic prev · empty";
            return;
        }

        ApplyIntercomTopicSelection(prev);
        StatusText.Text = $"glass · topic prev · {ShortTopicLabel(prev)}";
    }

    void ApplyIntercomTopicSelection(string id)
    {
        _selectedTopicId = id;
        var card = _topics.FirstOrDefault(t =>
            string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
        _selectedTopicEntryIds = card?.EntryIds.ToArray() ?? [id];
        RebuildIntercomFeedFromJournal(stickEnd: true);
    }

    static string ShortTopicLabel(string id) =>
        id.Length <= 28 ? id : id[..25] + "…";

    void PageIntercomFeed(int direction)
    {
        var step = Math.Max(48, FeedScroll.ViewportHeight * 0.85);
        var target = FeedScroll.VerticalOffset + direction * step;
        if (target < 0)
            target = 0;
        var max = Math.Max(0, FeedScroll.ExtentHeight - FeedScroll.ViewportHeight);
        if (target > max)
            target = max;
        FeedScroll.ScrollToVerticalOffset(target);
        StatusText.Text = direction > 0
            ? "glass · feed page ↓"
            : "glass · feed page ↑";
    }
}
