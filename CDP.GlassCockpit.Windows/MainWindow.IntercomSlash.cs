#nullable enable

using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Intercom;
using CascadeIDE.Models;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass Intercom <c>/</c> autocomplete popup + local run (GlassSlashCatalog).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassSlashSuggestion> _slashSuggestions = new();
    readonly WorkspaceFileIndex _slashFileIndex = new();
    int _slashIndex;
    bool _composerSuggestIsMention;
    int _atTokenStart = -1;
    int _atTokenEnd = -1;

    void InitIntercomSlash()
    {
        SlashList.ItemsSource = _slashSuggestions;
        ComposerBox.TextChanged += ComposerBox_OnTextChanged;
        SlashList.PreviewKeyDown += SlashList_OnPreviewKeyDown;
        SlashList.MouseDoubleClick += (_, _) => CommitSlashSuggestion(run: true);
    }

    IReadOnlyList<(string InsertPath, string Help)> MatchWorkspaceFilesForSlash(string pathPrefix, int limit)
    {
        var root = _session.SolutionRoot;
        if (root is null)
            return [];

        var roots = new ObservableCollection<SolutionItem>(GlassSolutionExplorerFace.ResolveItems(root));
        if (roots.Count == 0)
            return [];

        _slashFileIndex.Invalidate(roots, _session.SolutionPath, _session.WorkspaceRoot ?? "");
        return _slashFileIndex.Search(pathPrefix, limit)
            .Select(m => (m.InsertPath, m.Help))
            .ToList();
    }

    void ComposerBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        NoteComposerPresenceChanged();
        RefreshSlashPopup();
    }

    void RefreshSlashPopup()
    {
        var text = ComposerBox.Text ?? "";
        if (GlassIntercomLane.IsComposerPlaceholder(text))
        {
            HideSlashPopup();
            return;
        }

        if (GlassSlashCatalog.IsSlashLine(text))
        {
            _composerSuggestIsMention = false;
            _atTokenStart = -1;
            _atTokenEnd = -1;
            var hits = GlassSlashCatalog.Suggest(text, MatchWorkspaceFilesForSlash);
            ShowComposerSuggestions(hits);
            return;
        }

        // SoftFL densify: @ mentions reuse SlashPopup (was / only).
        var caret = ComposerBox.CaretIndex;
        if (GlassIntercomMention.TryGetAtToken(text, caret, out var atStart, out var prefix))
        {
            _composerSuggestIsMention = true;
            _atTokenStart = atStart;
            _atTokenEnd = atStart + 1 + prefix.Length;
            var mentionHits = GlassIntercomMention.Suggest(prefix, CurrentMentionRoster())
                .Select(h => new GlassSlashSuggestion(h.Insert, h.Title, h.Help))
                .ToList();
            ShowComposerSuggestions(mentionHits);
            return;
        }

        HideSlashPopup();
    }

    void ShowComposerSuggestions(IReadOnlyList<GlassSlashSuggestion> hits)
    {
        _slashSuggestions.Clear();
        foreach (var h in hits)
            _slashSuggestions.Add(h);

        if (_slashSuggestions.Count == 0)
        {
            HideSlashPopup();
            return;
        }

        _slashIndex = 0;
        SlashList.SelectedIndex = 0;
        SlashPopup.IsOpen = true;
        SlashPopup.PlacementTarget = ComposerBox;
    }

    void HideSlashPopup()
    {
        if (SlashPopup.IsOpen)
            SlashPopup.IsOpen = false;
        _slashSuggestions.Clear();
        _composerSuggestIsMention = false;
        _atTokenStart = -1;
        _atTokenEnd = -1;
    }

    void SlashList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Tab)
        {
            e.Handled = true;
            CommitSlashSuggestion(run: e.Key == Key.Enter);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HideSlashPopup();
            ComposerBox.Focus();
        }
    }

    bool TryHandleSlashComposerKeys(KeyEventArgs e)
    {
        if (!SlashPopup.IsOpen || _slashSuggestions.Count == 0)
            return false;

        if (e.Key == Key.Escape)
        {
            HideSlashPopup();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Down)
        {
            _slashIndex = Math.Min(_slashIndex + 1, _slashSuggestions.Count - 1);
            SlashList.SelectedIndex = _slashIndex;
            SlashList.ScrollIntoView(_slashSuggestions[_slashIndex]);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Up)
        {
            _slashIndex = Math.Max(_slashIndex - 1, 0);
            SlashList.SelectedIndex = _slashIndex;
            SlashList.ScrollIntoView(_slashSuggestions[_slashIndex]);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Tab)
        {
            CommitSlashSuggestion(run: false);
            e.Handled = true;
            return true;
        }

        return false;
    }

    void CommitSlashSuggestion(bool run)
    {
        if (SlashList.SelectedItem is not GlassSlashSuggestion s
            && (_slashSuggestions.Count == 0 || _slashIndex < 0 || _slashIndex >= _slashSuggestions.Count))
        {
            HideSlashPopup();
            return;
        }

        var pick = SlashList.SelectedItem as GlassSlashSuggestion ?? _slashSuggestions[_slashIndex];

        // SoftFL densify: @ mention replaces token under caret (keep surrounding prose).
        if (_composerSuggestIsMention && _atTokenStart >= 0)
        {
            var text = ComposerBox.Text ?? "";
            var end = Math.Clamp(_atTokenEnd, _atTokenStart, text.Length);
            var before = text[.._atTokenStart];
            var after = end < text.Length ? text[end..] : "";
            var insert = pick.InsertText;
            ComposerBox.Text = before + insert + after;
            ComposerBox.CaretIndex = before.Length + insert.Length;
            HideSlashPopup();
            ComposerBox.Focus();
            return;
        }

        // ADR 0150: insert catalog InsertText; auto-run only when ArgTail policy allows (required needs N).
        ComposerBox.Text = pick.InsertText;
        ComposerBox.CaretIndex = pick.InsertText.Length;
        HideSlashPopup();
        ComposerBox.Focus();

        if (run && GlassSlashCatalog.ShouldAutoRunOnCommit(pick.InsertText.TrimEnd()))
            TryRunGlassSlash(pick.InsertText.TrimEnd());
        else if (run)
            StatusText.Text =
                $"glass · slash · arg_tail required · {pick.Title} · {DateTime.Now:HH:mm:ss}";
    }
}
