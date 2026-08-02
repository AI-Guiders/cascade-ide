#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass Intercom <c>/</c> autocomplete popup + local run (GlassSlashCatalog).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassSlashSuggestion> _slashSuggestions = new();
    int _slashIndex;

    void InitIntercomSlash()
    {
        SlashList.ItemsSource = _slashSuggestions;
        ComposerBox.TextChanged += ComposerBox_OnTextChanged;
        SlashList.PreviewKeyDown += SlashList_OnPreviewKeyDown;
        SlashList.MouseDoubleClick += (_, _) => CommitSlashSuggestion(run: true);
    }

    void ComposerBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        NoteComposerPresenceChanged();
        RefreshSlashPopup();
    }

    void RefreshSlashPopup()
    {
        var text = ComposerBox.Text ?? "";
        if (text is "Message @PF…" or "Message @PM…" || !GlassSlashCatalog.IsSlashLine(text))
        {
            HideSlashPopup();
            return;
        }

        var hits = GlassSlashCatalog.Suggest(text);
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
        ComposerBox.Text = pick.InsertText.TrimEnd() + (run ? "" : " ");
        ComposerBox.CaretIndex = ComposerBox.Text.Length;
        HideSlashPopup();
        ComposerBox.Focus();

        if (run)
            TryRunGlassSlash(ComposerBox.Text);
    }

    bool TryRunGlassSlash(string? raw)
    {
        if (!GlassSlashCatalog.TryResolve(raw, out var cmd, out var argsTail))
        {
            if (GlassSlashCatalog.IsSlashLine(raw))
            {
                StatusText.Text = $"glass · slash · unknown: {raw?.Trim()}";
                AppendSlashBubble($"/ ?", $"Unknown slash. Try /help\n({raw?.Trim()})");
                ComposerBox.Clear();
                HideSlashPopup();
                return true;
            }

            return false;
        }

        if (cmd.Id == "fds")
        {
            SelectMfdPage("FlightDataStorage");
            AppendSlashBubble(cmd.Path, GlassFdsGlance.Format(_session.WorkspaceRoot));
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "open")
        {
            var openBody = TryOpenPathSlash(argsTail);
            AppendSlashBubble(cmd.Path, openBody);
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        var body = cmd.Id switch
        {
            "help" => GlassSlashCatalog.FormatHelp(),
            "status" => BuildGlassStatusSlashBody(),
            "topics" => BuildGlassTopicsSlashBody(),
            "letter" => BuildGlassLetterSlashBody(),
            _ => $"Unhandled {cmd.Path}",
        };

        AppendSlashBubble(cmd.Path, body);
        ComposerBox.Clear();
        HideSlashPopup();
        StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
        return true;
    }

    string TryOpenPathSlash(string argsTail)
    {
        if (string.IsNullOrWhiteSpace(argsTail))
            return "usage: /open path[:line]\nexample: /open LatchPaint.cs:40";

        var raw = argsTail.Trim().Trim('"');
        int? line = null;
        var path = raw;
        var colon = raw.LastIndexOf(':');
        if (colon > 1
            && colon < raw.Length - 1
            && int.TryParse(raw[(colon + 1)..], out var ln)
            && ln > 0
            && !raw[(colon + 1)..].Contains('\\')
            && !raw[(colon + 1)..].Contains('/'))
        {
            path = raw[..colon];
            line = ln;
        }

        if (!Path.IsPathRooted(path))
        {
            var root = _session.WorkspaceRoot;
            if (!string.IsNullOrWhiteSpace(root))
                path = Path.Combine(root, path);
        }

        if (!File.Exists(path))
            return $"not found: {path}";

        OpenCodeFile(path, line);
        return line is int L ? $"opened {path}:{L}" : $"opened {path}";
    }

    void AppendSlashBubble(string path, string body)
    {
        _feed.Add(new ChatBubble("slash", $"{path}\n{body}", DateTime.Now.ToString("HH:mm:ss")));
        FeedScroll.ScrollToEnd();
    }

    string BuildGlassStatusSlashBody()
        => $"workspace: {_session.WorkspaceRoot}\n"
           + $"intercom forward: {_session.IsIntercomForward}\n"
           + $"status: {StatusText.Text}\n"
           + $"subtitle: {IntercomSubtitle.Text}";

    string BuildGlassTopicsSlashBody()
    {
        if (_topics.Count == 0)
            return "(no topics yet — journal empty or single quiet gap; send a few messages)";
        return string.Join('\n', _topics.Select(t =>
            $"{(t.IsSelected ? "*" : " ")} {t.Title} ({t.EntryIds.Count})"));
    }

    static string BuildGlassLetterSlashBody()
        => "Canon (CDP):\n"
           + "https://github.com/AI-Guiders/cdp-mcp/tree/main/docs/open-letters\n\n"
           + "Attribution (CIDE):\n"
           + "https://github.com/AI-Guiders/cascade-ide/tree/main/docs/open-letters";
}
