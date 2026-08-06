#nullable enable

using System.IO;
using System.Windows;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass slash command run + /open /attach + bubble helpers (peeled from IntercomSlash UI).</summary>
public partial class MainWindow
{
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
            SelectMfdPage("FlightDataStorage", sticky: true);
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

        if (cmd.Id is "attach" or "attach_selection" or "attach_file")
        {
            var attachArgs = cmd.Id == "attach_selection" ? "" : argsTail;
            if (cmd.Id == "attach_file" && string.IsNullOrWhiteSpace(attachArgs))
            {
                AppendSlashBubble(cmd.Path,
                    "usage: /intercom attach file path[:line[-line]]\n(or /attach with AvalonEdit selection)");
                ComposerBox.Clear();
                HideSlashPopup();
                StatusText.Text = $"glass · slash · {cmd.Path} · usage · {DateTime.Now:HH:mm:ss}";
                return true;
            }

            var attachBody = TryAttachSlash(attachArgs);
            AppendSlashBubble(cmd.Path, attachBody);
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "attach_scope")
        {
            AppendSlashBubble(cmd.Path,
                "Glass has no Roslyn caret-scope attach yet (DIG REJECT SoftFL).\n"
                + "Use /attach or /intercom attach selection · /intercom attach file path[:line]");
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · refuse · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id is "topic_overview" or "topic_cards")
        {
            ShowIntercomTopicOverview();
            AppendSlashBubble(cmd.Path, $"topics overview · {_topics.Count} cards");
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "topic_next")
        {
            SelectIntercomTopicNext();
            AppendSlashBubble(cmd.Path, StatusText.Text ?? "topic next");
            ComposerBox.Clear();
            HideSlashPopup();
            return true;
        }

        if (cmd.Id == "topic_prev")
        {
            SelectIntercomTopicPrev();
            AppendSlashBubble(cmd.Path, StatusText.Text ?? "topic prev");
            ComposerBox.Clear();
            HideSlashPopup();
            return true;
        }

        if (cmd.Id == "topic_open")
        {
            if (!string.IsNullOrWhiteSpace(argsTail)
                && int.TryParse(argsTail.Trim(), out var topicOrdinal)
                && topicOrdinal > 0)
            {
                RebuildIntercomFeedFromJournal(stickEnd: false);
                var clustered = GlassIntercomTopics.Cluster(
                    GlassIntercomJournal.LoadTail(TopicClusterTail));
                var pick = CascadeIDE.Intercom.GlassIntercomTopicFollow.IdByOrdinal(clustered, topicOrdinal);
                if (pick is null)
                    AppendSlashBubble(cmd.Path, $"no topic #{topicOrdinal} (have {_topics.Count})");
                else
                {
                    _isTopicOverviewMode = false;
                    ApplyIntercomTopicSelection(pick);
                    AppendSlashBubble(cmd.Path, $"opened #{topicOrdinal} · {ShortTopicLabel(pick)}");
                }
            }
            else
            {
                EnterIntercomFocusedTopic();
                AppendSlashBubble(cmd.Path, StatusText.Text ?? "topic enter");
            }

            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id is "spine_show" or "spine_toggle")
        {
            if (cmd.Id == "spine_toggle"
                && ProductSpineStrip.Visibility == Visibility.Visible)
            {
                ProductSpineStrip.Visibility = Visibility.Collapsed;
                AppendSlashBubble(cmd.Path, "spine · hidden");
            }
            else
            {
                SyncProductSpineChrome();
                var spineBody = ProductSpineStrip.Visibility == Visibility.Visible
                    ? ProductSpineStrip.Text
                    : "spine · empty latch (product-spine-LATEST.json)";
                AppendSlashBubble(cmd.Path, spineBody ?? "spine");
            }

            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "message_find")
        {
            if (!GlassIntercomMessageFind.TryParseNeedle(argsTail, out var needle, out var parseErr))
            {
                AppendSlashBubble(cmd.Path, parseErr);
                ComposerBox.Clear();
                HideSlashPopup();
                StatusText.Text = $"glass · slash · {cmd.Path} · usage · {DateTime.Now:HH:mm:ss}";
                return true;
            }

            var hits = new List<GlassIntercomMessageFind.Hit>(_feed.Count);
            for (var i = 0; i < _feed.Count; i++)
            {
                var b = _feed[i];
                var ordinal = b.Ordinal > 0 ? b.Ordinal : i + 1;
                hits.Add(new GlassIntercomMessageFind.Hit(ordinal, b.Body, b.Chips));
            }

            var ordinals = GlassIntercomMessageFind.MatchOrdinals(needle, hits);
            var apply = GlassIntercomMessageSelect.ApplyOrdinals(_feed.Count, ordinals, out var sel);
            string reply;
            if (string.Equals(apply, "OK", StringComparison.Ordinal))
            {
                _messageSelect = sel;
                ApplyMessageSelectToFeed();
                reply = GlassIntercomMessageFind.FormatResult(needle, ordinals)
                        + "\n" + GlassIntercomMessageSelect.FormatOk(sel);
            }
            else
            {
                reply = GlassIntercomMessageFind.FormatResult(needle, ordinals) + "\n" + apply;
            }

            AppendSlashBubble(cmd.Path, reply);
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · find · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "message_anchors")
        {
            var scopeSelected = _messageSelect.ActiveOrdinal > 0;
            var lines = new List<string>();
            for (var i = 0; i < _feed.Count; i++)
            {
                var b = _feed[i];
                var ordinal = b.Ordinal > 0 ? b.Ordinal : i + 1;
                if (scopeSelected && !_messageSelect.Highlighted.Contains(ordinal))
                    continue;

                var chips = b.Chips is { Count: > 0 }
                    ? b.Chips
                    : GlassAttachChipPeel.FromBody(b.Body);
                if (chips.Count == 0)
                    continue;

                lines.Add($"#{ordinal} · {string.Join(" ", chips.Select(static c => c.Bracket))}");
            }

            var reply = lines.Count == 0
                ? (scopeSelected
                    ? "anchors · none on selected messages (select first or clear select)"
                    : "anchors · none in feed")
                : "anchors · " + (scopeSelected ? "selected" : "feed") + "\n" + string.Join("\n", lines);

            AppendSlashBubble(cmd.Path, reply);
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · anchors · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "message_relate")
        {
            AppendSlashBubble(cmd.Path,
                "Glass has no Avalonia IntercomCodeRef relate/event-log peel yet (DIG REJECT SoftFL).\n"
                + "A4 denser shipped: /intercom message find [path:line] · /intercom message anchors\n"
                + "CIDE /intercom message relate remains Avalonia SSOT");
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · refuse · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "citizen")
        {
            if (string.IsNullOrWhiteSpace(argsTail))
            {
                AppendSlashBubble(cmd.Path, "usage: /citizen your message\nTalks to habitat citizen (dialog), not guest Кир @PF.");
                ComposerBox.Clear();
                HideSlashPopup();
                StatusText.Text = $"glass · slash · {cmd.Path} · usage · {DateTime.Now:HH:mm:ss}";
                return true;
            }

            var sent = GlassCitizenDialogRequest.TryEnqueue(argsTail, workspaceRoot: _session.WorkspaceRoot);
            if (sent is null)
            {
                StatusText.Text = "glass · citizen · enqueue failed";
                return true;
            }

            _seenIntercomIds.Add(sent.Id);
            RebuildIntercomFeedFromJournal(stickEnd: true);
            ComposerBox.Clear();
            HideSlashPopup();
            PublishPmIdle();
            StatusText.Text =
                CascadeIDE.Intercom.CitizenDialogRequestStatus.FormatLine(sent.Id, "pending", null)
                + $" · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id is "select" or "message_next" or "message_prev")
        {
            string reply;
            if (cmd.Id == "message_next" || cmd.Id == "message_prev")
            {
                var delta = cmd.Id == "message_next" ? 1 : -1;
                var apply = GlassIntercomMessageSelect.ApplyOffset(_feed.Count, _messageSelect, delta, out var sel);
                if (!string.Equals(apply, "OK", StringComparison.Ordinal))
                    reply = apply;
                else
                {
                    _messageSelect = sel;
                    ApplyMessageSelectToFeed();
                    reply = GlassIntercomMessageSelect.FormatOk(sel);
                }
            }
            else if (GlassIntercomMessageSelect.IsClear(argsTail))
            {
                _messageSelect = GlassIntercomMessageSelect.Empty;
                ApplyMessageSelectToFeed();
                reply = GlassIntercomMessageSelect.FormatOk(_messageSelect);
            }
            else if (!GlassIntercomMessageSelect.TryParseSegments(argsTail, out var segments, out var parseErr))
            {
                reply = parseErr; // includes ADR usage when ArgTail empty
            }
            else
            {
                var apply = GlassIntercomMessageSelect.ApplySegments(_feed.Count, segments, out var sel);
                if (!string.Equals(apply, "OK", StringComparison.Ordinal))
                    reply = apply;
                else
                {
                    _messageSelect = sel;
                    ApplyMessageSelectToFeed();
                    reply = GlassIntercomMessageSelect.FormatOk(sel);
                }
            }

            AppendSlashBubble(cmd.Path, reply);
            ApplyMessageSelectToFeed();
            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        if (cmd.Id == "topics")
        {
            RebuildIntercomFeedFromJournal(stickEnd: false);
            if (!string.IsNullOrWhiteSpace(argsTail)
                && int.TryParse(argsTail.Trim(), out var ordinal)
                && ordinal > 0)
            {
                var clustered = GlassIntercomTopics.Cluster(
                    GlassIntercomJournal.LoadTail(TopicClusterTail));
                var pick = CascadeIDE.Intercom.GlassIntercomTopicFollow.IdByOrdinal(clustered, ordinal);
                if (pick is null)
                {
                    AppendSlashBubble(cmd.Path,
                        $"no topic #{ordinal} (have {_topics.Count})\n" + BuildGlassTopicsSlashBody());
                }
                else
                {
                    _selectedTopicId = pick;
                    RebuildIntercomFeedFromJournal(stickEnd: true);
                    AppendSlashBubble(cmd.Path, $"selected #{ordinal}\n" + BuildGlassTopicsSlashBody());
                }
            }
            else
            {
                AppendSlashBubble(cmd.Path,
                    BuildGlassTopicsSlashBody()
                    + "\n\n(usage: /topics · /topics N — 1-based · 30m quiet gap)");
            }

            ComposerBox.Clear();
            HideSlashPopup();
            StatusText.Text = $"glass · slash · {cmd.Path} · {DateTime.Now:HH:mm:ss}";
            return true;
        }

        var body = cmd.Id switch
        {
            "help" => GlassSlashCatalog.FormatHelp(),
            "status" => BuildGlassStatusSlashBody(),
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

    string TryAttachSlash(string argsTail)
    {
        string path;
        int? lineStart = null;
        int? lineEnd = null;

        if (!string.IsNullOrWhiteSpace(argsTail))
        {
            var raw = argsTail.Trim().Trim('"');
            path = raw;
            var colon = raw.LastIndexOf(':');
            if (colon > 1
                && colon < raw.Length - 1
                && !raw[(colon + 1)..].Contains('\\')
                && !raw[(colon + 1)..].Contains('/'))
            {
                var linePart = raw[(colon + 1)..];
                var dash = linePart.IndexOf('-');
                if (dash > 0
                    && int.TryParse(linePart[..dash], out var a)
                    && int.TryParse(linePart[(dash + 1)..], out var b)
                    && a > 0
                    && b >= a)
                {
                    path = raw[..colon];
                    lineStart = a;
                    lineEnd = b;
                }
                else if (int.TryParse(linePart, out var ln) && ln > 0)
                {
                    path = raw[..colon];
                    lineStart = ln;
                }
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_editorPath))
                return "usage: /attach [path[:line[-line]]]\n(or open a file / select lines in AvalonEdit)";

            path = _editorPath;
            var sel = CodeEditor.TextArea.Selection;
            if (!sel.IsEmpty && sel.SurroundingSegment is { } seg)
            {
                var startOff = seg.Offset;
                var endOff = Math.Max(seg.Offset, seg.EndOffset - 1);
                lineStart = CodeEditor.Document.GetLineByOffset(startOff).LineNumber;
                lineEnd = CodeEditor.Document.GetLineByOffset(endOff).LineNumber;
                if (lineEnd < lineStart)
                    (lineStart, lineEnd) = (lineEnd, lineStart);
                if (lineEnd == lineStart)
                    lineEnd = null;
            }
            else
            {
                lineStart = CodeEditor.TextArea.Caret.Line;
            }
        }

        if (!Path.IsPathRooted(path))
        {
            var root = _session.WorkspaceRoot;
            if (!string.IsNullOrWhiteSpace(root))
                path = Path.Combine(root, path);
        }

        var display = path;
        var rootWs = _session.WorkspaceRoot;
        if (!string.IsNullOrWhiteSpace(rootWs)
            && path.StartsWith(rootWs, StringComparison.OrdinalIgnoreCase))
        {
            display = path[rootWs.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        var bracket = GlassAttachChipPeel.FormatBracket(display, lineStart, lineEnd);
        var cur = ComposerBox.Text ?? "";
        if (GlassIntercomLane.IsComposerPlaceholder(cur))
            cur = "";
        ComposerBox.Text = string.IsNullOrWhiteSpace(cur)
            ? bracket
            : cur.TrimEnd() + " " + bracket;
        ComposerBox.CaretIndex = ComposerBox.Text.Length;
        ComposerBox.Focus();
        return $"chip → composer {bracket}";
    }

    void AttachChip_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: GlassAttachChip chip })
            return;

        var path = GlassAttachChipPeel.ResolvePath(chip.File, _session.WorkspaceRoot);
        if (path.Length == 0 || !File.Exists(path))
        {
            StatusText.Text = $"glass · attach · missing {chip.File}";
            return;
        }

        OpenCodeFile(path, chip.LineStart, chip.LineEnd);
        StatusText.Text = $"glass · attach · {chip.Label} · {DateTime.Now:HH:mm:ss}";
    }

    void AppendSlashBubble(string path, string body)
    {
        var chips = GlassAttachChipPeel.ResolveAgainstDisk(
            GlassAttachChipPeel.FromBody(body),
            _session.WorkspaceRoot);
        var display = chips.Count > 0
            ? GlassAttachChipPeel.StripBracketsForDisplay(body)
            : body;
        if (string.IsNullOrWhiteSpace(display) && chips.Count > 0)
            display = "(attach)";
        _feed.Add(new ChatBubble("slash", $"{path}\n{display}", DateTime.Now.ToString("HH:mm:ss"), chips));
        FeedScroll.ScrollToEnd();
    }

    string BuildGlassStatusSlashBody()
    {
        int? caret = null;
        bool? dirty = null;
        if (CodeEditor?.Document is not null)
        {
            caret = CodeEditor.TextArea.Caret.Line;
            dirty = CodeEditor.IsModified;
        }

        return GlassIopStatusGlance.Format(new GlassIopStatusGlance.Snapshot(
            WorkspaceRoot: _session.WorkspaceRoot,
            IntercomForward: _session.IsIntercomForward,
            StatusLine: StatusText.Text,
            Subtitle: IntercomSubtitle.Text,
            EditorPath: _editorPath,
            CaretLine: caret,
            EditorDirty: dirty,
            MfdPage: CurrentMfdPage(),
            Topology: _session.Layout.Topology,
            ColumnDefinitions: _session.Layout.ColumnDefinitions,
            LatchStateRoot: _latches.StateRoot));
    }

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
