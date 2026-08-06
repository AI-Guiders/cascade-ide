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

        var entries = GlassIntercomJournal.LoadTail(TopicClusterTail)
            .Where(e => !LatchPaint.IsAutoiWakeFeedNoise(e.Body, roleLabel: e.RoleLabel))
            .Where(e => GlassIntercomChannel.MatchesFeed(_channel, e.Channel))
            .ToList();
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

        // Stick-end send/receive always leaves overview (All selection must not keep tile grid).
        if (stickEnd)
            _isTopicOverviewMode = false;

        // Hide while mutate — Clear+Add at scroll 0 painted tile stack for a frame (Send flash).
        // Do NOT null ItemsSource: tears down N RichTextBox/FlowDocuments mid-layout → PtsHost FailFast
        // («Unknown Hard Error», 2026-08-06 07:58).
        if (!_isTopicOverviewMode)
            FeedScroll.Opacity = 0;

        _topics.Clear();
        foreach (var t in clustered)
        {
            var selected = _selectedTopicId is { Length: > 0 } sid
                && string.Equals(t.Id, sid, StringComparison.OrdinalIgnoreCase);
            if (selected)
                _selectedTopicEntryIds = t.EntryIds.ToArray();
            var allow = t.EntryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var bodies = entries
                .Where(e => allow.Contains(e.Id))
                .Select(e => e.Body);
            var summary = GlassTopicCardSummary.Format(t.Count, t.StartUtc, t.EndUtc, bodies);
            _topics.Add(new TopicCard(t.Id, t.Title, selected, t.EntryIds, summary));
        }

        TopicsEmptyHint.Visibility = _topics.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_selectedTopicId is null)
            _selectedTopicEntryIds = [];

        // Face: flat feed default — topic catalog is opt-in via Overview (no adaptive overview slap).
        SyncTopicOverviewChrome();
        SyncProductSpineChrome();

        if (_isTopicOverviewMode)
        {
            _feed.Clear();
            FeedScroll.Opacity = 1;
            ApplyFeedScrollAfterRebuild(stickEnd, wasPinned, priorOffset);
            return;
        }

        _feed.Clear();

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
            if (LatchPaint.IsAutoiWakeFeedNoise(e.Body, roleLabel: e.RoleLabel))
                continue;

            var chips = GlassAttachChipPeel.ResolveAgainstDisk(e.Chips, ws);
            chips = GlassMessageCodePeel.MergeWithAttach(chips, e.Body);
            var body = chips.Count > 0
                ? GlassAttachChipPeel.StripBracketsForDisplay(e.Body)
                : e.Body;
            body = LatchPaint.CompactIntercomBody(body);
            var radio = CascadeIDE.Intercom.GlassRadioPointer.FromBody(body);
            body = radio.Body;
            var pointers = radio.Pointers.Count > 0 ? radio.Pointers : null;
            if (string.IsNullOrWhiteSpace(body) && chips.Count > 0)
                body = "(attach)";
            if (string.IsNullOrWhiteSpace(body) && pointers is { Count: > 0 })
                body = "(radio)";
            var role = GlassIntercomFaceMeta.QuietRole(e.RoleLabel);
            var ordinal = _feed.Count + 1;
            var hi = _messageSelect.Highlighted.Contains(ordinal);
            var sel = ordinal == _messageSelect.ActiveOrdinal;
            _feed.Add(new ChatBubble(role, body, e.WhenLabel, chips, pointers, ordinal, sel, hi));
        }

        // Drop selection that no longer fits the rebuilt feed.
        if (_messageSelect.ActiveOrdinal > _feed.Count)
            _messageSelect = GlassIntercomMessageSelect.Empty;

        while (_feed.Count > 81)
            _feed.RemoveAt(1);

        ApplyMessageSelectToFeed();
        ApplyFeedScrollAfterRebuild(stickEnd, wasPinned, priorOffset);
    }

    void ApplyMessageSelectToFeed()
    {
        if (_messageSelect.ActiveOrdinal > _feed.Count)
            _messageSelect = GlassIntercomMessageSelect.Empty;

        for (var i = 0; i < _feed.Count; i++)
        {
            var ord = i + 1;
            var b = _feed[i];
            var hi = _messageSelect.Highlighted.Contains(ord);
            var sel = ord == _messageSelect.ActiveOrdinal;
            if (b.Ordinal == ord && b.IsSelected == sel && b.IsHighlighted == hi)
                continue;
            _feed[i] = b with { Ordinal = ord, IsSelected = sel, IsHighlighted = hi };
        }
    }

    void MessageSelectMenu_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi)
            return;
        var bubble = mi.DataContext as ChatBubble
                     ?? (mi.Parent as ContextMenu)?.PlacementTarget is FrameworkElement fe
                         ? fe.DataContext as ChatBubble
                         : null;
        if (bubble is null || bubble.Ordinal <= 0)
            return;
        var apply = GlassIntercomMessageSelect.Apply(_feed.Count, bubble.Ordinal, bubble.Ordinal, out var sel);
        if (!string.Equals(apply, "OK", StringComparison.Ordinal))
            return;
        _messageSelect = sel;
        ApplyMessageSelectToFeed();
        StatusText.Text = $"glass · select #{bubble.Ordinal} · {DateTime.Now:HH:mm:ss}";
    }

    void MessageSelectClearMenu_OnClick(object sender, RoutedEventArgs e)
    {
        _messageSelect = GlassIntercomMessageSelect.Empty;
        ApplyMessageSelectToFeed();
        StatusText.Text = $"glass · select clear · {DateTime.Now:HH:mm:ss}";
    }

    void ApplyFeedScrollAfterRebuild(bool stickEnd, bool wasPinned, double priorOffset)
    {
        if (stickEnd || wasPinned)
            _pendingNewBelow = CascadeIDE.Intercom.GlassIntercomNewMessageCue.AfterPinnedOrStickEnd(_pendingNewBelow);

        var target = CascadeIDE.Intercom.GlassIntercomFeedScroll.ResolveOffsetAfterRebuild(
            stickEnd, wasPinned, priorOffset);

        // Scroll after layout — never sync UpdateLayout here (PTS/RTB measure FailFast under load).
        Dispatcher.BeginInvoke(() =>
        {
            if (double.IsPositiveInfinity(target))
                FeedScroll.ScrollToEnd();
            else
                FeedScroll.ScrollToVerticalOffset(target);
            FeedScroll.Opacity = 1;
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

    void SyncTopicOverviewChrome()
    {
        var overview = _isTopicOverviewMode;
        TopicOverviewScroll.Visibility = overview ? Visibility.Visible : Visibility.Collapsed;
        FeedScroll.Visibility = overview ? Visibility.Collapsed : Visibility.Visible;
        TopicAllBtn.Tag = overview ? "selected" : "";
        TopicBackBtn.Visibility = overview || _topics.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        TopicsOverviewHint.Visibility = overview && _topics.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (overview)
        {
            var n = _topics.Count;
            TopicsOverviewHint.Text = n == 1
                ? "1 тема · Enter (ato) — открыть · atp/atn — выбор · atb — сюда"
                : $"{n} тем · Enter (ato) — открыть · atp/atn — выбор · atb — сюда";
            TopicOverviewHeader.Text = n <= 1 ? "Topic" : $"{n} topics";
        }
    }

    void SyncProductSpineChrome()
    {
        var spine = GlassProductSpineStore.LoadOrEmpty();
        var strip = GlassProductSpineStore.FormatStrip(spine);
        if (strip.Length == 0)
        {
            ProductSpineStrip.Visibility = Visibility.Collapsed;
            ProductSpineStrip.Text = "";
            return;
        }

        ProductSpineStrip.Text = strip;
        ProductSpineStrip.Visibility = Visibility.Visible;
    }

    void TopicAllBtn_OnClick(object sender, RoutedEventArgs e) => ShowIntercomTopicOverview();

    void TopicBackBtn_OnClick(object sender, RoutedEventArgs e) => ShowIntercomTopicOverview();

    void ShowIntercomTopicOverview()
    {
        _isTopicOverviewMode = true;
        if (_topics.Count > 0)
            _lastOverviewTopicCount = _topics.Count;
        RebuildIntercomFeedFromJournal(stickEnd: false);
        StatusText.Text = $"glass · topics overview · {_topics.Count}";
    }

    void EnterIntercomFocusedTopic()
    {
        if (_selectedTopicId is not { Length: > 0 } id)
        {
            id = _topics.FirstOrDefault()?.Id;
            if (id is null || id.Length == 0)
            {
                StatusText.Text = "glass · topic enter · empty";
                return;
            }
        }

        _isTopicOverviewMode = false;
        ApplyIntercomTopicSelection(id);
        StatusText.Text = $"glass · topic enter · {ShortTopicLabel(id)}";
    }

    void TopicCard_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id } || id.Length == 0)
            return;
        _isTopicOverviewMode = false;
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

        if (_isTopicOverviewMode)
        {
            _selectedTopicId = next;
            RebuildIntercomFeedFromJournal(stickEnd: false);
        }
        else
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

        if (_isTopicOverviewMode)
        {
            _selectedTopicId = prev;
            RebuildIntercomFeedFromJournal(stickEnd: false);
        }
        else
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
