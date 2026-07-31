#nullable enable
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;
    readonly GlassSession _session;
    readonly SoftOrganChromeAggregator _softOrgans = new();
    readonly EicasBandAggregator _eicas = new();
    readonly ObservableCollection<ChatBubble> _feed = new();
    readonly ObservableCollection<GlassIntercomTopics.Topic> _topics = new();
    readonly GlassHostWindows _hosts;
    readonly HashSet<string> _seenIntercomIds = new(StringComparer.OrdinalIgnoreCase);
    bool _hostsReady;
    string? _editorPath;
    string? _selectedTopicId;

    public MainWindow()
    {
        InitializeComponent();
        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
        MessageFeed.ItemsSource = _feed;
        TopicCards.ItemsSource = _topics;
        _hosts = new GlassHostWindows(this);

        _session = new GlassSession();
        ApplyLayoutFromSession();
        ApplyPrimaryWorkSurface();

        LoadIntercomHistory();

        TryOpenDogfoodFile();

        _latches = new LatchHub();
        _latches.IntercomChanged += OnIntercomChanged;
        _latches.PresentationChanged += OnPresentationChanged;
        _latches.SoftOrganChanged += OnSoftOrganChanged;
        _latches.AlertChanged += OnAlertChanged;
        _latches.QrhChanged += OnQrhChanged;
        _latches.Start();

        StatusText.Text =
            $"glass · {_session.Layout.Topology} · cols={_session.Layout.ColumnDefinitions} · {_latches.StateRoot}";
        Loaded += (_, _) =>
        {
            _hostsReady = true;
            SyncHostWindows();
        };
        Closed += (_, _) =>
        {
            _hosts.Dispose();
            _latches.Dispose();
        };
        UpdateMfdBody();
        RefreshEicasHealth();
    }


    void LoadIntercomHistory()
    {
        try
        {
            RebuildIntercomFeedFromJournal();
        }
        catch
        {
            /* best-effort */
        }
    }

    void RebuildIntercomFeedFromJournal()
    {
        var entries = GlassIntercomJournal.LoadTail(80);
        foreach (var e in entries)
        {
            if (e.Id.Length > 0)
                _seenIntercomIds.Add(e.Id);
        }

        _topics.Clear();
        foreach (var t in GlassIntercomTopics.Cluster(entries))
            _topics.Add(t);

        _feed.Clear();
        _feed.Add(new ChatBubble(
            "system",
            "MVP: Forward respects primary_work_surface. Intercom→editor on M; dark AvalonEdit theme. Virtual History + topic cards.",
            DateTime.Now.ToString("HH:mm")));

        IEnumerable<GlassIntercomJournal.Entry> shown = entries;
        if (_selectedTopicId is { Length: > 0 } topicId)
        {
            var topic = _topics.FirstOrDefault(t =>
                string.Equals(t.Id, topicId, StringComparison.OrdinalIgnoreCase));
            if (topic is not null)
            {
                var allow = topic.EntryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                shown = entries.Where(e => allow.Contains(e.Id));
            }
        }

        foreach (var e in shown)
            _feed.Add(new ChatBubble(e.RoleLabel, e.Body, e.WhenLabel));

        while (_feed.Count > 81)
            _feed.RemoveAt(1);
    }

    void TopicAllBtn_OnClick(object sender, RoutedEventArgs e)
    {
        _selectedTopicId = null;
        RebuildIntercomFeedFromJournal();
        FeedScroll.ScrollToEnd();
    }

    void TopicCard_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id } || id.Length == 0)
            return;
        _selectedTopicId = id;
        RebuildIntercomFeedFromJournal();
        FeedScroll.ScrollToEnd();
    }

    void ApplyLayoutFromSession()
    {
        WpfMainGridColumns.Apply(MainGrid, _session.Layout.ColumnDefinitions);
        TopologyBadge.Text = _session.Layout.Topology;
        ChromeHint.Text =
            $"settings.toml · {_session.Settings.Workspace.PrimaryWorkSurface} · tier={_session.Settings.Display.Presentation.Tier}" +
            (_session.Layout.ParseOk ? "" : $" · parse fail: {_session.Layout.ParseError}");
        SyncHostWindows();
    }

    void SyncHostWindows()
    {
        if (!_hostsReady)
            return;
        _hosts.Sync(_session.Layout.Flags);
    }

    void ApplyPrimaryWorkSurface()
    {
        if (_session.IsIntercomForward)
        {
            // ADR 0120: Intercom owns Forward; docs open on M · Editor.
            ForwardTitle.Text = "F · Intercom";
            ForwardEditorRow.Height = new GridLength(0);
            ForwardSplitRow.Height = new GridLength(0);
            ForwardIntercomRow.Height = new GridLength(1, GridUnitType.Star);
            ForwardEditorHost.Visibility = Visibility.Collapsed;
            ForwardSplit.Visibility = Visibility.Collapsed;
            IntercomSurface.Visibility = Visibility.Visible;
            MountEditor(MfdEditorHost);
            SelectMfdPage("Editor");
        }
        else
        {
            ForwardTitle.Text = "F · Editor";
            ForwardEditorRow.Height = new GridLength(1, GridUnitType.Star);
            ForwardSplitRow.Height = new GridLength(0);
            ForwardIntercomRow.Height = new GridLength(0);
            ForwardEditorHost.Visibility = Visibility.Visible;
            ForwardSplit.Visibility = Visibility.Collapsed;
            IntercomSurface.Visibility = Visibility.Collapsed;
            MountEditor(ForwardEditorHost);
            RefreshMfdEditorVisibility();
        }
    }

    void MountEditor(ContentControl host)
    {
        if (ReferenceEquals(EditorChrome.Parent, host))
            return;

        switch (EditorChrome.Parent)
        {
            case ContentControl cc:
                cc.Content = null;
                break;
            case Panel panel:
                panel.Children.Remove(EditorChrome);
                break;
        }

        host.Content = EditorChrome;
        RefreshMfdEditorVisibility();
    }

    void RefreshMfdEditorVisibility()
    {
        if (MfdEditorHost is null || MfdBody is null)
            return;

        var editorOnM = ReferenceEquals(EditorChrome.Parent, MfdEditorHost);
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = editorOnM && string.Equals(page, "Editor", StringComparison.OrdinalIgnoreCase);
        MfdEditorHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        MfdBody.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
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

    void TryOpenDogfoodFile()
    {
        var here = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
        if (string.IsNullOrWhiteSpace(here))
        {
            EditorPathLabel.Text = "(no assembly dir)";
            return;
        }

        var src = Path.GetFullPath(Path.Combine(here, "..", "..", "..", "MainWindow.xaml.cs"));
        if (!File.Exists(src))
        {
            EditorPathLabel.Text = "(dogfood MainWindow.xaml.cs not found)";
            return;
        }

        OpenCodeFile(src);
    }

    void OpenCodeFile(string path)
    {
        CodeEditor.Load(path);
        CodeEditor.SyntaxHighlighting =
            HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path))
            ?? HighlightingManager.Instance.GetDefinition("C#");
        GlassAvalonEditTheme.ApplyDarkReadable(CodeEditor);
        _editorPath = path;
        EditorPathLabel.Text = path;

        if (_session.IsIntercomForward)
        {
            MountEditor(MfdEditorHost);
            SelectMfdPage("Editor");
        }

        RefreshMfdEditorVisibility();
    }

    void OpenFileBtn_OnClick(object sender, RoutedEventArgs e) => TryPickOpenFile();

    void SaveFileBtn_OnClick(object sender, RoutedEventArgs e) => TrySaveEditor();

    void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;

        if (e.Key == Key.S)
        {
            TrySaveEditor();
            e.Handled = true;
        }
        else if (e.Key == Key.O)
        {
            TryPickOpenFile();
            e.Handled = true;
        }
    }

    void TryPickOpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open in Glass editor",
            Filter = "Code|*.cs;*.xaml;*.csproj;*.json;*.md;*.toml;*.txt|All|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (!string.IsNullOrWhiteSpace(_editorPath) && File.Exists(_editorPath))
            dlg.InitialDirectory = Path.GetDirectoryName(_editorPath);
        else if (!string.IsNullOrWhiteSpace(_session.WorkspaceRoot) && Directory.Exists(_session.WorkspaceRoot))
            dlg.InitialDirectory = _session.WorkspaceRoot;

        if (dlg.ShowDialog(this) == true)
            OpenCodeFile(dlg.FileName);
    }

    void TrySaveEditor()
    {
        if (string.IsNullOrWhiteSpace(_editorPath))
        {
            StatusText.Text = "glass · save skipped · no file open";
            return;
        }

        try
        {
            CodeEditor.Save(_editorPath);
            StatusText.Text = $"glass · saved · {Path.GetFileName(_editorPath)} · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"glass · save fail · {ex.Message}";
        }
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
                RebuildIntercomFeedFromJournal();
                FeedScroll.ScrollToEnd();
                StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · intercom fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnPresentationChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintPresentation(raw);
                PlanTitle.Text = string.IsNullOrWhiteSpace(view.Headline) ? "Presentation" : view.Headline;
                PlanMeta.Text = view.Detail;

                var layout = _session.ApplyTopology(view.Topology);
                WpfMainGridColumns.Apply(MainGrid, layout.ColumnDefinitions);
                TopologyBadge.Text = layout.Topology;
                SyncHostWindows();

                SelectMfdPage(view.MfdPage);
                RefreshEicasHealth();
                StatusText.Text =
                    $"glass · {view.StatusLine} · cols={layout.ColumnDefinitions} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · presentation fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnSoftOrganChanged(string organId, string? chromeHint)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _softOrgans.Apply(organId, chromeHint);
            var band = _softOrgans.BandLine;
            if (string.IsNullOrWhiteSpace(band))
            {
                SoftOrganHint.Text = "";
                SoftOrganHint.Visibility = Visibility.Collapsed;
            }
            else
            {
                SoftOrganHint.Text = band;
                SoftOrganHint.Visibility = Visibility.Visible;
            }
        }, DispatcherPriority.Background);
    }

    void OnAlertChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintAlert(raw);
                _eicas.Apply("alert", view?.StatusLine);
                RefreshEicasHealth();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · alert fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnQrhChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintQrh(raw);
                _eicas.Apply("qrh", view?.StatusLine);
                RefreshEicasHealth();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · qrh fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void RefreshEicasHealth()
    {
        if (MfdHealth is null)
            return;

        var line = _eicas.BandLine;
        if (!string.IsNullOrWhiteSpace(line))
        {
            MfdHealth.Text = line;
            return;
        }

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString() ?? "?";
        MfdHealth.Text = $"EICAS · idle · page={page}";
    }

    void SelectMfdPage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page) || MfdPages is null)
            return;

        foreach (var item in MfdPages.Items)
        {
            if (item is ListBoxItem lbi &&
                string.Equals(lbi.Content?.ToString(), page, StringComparison.OrdinalIgnoreCase))
            {
                MfdPages.SelectedItem = lbi;
                return;
            }
        }

        // 0-sync: CabinGlass may name a page before XAML list catches up — ensure selectable.
        var created = new ListBoxItem { Content = page.Trim() };
        MfdPages.Items.Add(created);
        MfdPages.SelectedItem = created;
    }

    void MfdPages_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateMfdBody();

    void UpdateMfdBody()
    {
        RefreshMfdEditorVisibility();

        if (MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString() ?? "?";
        if (string.Equals(page, "Editor", StringComparison.OrdinalIgnoreCase)
            && ReferenceEquals(EditorChrome.Parent, MfdEditorHost))
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        MfdBody.Text = page switch
        {
            "Terminal" => "Terminal page host.\n\nConPTY / shell organ wires in later peels.\nNow: page chrome only (like CIDE MfdShell).",
            "SolutionExplorer" => "Solution Explorer host.\n\nTree of CascadeIDE.sln / open workspace — later.",
            "SemanticMap" => "Semantic Map host.\n\nGraph surface later (not adjacency dump).",
            "Tests" => "Tests page host.\n\ncdp_test / test_desk projection (CabinGlass catalog).",
            "HybridIndex" => "Hybrid Index host.\n\ncodebase_index organ → glass MFD (stub peel).",
            "RelatedFiles" => "Related Files host.\n\nfind_desk / related organ projection.",
            "Correspondence" => "Correspondence host.\n\ncrs organ projection — later inbox chrome.",
            "MarkdownPreview" => "Markdown Preview host.\n\nmd_preview / md_author projection.",
            "WebAiPortal" => "Web / AI Portal host.\n\nbrowser organ projection.",
            "AiChatSettings" => "AI Chat Settings host.\n\noptions / ignite projection (settings.toml SSOT).",
            "Editor" => _session.IsIntercomForward
                ? "Editor page — AvalonEdit mounts here when Forward=intercom (ADR 0120)."
                : "Editor is on Forward (primary_work_surface=editor).",
            "Chat" => "Chat/Intercom also on M when needed; primary Intercom is Forward.",
            _ => $"{page} page host.\n\nInstrument content peels later. (CabinGlass catalog may select this.)"
        };
        RefreshEicasHealth();
    }

    void SendBtn_OnClick(object sender, RoutedEventArgs e) => TrySendComposer();

    void ComposerBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
            return;

        // Shift+Enter = newline; Enter / Ctrl+Enter = send
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            return;

        e.Handled = true;
        TrySendComposer();
    }

    void ComposerBox_OnGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (ComposerBox.Text is "Message @PF…" or "Message @PM…")
            ComposerBox.Clear();
    }

    void TrySendComposer()
    {
        var raw = ComposerBox.Text;
        var sent = GlassIntercomSend.TrySend(raw);
        if (sent is null)
        {
            StatusText.Text = "glass · intercom · empty — nothing sent";
            return;
        }

        _seenIntercomIds.Add(sent.Id);
        RebuildIntercomFeedFromJournal();

        ComposerBox.Clear();
        FeedScroll.ScrollToEnd();
        StatusText.Text = $"glass · intercom · sent {sent.Id} · @PM→@PF · {DateTime.Now:HH:mm:ss}";
    }

    public sealed record ChatBubble(string Role, string Body, string When);
}
