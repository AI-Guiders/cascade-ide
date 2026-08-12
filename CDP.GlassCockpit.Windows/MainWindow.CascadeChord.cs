#nullable enable

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass Ctrl+K AwaitMelodyTail — live PreviewKeyDown tunnel (ADR 0060).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassChordMelodyEntry> _chordMelodyEntries = new();
    DispatcherTimer? _chordTimeout;
    string _chordMelodyTail = "";
    bool _chordMelodyAwait;

    void InitCascadeChord()
    {
        ChordList.ItemsSource = _chordMelodyEntries;
        ChordQuery.IsReadOnly = true;
        ChordQuery.IsTabStop = false;
        ChordQuery.Focusable = false;
        ChordList.MouseDoubleClick += (_, _) => ExecuteChordMelodySelection();
        ChordList.PreviewKeyDown += ChordList_OnPreviewKeyDown;
    }

    void ToggleCascadeChord()
    {
        if (ChordOverlay?.Visibility == Visibility.Visible)
        {
            CloseCascadeChord();
            return;
        }

        CloseCommandPalette();
        CloseOpenFamily();
        BeginChordMelodyAwait();
    }

    void BeginChordMelodyAwait()
    {
        _chordMelodyAwait = true;
        _chordMelodyTail = "";
        RefreshChordMelodyUi();
        SetFloatingOverlay(ChordOverlay, true);
        Focus();
        ArmChordTimeout();
    }

    void CloseCascadeChord()
    {
        _chordMelodyAwait = false;
        _chordMelodyTail = "";
        SetFloatingOverlay(ChordOverlay, false);
        _chordMelodyEntries.Clear();
        if (ChordQuery is not null)
            ChordQuery.Text = "";
        DisarmChordTimeout();
    }

    void ArmChordTimeout()
    {
        DisarmChordTimeout();
        _chordTimeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _chordTimeout.Tick += (_, _) => CloseCascadeChord();
        _chordTimeout.Start();
    }

    void DisarmChordTimeout()
    {
        if (_chordTimeout is null)
            return;
        _chordTimeout.Stop();
        _chordTimeout = null;
    }

    void RefreshChordMelodyUi()
    {
        if (!_chordMelodyAwait)
            return;

        ArmChordTimeout();
        if (ChordQuery is not null)
            ChordQuery.Text = string.IsNullOrEmpty(_chordMelodyTail)
                ? "мелодия (как c:)"
                : _chordMelodyTail;

        var hits = GlassChordCatalog.FilterMelodyTail(_chordMelodyTail).ToList();
        foreach (var local in GlassChordCatalog.Filter(_chordMelodyTail))
        {
            if (hits.Any(h => string.Equals(h.Alias, local.Alias, StringComparison.Ordinal)))
                continue;
            hits.Add(new GlassChordMelodyEntry(local.Alias, local.ActionId, local.Help, true));
        }

        _chordMelodyEntries.Clear();
        foreach (var h in hits.Take(GlassChordMelody.MaxSuggestions))
            _chordMelodyEntries.Add(h);
        ChordList.SelectedIndex = _chordMelodyEntries.Count > 0 ? 0 : -1;

        if (GlassChordCatalog.Exact(_chordMelodyTail) is not null
            && !GlassChordMelody.HasStrictLongerAliasPrefix(_chordMelodyTail))
        {
            CommitChordMelody(instant: true);
            return;
        }

        if (GlassChordMelody.TryResolveExactCommand(_chordMelodyTail, out var cmdId)
            && GlassMelodyGlassActions.TryMapCommandId(cmdId, out _))
        {
            CommitChordMelody(instant: true);
        }
    }

    bool TryConsumeChordMelodyKeyDown(KeyEventArgs e)
    {
        if (!_chordMelodyAwait || ChordOverlay?.Visibility != Visibility.Visible)
            return false;

        if (e.Key == Key.Escape)
        {
            CloseCascadeChord();
            e.Handled = true;
            return true;
        }

        if (Keyboard.Modifiers is ModifierKeys.Control or ModifierKeys.Alt)
        {
            CloseCascadeChord();
            return false;
        }

        if (e.Key == Key.Enter)
        {
            CommitChordMelody(instant: false);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Back)
        {
            if (_chordMelodyTail.Length > 0)
                _chordMelodyTail = _chordMelodyTail[..^1];
            else
                CloseCascadeChord();
            RefreshChordMelodyUi();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Down && _chordMelodyEntries.Count > 0)
        {
            ChordList.Focus();
            ChordList.SelectedIndex = 0;
            e.Handled = true;
            return true;
        }

        if (!TryMapChordMelodyGlyph(e.Key, out var ch))
        {
            CloseCascadeChord();
            e.Handled = true;
            return true;
        }

        if (_chordMelodyTail.Length == 0 && ch == '/')
        {
            CloseCascadeChord();
            TryRunGlassSlash("/");
            e.Handled = true;
            return true;
        }

        _chordMelodyTail = GlassChordMelody.NormalizeInput(_chordMelodyTail + ch);
        RefreshChordMelodyUi();
        e.Handled = true;
        return true;
    }

    static bool TryMapChordMelodyGlyph(Key key, out char ch)
    {
        ch = '\0';
        if (key is >= Key.A and <= Key.Z)
        {
            ch = (char)('a' + (key - Key.A));
            return true;
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            ch = (char)('0' + (key - Key.D0));
            return true;
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            ch = (char)('0' + (key - Key.NumPad0));
            return true;
        }

        return key switch
        {
            Key.OemSemicolon => Assign(':', out ch),
            Key.OemComma => Assign(';', out ch),
            Key.OemPeriod => Assign('.', out ch),
            Key.Oem2 => Assign('/', out ch),
            Key.OemMinus => Assign('-', out ch),
            Key.Subtract => Assign('-', out ch),
            Key.Oem5 => Assign('_', out ch),
            _ => false,
        };

        static bool Assign(char value, out char outCh)
        {
            outCh = value;
            return true;
        }
    }

    void ChordList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryConsumeChordMelodyKeyDown(e))
            return;

        if (e.Key == Key.Enter)
        {
            ExecuteChordMelodySelection();
            e.Handled = true;
        }
    }

    void ExecuteChordMelodySelection()
    {
        if (ChordList.SelectedItem is GlassChordMelodyEntry entry)
            _chordMelodyTail = entry.Alias;
        CommitChordMelody(instant: false);
    }

    void CommitChordMelody(bool instant)
    {
        if (!_chordMelodyAwait)
            return;

        var tail = _chordMelodyTail;
        CloseCascadeChord();

        if (GlassChordMelody.TryResolveParametricSelect(tail, out var start, out var end))
        {
            SelectOpenDocumentLines(start, end);
            return;
        }

        if (GlassChordMelody.TryResolveParametricWebAi(tail, out var url))
        {
            RunWebAiPortal(url);
            return;
        }

        if (GlassChordMelody.TryResolveExactCommand(tail, out var commandId)
            || (instant && GlassIntentMelodyCatalog.FilterByTailPrefix(GlassMelodyTail.AliasPrefix(tail))
                .FirstOrDefault(a => a.Alias == GlassMelodyTail.AliasPrefix(tail)) is { } only
                && (commandId = only.CommandId).Length > 0))
        {
            if (GlassMelodyGlassActions.TryMapCommandId(commandId, out var action))
            {
                if (action == GlassMelodyGlassActions.RunSelectLines)
                    return;
                RunPaletteEntry(action);
                return;
            }
        }

        if (!instant && GlassMelodyGlassActions.TryMapCommandId(
                GlassIntentMelodyCatalog.FilterByTailPrefix(GlassMelodyTail.AliasPrefix(tail))
                    .FirstOrDefault(a => a.Alias == GlassMelodyTail.AliasPrefix(tail))?.CommandId,
                out var mapped))
        {
            RunPaletteEntry(mapped);
            return;
        }

        // Glass-local chords (SoftInstrument SoftFL) — not only CIDE intent-catalog.toml.
        if (GlassChordCatalog.Exact(tail) is { } local)
            RunPaletteEntry(local.ActionId);
    }

    void RunWebAiPortal(string? urlPayload)
    {
        SelectMfdPage("WebAiPortal", sticky: true);
        if (WebAiUrl is null)
            return;

        if (!string.IsNullOrWhiteSpace(urlPayload))
        {
            var url = urlPayload.Trim();
            if (!url.Contains("://", StringComparison.Ordinal))
                url = "https://" + url;
            WebAiUrl.Text = url;
            WebAiGo_OnClick(WebAiUrl, new RoutedEventArgs());
        }
    }

    void RunChordAction(string actionId) => RunPaletteEntry(actionId);
}
