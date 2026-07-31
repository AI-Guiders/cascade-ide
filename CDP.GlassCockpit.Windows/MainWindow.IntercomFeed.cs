#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>Intercom Virtual History — latch watch, journal append, composer send.</summary>
public partial class MainWindow
{
    readonly ObservableCollection<ChatBubble> _feed = new();
    readonly ObservableCollection<TopicCard> _topics = new();
    readonly HashSet<string> _seenIntercomIds = new(StringComparer.OrdinalIgnoreCase);
    string? _selectedTopicId;
    string[] _selectedTopicEntryIds = [];
    int _pendingNewBelow;

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
        // RoleLabel looks like "@PM → @PF · human"
        var role = view.RoleLabel;
        var from = "?";
        var to = "?";
        var origin = "?";
        var parts = role.Split('·', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
            origin = parts[1];
        var arrow = parts[0].Split('→', 2, StringSplitOptions.TrimEntries);
        if (arrow.Length == 2)
        {
            from = arrow[0].Trim().TrimStart('@').ToLowerInvariant();
            to = arrow[1].Trim().TrimStart('@').ToLowerInvariant();
        }

        GlassIntercomJournal.Append(id, from, to, view.Body, origin, DateTimeOffset.Now);
    }

    void OnIntercomChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintIntercom(raw);
                IntercomSubtitle.Text = view.Header;

                // FileSystemWatcher often fires twice on atomic replace; also skip own Send echo.
                if (view.MessageId is { Length: > 0 } id && !_seenIntercomIds.Add(id))
                {
                    StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
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
        if (ComposerBox.Text is "Message @PF…" or "Message @PM…")
            ComposerBox.Clear();
    }

    void TrySendComposer()
    {
        var raw = ComposerBox.Text;
        if (TryRunGlassSlash(raw))
            return;

        var sent = GlassIntercomSend.TrySend(raw);
        if (sent is null)
        {
            StatusText.Text = "glass · intercom · empty — nothing sent";
            return;
        }

        _seenIntercomIds.Add(sent.Id);
        RebuildIntercomFeedFromJournal(stickEnd: true);

        ComposerBox.Clear();
        HideSlashPopup();
        StatusText.Text = $"glass · intercom · sent {sent.Id} · @PM→@PF · {DateTime.Now:HH:mm:ss}";
    }

    public sealed record TopicCard(
        string Id,
        string Title,
        bool IsSelected,
        IReadOnlyList<string> EntryIds);

    public sealed record ChatBubble(string Role, string Body, string When);
}
