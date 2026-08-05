#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Intercom Virtual History — latch watch, journal append, composer send.</summary>
public partial class MainWindow
{
    readonly ObservableCollection<ChatBubble> _feed = new();
    readonly ObservableCollection<TopicCard> _topics = new();
    readonly HashSet<string> _seenIntercomIds = new(StringComparer.OrdinalIgnoreCase);
    string? _selectedTopicId;
    string[] _selectedTopicEntryIds = [];
    bool _isTopicOverviewMode;
    int _lastOverviewTopicCount = -1;
    int _pendingNewBelow;
    string _intercomHeader = "Intercom";
    string? _partnerPresenceLine;
    DispatcherTimer? _presenceStaleTimer;
    DispatcherTimer? _composingDebounce;
    string? _lastPublishedPmPresence;

    void LoadIntercomHistory()
    {
        try
        {
            RebuildIntercomFeedFromJournal(stickEnd: true);
        }
        catch
        {
            /* best-effort */
        }
    }

    static void TryJournalFromView(LatchPaint.IntercomView view)
    {
        if (view.MessageId is not { Length: > 0 } id)
            return;

        GlassIntercomJournal.Append(
            id,
            view.FromSeat,
            view.ToSeat,
            view.Body,
            view.Origin,
            DateTimeOffset.Now,
            view.Name,
            view.Kind);
    }

    void OnIntercomChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintIntercom(raw);
                _intercomHeader = view.Header;
                MergeIntercomSubtitle();

                // FileSystemWatcher often fires twice on atomic replace; also skip own Send echo.
                if (view.MessageId is { Length: > 0 } id && !_seenIntercomIds.Add(id))
                {
                    StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
                    return;
                }

                // Autoi wake → SoftOrgan tip / StatusText (ignite-wake latch), not Intercom chat.
                if (LatchPaint.IsAutoiWakeFeedNoise(view.Body, view.Name, view.Kind, view.RoleLabel))
                {
                    StatusText.Text = $"glass · {view.StatusLine} · tip only · {DateTime.Now:HH:mm:ss}";
                    return;
                }

                TryJournalFromView(view);
                var wasPinned = CascadeIDE.Intercom.GlassIntercomFeedScroll.IsPinnedToEnd(
                    FeedScroll.VerticalOffset,
                    FeedScroll.ExtentHeight,
                    FeedScroll.ViewportHeight);
                RebuildIntercomFeedFromJournal(); // preserve scroll unless pinned to end
                NoteArrivalWhileReading(wasPinned);
                StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · intercom fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnCitizenDialogRequestChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = CascadeIDE.Intercom.CitizenDialogRequestStatus.TryPaint(raw);
                if (view is null)
                    return;
                StatusText.Text = $"{view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · citizen request fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnPresenceChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                ApplyPartnerPresenceFromDisk(path);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · presence fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void InitIntercomPresence()
    {
        ApplyPartnerPresenceFromDisk(null);
        _presenceStaleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _presenceStaleTimer.Tick += (_, _) => ApplyPartnerPresenceFromDisk(null);
        _presenceStaleTimer.Start();

        _composingDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _composingDebounce.Tick += (_, _) =>
        {
            _composingDebounce.Stop();
            PublishPmPresenceFromComposer();
        };
    }

    void ApplyPartnerPresenceFromDisk(string? path)
    {
        string? json = null;
        if (path is not null && File.Exists(path))
            json = File.ReadAllText(path);
        _partnerPresenceLine = GlassIntercomPresence.TryPartnerLine(json);
        MergeIntercomSubtitle();
    }

    void MergeIntercomSubtitle()
    {
        IntercomSubtitle.Text = string.IsNullOrWhiteSpace(_partnerPresenceLine)
            ? _intercomHeader
            : $"{_intercomHeader} · {_partnerPresenceLine}";
    }

    void NoteComposerPresenceChanged()
    {
        if (_composingDebounce is null)
            return;
        _composingDebounce.Stop();
        _composingDebounce.Start();
    }

    void PublishPmPresenceFromComposer()
    {
        var text = ComposerBox.Text ?? "";
        var empty = string.IsNullOrWhiteSpace(text)
                    || GlassIntercomLane.IsComposerPlaceholder(text);
        var state = empty ? "idle" : "composing";
        if (string.Equals(_lastPublishedPmPresence, state, StringComparison.Ordinal))
            return;
        if (GlassIntercomPresence.TryPublish("pm", state))
            _lastPublishedPmPresence = state;
    }

    void PublishPmIdle()
    {
        if (GlassIntercomPresence.TryPublish("pm", "idle"))
            _lastPublishedPmPresence = "idle";
    }

    void SendBtn_OnClick(object sender, RoutedEventArgs e) => TrySendComposer();

    void ComposerBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryHandleSlashComposerKeys(e))
            return;

        if (e.Key != Key.Enter)
            return;

        // Shift+Enter = newline; Enter / Ctrl+Enter = send (or slash run)
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            return;

        e.Handled = true;
        if (SlashPopup.IsOpen && _slashSuggestions.Count > 0)
        {
            CommitSlashSuggestion(run: true);
            return;
        }

        TrySendComposer();
    }

    void ComposerBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (GlassIntercomLane.IsComposerPlaceholder(ComposerBox.Text))
            ComposerBox.Clear();
    }

    void TrySendComposer()
    {
        var raw = ComposerBox.Text;
        if (TryRunGlassSlash(raw))
            return;

        string? id;
        string? roleLabel;
        if (_lane == GlassIntercomLane.Kind.Cit)
        {
            var cit = GlassCitizenDialogRequest.TryEnqueue(raw, _modelId, _session.WorkspaceRoot);
            if (cit is null)
            {
                StatusText.Text = "glass · intercom · empty — nothing sent";
                return;
            }

            id = cit.Id;
            roleLabel = cit.RoleLabel;
        }
        else if (_lane == GlassIntercomLane.Kind.Host)
        {
            var host = GlassHostComposerRequest.TryEnqueue(raw, _session.WorkspaceRoot);
            if (host is null)
            {
                StatusText.Text = "glass · intercom · empty — nothing sent";
                return;
            }

            id = host.Id;
            roleLabel = host.RoleLabel;
        }
        else
        {
            var pf = GlassIntercomSend.TrySend(raw, _session.WorkspaceRoot);
            if (pf is null)
            {
                StatusText.Text = "glass · intercom · empty — nothing sent";
                return;
            }

            id = pf.Id;
            roleLabel = pf.RoleLabel;
        }

        _seenIntercomIds.Add(id);
        RebuildIntercomFeedFromJournal(stickEnd: true);

        ComposerBox.Clear();
        HideSlashPopup();
        PublishPmIdle();
        StatusText.Text =
            $"glass · intercom · {_lane} · sent {id} · {roleLabel} · {DateTime.Now:HH:mm:ss}";
    }

    public sealed record TopicCard(
        string Id,
        string Title,
        bool IsSelected,
        IReadOnlyList<string> EntryIds,
        string Summary = "");

    public sealed record ChatBubble(
        string Role,
        string Body,
        string When,
        IReadOnlyList<CascadeIDE.Intercom.GlassAttachChip>? Chips = null,
        IReadOnlyList<CascadeIDE.SoftOrgan.GlassGlanceChip>? Pointers = null)
    {
        public bool HasChips => Chips is { Count: > 0 };
        public bool HasPointers => Pointers is { Count: > 0 };
    }
}
