#nullable enable
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;
    readonly GlassSession _session;
    readonly EicasBandAggregator _eicas = new();
    readonly GlassHostWindows _hosts;
    bool _hostsReady;

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
}
