#nullable enable

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass Ctrl+K melody chord HUD (local catalog; not full Avalonia CascadeChord).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassChordEntry> _chordEntries = new();
    DispatcherTimer? _chordTimeout;

    void InitCascadeChord()
    {
        ChordList.ItemsSource = _chordEntries;
        ChordQuery.TextChanged += (_, _) => RefreshChordFilter();
        ChordList.MouseDoubleClick += (_, _) => ExecuteChordSelection();
        ChordQuery.PreviewKeyDown += ChordQuery_OnPreviewKeyDown;
        ChordList.PreviewKeyDown += ChordList_OnPreviewKeyDown;
    }

    void ToggleCascadeChord()
    {
        if (ChordOverlay.Visibility == Visibility.Visible)
        {
            CloseCascadeChord();
            return;
        }

        CloseCommandPalette();
        ChordQuery.Text = "";
        RefreshChordFilter();
        ChordOverlay.Visibility = Visibility.Visible;
        ChordQuery.Focus();
        Keyboard.Focus(ChordQuery);
        ArmChordTimeout();
    }

    void CloseCascadeChord()
    {
        ChordOverlay.Visibility = Visibility.Collapsed;
        _chordEntries.Clear();
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

    void RefreshChordFilter()
    {
        ArmChordTimeout();
        var hits = GlassChordCatalog.Filter(ChordQuery.Text);
        _chordEntries.Clear();
        foreach (var h in hits)
            _chordEntries.Add(h);
        ChordList.SelectedIndex = _chordEntries.Count > 0 ? 0 : -1;

        // Unambiguous exact alias → run without Enter (ADR 0060 simple alias).
        if (GlassChordCatalog.Exact(ChordQuery.Text) is { } exact
            && hits.Count == 1
            && hits[0].Alias == exact.Alias)
        {
            CloseCascadeChord();
            RunChordAction(exact.ActionId);
        }
    }

    void ChordQuery_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseCascadeChord();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && _chordEntries.Count > 0)
        {
            ChordList.Focus();
            ChordList.SelectedIndex = 0;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecuteChordSelection();
            e.Handled = true;
        }
    }

    void ChordList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseCascadeChord();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecuteChordSelection();
            e.Handled = true;
        }
    }

    void ExecuteChordSelection()
    {
        if (ChordList.SelectedItem is not GlassChordEntry entry)
        {
            if (_chordEntries.Count == 0)
                return;
            entry = _chordEntries[0];
        }

        CloseCascadeChord();
        RunChordAction(entry.ActionId);
    }

    void RunChordAction(string actionId)
    {
        if (actionId == "palette")
        {
            ToggleCommandPalette();
            return;
        }

        RunPaletteEntry(actionId);
    }
}
