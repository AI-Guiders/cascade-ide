#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass Ctrl+Q command palette overlay (local catalog).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassPaletteEntry> _paletteEntries = new();

    void InitCommandPalette()
    {
        PaletteList.ItemsSource = _paletteEntries;
        PaletteQuery.TextChanged += (_, _) => RefreshPaletteFilter();
        PaletteList.MouseDoubleClick += (_, _) => ExecutePaletteSelection();
        PaletteQuery.PreviewKeyDown += PaletteQuery_OnPreviewKeyDown;
        PaletteList.PreviewKeyDown += PaletteList_OnPreviewKeyDown;
    }

    void ToggleCommandPalette()
    {
        if (PaletteOverlay?.Visibility == Visibility.Visible)
        {
            CloseCommandPalette();
            return;
        }

        PaletteQuery.Text = "";
        RefreshPaletteFilter();
        CloseCascadeChord();
        CloseOpenFamily();
        SetFloatingOverlay(PaletteOverlay, true);
        PaletteQuery.Focus();
        Keyboard.Focus(PaletteQuery);
    }

    void CloseCommandPalette()
    {
        SetFloatingOverlay(PaletteOverlay, false);
        _paletteEntries.Clear();
    }

    void RefreshPaletteFilter()
    {
        _paletteEntries.Clear();
        var q = PaletteQuery?.Text;

        if (GlassCommandPaletteCatalog.TryGetGoToFileTail(q, out var term))
        {
            var hits = CascadeIDE.SoftInstrument.GlassGoToFileIndex.Search(
                _session.SolutionRoot,
                _session.WorkspaceRoot,
                term);
            foreach (var h in hits)
            {
                _paletteEntries.Add(new GlassPaletteEntry(
                    "goto:" + h.FullPath,
                    h.Title,
                    h.Relative,
                    "f: goto file"));
            }

            if (_paletteEntries.Count == 0)
            {
                _paletteEntries.Add(new GlassPaletteEntry(
                    "goto_empty",
                    "Нет файлов",
                    string.IsNullOrEmpty(term)
                        ? "Открой проект (Ctrl+O → P) или уточни f:имя"
                        : $"Нет совпадений для «{term}»",
                    null));
            }

            PaletteList.SelectedIndex = 0;
            return;
        }

        var hitsCatalog = GlassCommandPaletteCatalog.Filter(q);
        foreach (var h in hitsCatalog)
            _paletteEntries.Add(h);
        PaletteList.SelectedIndex = _paletteEntries.Count > 0 ? 0 : -1;
    }

    void OpenGoToFilePalette()
    {
        CloseCascadeChord();
        CloseOpenFamily();
        PaletteQuery.Text = GlassCommandPaletteCatalog.GoToFilePrefix;
        RefreshPaletteFilter();
        SetFloatingOverlay(PaletteOverlay, true);
        PaletteQuery.CaretIndex = PaletteQuery.Text.Length;
        PaletteQuery.Focus();
        Keyboard.Focus(PaletteQuery);
        StatusText.Text = "goto · f: · Ctrl+P";
    }

    void PaletteQuery_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseCommandPalette();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && _paletteEntries.Count > 0)
        {
            PaletteList.Focus();
            PaletteList.SelectedIndex = 0;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecutePaletteSelection();
            e.Handled = true;
        }
    }

    void PaletteList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseCommandPalette();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecutePaletteSelection();
            e.Handled = true;
        }
    }

    void ExecutePaletteSelection()
    {
        if (PaletteList.SelectedItem is not GlassPaletteEntry entry)
        {
            if (_paletteEntries.Count == 0)
                return;
            entry = _paletteEntries[0];
        }

        if (GlassCommandPaletteCatalog.IsNonExecutableMelodyRow(entry.Id))
            return;

        if (entry.Id.StartsWith("goto:", StringComparison.Ordinal))
        {
            var path = entry.Id["goto:".Length..];
            CloseCommandPalette();
            if (File.Exists(path))
                OpenCodeFile(path);
            else
                StatusText.Text = $"goto · missing · {path}";
            return;
        }

        if (entry.Id == "goto_empty")
            return;

        if (GlassMelodyGlassActions.TryMapRowId(entry.Id, out var glassAction))
        {
            if (glassAction == GlassMelodyGlassActions.RunSelectLines)
            {
                if (!GlassCommandPaletteCatalog.TryGetMelodyTail(PaletteQuery.Text, out var tail)
                    || !GlassMelodyTail.TryParseLineRange(GlassMelodyTail.ArgRemainder(tail), out var start, out var end))
                {
                    StatusText.Text = "glass · c:els — need c:els:<line> or c:els:<start>:<end>";
                    return;
                }

                CloseCommandPalette();
                SelectOpenDocumentLines(start, end);
                return;
            }

            if (glassAction == GlassMelodyGlassActions.RunWebAiPortal)
            {
                if (!GlassCommandPaletteCatalog.TryGetMelodyTail(PaletteQuery.Text, out var waiTail)
                    || !GlassChordMelody.TryResolveParametricWebAi(waiTail, out var url))
                {
                    StatusText.Text = "glass · c:wai — need c:wai:<url>";
                    return;
                }

                CloseCommandPalette();
                RunWebAiPortal(url);
                return;
            }

            CloseCommandPalette();
            RunPaletteEntry(glassAction);
            return;
        }

        CloseCommandPalette();
        RunPaletteEntry(entry.Id);
    }

    void RunPaletteEntry(string id)
    {
        switch (id)
        {
            case GlassMelodyGlassActions.RunGitStatus:
                SelectMfdPage("Git", sticky: true);
                GitStatus_OnClick(this, new RoutedEventArgs());
                break;
            case GlassMelodyGlassActions.RunBuild:
                SelectMfdPage("Build", sticky: true);
                BuildRun_OnClick(this, new RoutedEventArgs());
                break;
            case GlassMelodyGlassActions.RunTests:
                SelectMfdPage("Tests", sticky: true);
                TestsRun_OnClick(this, new RoutedEventArgs());
                break;
            case GlassMelodyGlassActions.RunWebAiPortal:
                RunWebAiPortal(null);
                break;
            case "open_file":
                TryPickOpenFile();
                break;
            case "open_solution":
            case "open_solution_dialog":
                TryPickOpenProject();
                break;
            case "open_folder":
            case "open_folder_dialog":
                TryPickOpenFolder();
                break;
            case "open_recent":
                ShowOpenFamilyRecent();
                break;
            case "open_family":
                BeginOpenFamilyChord();
                break;
            case "go_to_file":
            case "workspace_go_to_file":
                OpenGoToFilePalette();
                break;
            case "save_file":
                TrySaveEditor();
                break;
            case "focus_composer":
                ComposerBox.Focus();
                break;
            case "slash_help":
                TryRunGlassSlash("/help");
                break;
            case "slash_status":
                TryRunGlassSlash("/status");
                break;
            case "slash_topics":
                TryRunGlassSlash("/topics");
                break;
            case "slash_letter":
                TryRunGlassSlash("/letter");
                break;
            case "slash_fds":
                TryRunGlassSlash("/fds");
                break;
            case "slash_attach":
                TryRunGlassSlash("/attach");
                break;
            case "slash_open":
                TryRunGlassSlash("/open"); // empty → park composer (no usage bubble)
                break;
            case "slash_citizen":
                TryRunGlassSlash("/citizen");
                break;
            case "slash_select":
                TryRunGlassSlash("/select"); // bare → last message
                break;
            case "topics_all":
            case "topic_overview":
                ShowIntercomTopicOverview();
                break;
            case "topic_enter":
                EnterIntercomFocusedTopic();
                break;
            case "topic_next":
                SelectIntercomTopicNext();
                break;
            case "topic_prev":
                SelectIntercomTopicPrev();
                break;
            case "feed_page_down":
                PageIntercomFeed(+1);
                break;
            case "feed_page_up":
                PageIntercomFeed(-1);
                break;
            case "message_select_next":
                TryRunGlassSlash("/intercom message next");
                break;
            case "message_select_prev":
                TryRunGlassSlash("/intercom message prev");
                break;
            case "mfd_editor":
                SelectMfdPage("Editor", sticky: true);
                break;
            case "mfd_terminal":
                SelectMfdPage("Terminal", sticky: true);
                break;
            case "mfd_fds":
                SelectMfdPage("FlightDataStorage", sticky: true);
                break;
            case "mfd_domain_board":
                SelectMfdPage("DomainBoard", sticky: true);
                break;
            case "mfd_build":
                SelectMfdPage("Build", sticky: true);
                break;
            case "mfd_tests":
                SelectMfdPage("Tests", sticky: true);
                break;
            case "mfd_git":
                SelectMfdPage("Git", sticky: true);
                break;
            case "mfd_problems":
                SelectMfdPage("Problems", sticky: true);
                break;
            case "mfd_related_files":
                SelectMfdPage("RelatedFiles", sticky: true);
                break;
            case "mfd_semantic_map":
                SelectMfdPage("SemanticMap", sticky: true);
                break;
            case "mfd_correspondence":
                SelectMfdPage("Correspondence", sticky: true);
                break;
            case "mfd_markdown":
                SelectMfdPage("MarkdownPreview", sticky: true);
                break;
            case "mfd_debug_stack":
                SelectMfdPage("DebugStack", sticky: true);
                break;
            case "mfd_webai":
                SelectMfdPage("WebAiPortal", sticky: true);
                break;
            case "mfd_solution_explorer":
                SelectMfdPage("SolutionExplorer", sticky: true);
                break;
            case "mfd_hybrid_index":
                SelectMfdPage("HybridIndex", sticky: true);
                break;
            case "mfd_workspace_health":
                SelectMfdPage("WorkspaceHealth", sticky: true);
                break;
            case "mfd_env_ready":
                SelectMfdPage("EnvironmentReadiness", sticky: true);
                break;
            case "mfd_events":
                SelectMfdPage("Events", sticky: true);
                break;
            case "mfd_hypotheses":
                SelectMfdPage("Hypotheses", sticky: true);
                break;
            case "mfd_chat":
                SelectMfdPage("Chat", sticky: true);
                break;
            case "mfd_here_next":
                SelectMfdPage("HereNext", sticky: true);
                StatusText.Text = $"glass · HERE/NEXT · {DateTime.Now:HH:mm:ss}";
                break;
            case "soft_qrh":
                OpenSoftInstrumentFace("qrh", "qrh");
                break;
            case "soft_ecl":
                OpenSoftInstrumentFace("ecl", "ecl");
                break;
            case "soft_alert":
                OpenSoftInstrumentFace("alert", "alert");
                break;
            case "toggle_pm_oneof_role":
                if (_hosts.TogglePmOneOfRole())
                {
                    // ApplyMainScanOneOfColumns already stamped StatusText for single-TopLevel.
                    if (!_hosts.IsMainScanOneOf)
                        StatusText.Text = $"glass · OneOf · {_hosts.PmOneOfActiveSurface} · {DateTime.Now:HH:mm:ss}";
                }
                else
                    StatusText.Text = "glass · OneOf host not active (need F + P/M channel stack)";
                break;
            default:
                StatusText.Text = $"glass · palette · unknown {id}";
                break;
        }
    }

    /// <summary>Agent surface: open Ctrl+Q palette, optional query, optional execute (melody dogfood).</summary>
    internal string AgentSurfacePalette(string? query, bool execute)
    {
        if (PaletteOverlay?.Visibility != Visibility.Visible)
        {
            PaletteQuery.Text = "";
            RefreshPaletteFilter();
            CloseCascadeChord();
            SetFloatingOverlay(PaletteOverlay, true);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            PaletteQuery.Text = query.Trim();
            RefreshPaletteFilter();
        }

        PaletteQuery.Focus();
        Keyboard.Focus(PaletteQuery);

        string? executed = null;
        string? lastFeed = null;
        if (execute)
        {
            if (_paletteEntries.Count == 0)
                return System.Text.Json.JsonSerializer.Serialize(new { ok = false, error = "no_palette_hits", query = PaletteQuery.Text });

            var before = _feed.Count;
            ExecutePaletteSelection();
            executed = "true";
            if (_feed.Count > 0)
                lastFeed = _feed[^1].Body;
            else if (before == 0)
                lastFeed = StatusText.Text;
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            ok = true,
            open = PaletteOverlay?.Visibility == Visibility.Visible,
            query = PaletteQuery.Text ?? "",
            hits = _paletteEntries.Count,
            top = _paletteEntries.Count > 0 ? _paletteEntries[0].Title : null,
            executed,
            last_feed = lastFeed,
            status = StatusText.Text
        });
    }

    /// <summary>Agent surface: run Glass action / melody command_id / slash text without Ctrl+Q ritual.</summary>
    internal string AgentSurfaceRun(string? action, string? commandId, string? text, string? startRaw, string? endRaw)
    {
        CloseCommandPalette();
        CloseCascadeChord();

        var slash = text?.Trim();
        if (!string.IsNullOrEmpty(slash) && slash[0] == '/')
        {
            var handled = TryRunGlassSlash(slash);
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                ok = handled,
                kind = "slash",
                text = slash,
                error = handled ? null : "slash_not_handled",
                status = StatusText.Text,
                last_feed = _feed.Count > 0 ? _feed[^1].Body : null
            });
        }

        string? glassAction = null;
        if (!string.IsNullOrWhiteSpace(commandId)
            && GlassMelodyGlassActions.TryMapCommandId(commandId, out var mapped))
            glassAction = mapped;
        else if (!string.IsNullOrWhiteSpace(action))
            glassAction = action.Trim();
        else if (!string.IsNullOrWhiteSpace(slash))
            glassAction = slash;

        if (string.IsNullOrWhiteSpace(glassAction))
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                ok = false,
                error = "run_target_required",
                hint = "action=|command_id=|text=/slash…"
            });
        }

        if (glassAction == GlassMelodyGlassActions.RunSelectLines)
        {
            int start;
            int end;
            if (!string.IsNullOrWhiteSpace(startRaw) && int.TryParse(startRaw.Trim(), out start))
            {
                end = !string.IsNullOrWhiteSpace(endRaw) && int.TryParse(endRaw.Trim(), out var e) ? e : start;
            }
            else if (!string.IsNullOrWhiteSpace(slash)
                     && GlassMelodyTail.TryParseLineRange(slash, out start, out var endOpt))
            {
                end = endOpt ?? start;
            }
            else
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    ok = false,
                    error = "select_range_required",
                    hint = "start=/end= or text=L or L:L",
                    action = glassAction
                });
            }

            SelectOpenDocumentLines(start, end);
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                ok = true,
                kind = "select",
                action = glassAction,
                start,
                end,
                status = StatusText.Text
            });
        }

        if (glassAction == GlassMelodyGlassActions.RunWebAiPortal)
        {
            string? url = null;
            if (!string.IsNullOrWhiteSpace(text))
                GlassChordMelody.TryResolveParametricWebAi(text.Trim(), out url);
            RunWebAiPortal(url);
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                ok = true,
                kind = "webai",
                action = glassAction,
                url,
                status = StatusText.Text
            });
        }

        RunPaletteEntry(glassAction);
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            ok = true,
            kind = "action",
            action = glassAction,
            command_id = commandId,
            status = StatusText.Text,
            last_feed = _feed.Count > 0 ? _feed[^1].Body : null
        });
    }


}
