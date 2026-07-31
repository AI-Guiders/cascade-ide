#nullable enable
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Highlighting;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;
    readonly GlassSession _session;
    readonly SoftOrganChromeAggregator _softOrgans = new();
    readonly EicasBandAggregator _eicas = new();
    readonly ObservableCollection<ChatBubble> _feed = new();
    readonly GlassHostWindows _hosts;
    bool _hostsReady;

    public MainWindow()
    {
        InitializeComponent();
        MessageFeed.ItemsSource = _feed;
        _hosts = new GlassHostWindows(this);

        _session = new GlassSession();
        ApplyLayoutFromSession();
        ApplyPrimaryWorkSurface();

        _feed.Add(new ChatBubble(
            "system",
            "MVP: AvalonEdit (top) + Intercom (bottom). Multi-window hosts for ()()() / (P)(F)(M).",
            DateTime.Now.ToString("HH:mm")));

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
        // MVP dogfood: both always on Forward. CIDE keeps primary_work_surface=intercom in shared settings.toml.
        IntercomSurface.Visibility = Visibility.Visible;
        EditorSurface.Visibility = Visibility.Visible;
        ForwardTitle.Text = "F · Editor + Intercom";
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
        CodeEditor.Options.EnableHyperlinks = false;
        EditorPathLabel.Text = path;
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

                _feed.Add(new ChatBubble(
                    view.RoleLabel,
                    view.Body,
                    view.WhenLabel));

                while (_feed.Count > 40)
                    _feed.RemoveAt(0);

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
    }

    void MfdPages_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateMfdBody();

    void UpdateMfdBody()
    {
        if (MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString() ?? "?";
        MfdBody.Text = page switch
        {
            "Terminal" => "Terminal page host.\n\nConPTY / shell organ wires in later peels.\nNow: page chrome only (like CIDE MfdShell).",
            "SolutionExplorer" => "Solution Explorer host.\n\nTree of CascadeIDE.sln / open workspace — later.",
            "SemanticMap" => "Semantic Map host.\n\nGraph surface later (not adjacency dump).",
            "Editor" => "Editor page (when Forward=Intercom, docs live here — CIDE pattern).",
            "Chat" => "Chat/Intercom also on M when needed; primary Intercom is Forward.",
            _ => $"{page} page host.\n\nInstrument content peels later."
        };
        RefreshEicasHealth();
    }

    void SendBtn_OnClick(object sender, RoutedEventArgs e)
    {
        /* reply latch later */
    }

    public sealed record ChatBubble(string Role, string Body, string When);
}
