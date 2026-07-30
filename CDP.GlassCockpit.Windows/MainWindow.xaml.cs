#nullable enable
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;
    readonly GlassSession _session;
    readonly ObservableCollection<ChatBubble> _feed = new();

    public MainWindow()
    {
        InitializeComponent();
        MessageFeed.ItemsSource = _feed;

        _session = new GlassSession();
        ApplyLayoutFromSession();
        ApplyPrimaryWorkSurface();

        _feed.Add(new ChatBubble(
            "system",
            "Forward = Intercom. Settings/topology from CascadeIDE settings.toml + presentation latch.",
            DateTime.Now.ToString("HH:mm")));

        _latches = new LatchHub();
        _latches.IntercomChanged += OnIntercomChanged;
        _latches.PresentationChanged += OnPresentationChanged;
        _latches.Start();

        StatusText.Text =
            $"glass · {_session.Layout.Topology} · cols={_session.Layout.ColumnDefinitions} · {_latches.StateRoot}";
        Closed += (_, _) => _latches.Dispose();
        UpdateMfdBody();
    }

    void ApplyLayoutFromSession()
    {
        WpfMainGridColumns.Apply(MainGrid, _session.Layout.ColumnDefinitions);
        TopologyBadge.Text = _session.Layout.Topology;
        ChromeHint.Text =
            $"settings.toml · {_session.Settings.PrimaryWorkSurface} · tier={_session.Settings.Tier}" +
            (_session.Layout.ParseOk ? "" : $" · parse fail: {_session.Layout.ParseError}");
    }

    void ApplyPrimaryWorkSurface()
    {
        var intercom = _session.IsIntercomForward;
        IntercomSurface.Visibility = intercom ? Visibility.Visible : Visibility.Collapsed;
        EditorSurface.Visibility = intercom ? Visibility.Collapsed : Visibility.Visible;
        ForwardTitle.Text = intercom ? "F · Intercom" : "F · Editor";
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

                SelectMfdPage(view.MfdPage);
                MfdHealth.Text = $"EICAS · {view.StatusLine}";
                StatusText.Text =
                    $"glass · {view.StatusLine} · cols={layout.ColumnDefinitions} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · presentation fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void SelectMfdPage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
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
        var page = (MfdPages.SelectedItem as ListBoxItem)?.Content?.ToString() ?? "?";
        MfdBody.Text = page switch
        {
            "Terminal" => "Terminal page host.\n\nConPTY / shell organ wires in later peels.\nNow: page chrome only (like CIDE MfdShell).",
            "SolutionExplorer" => "Solution Explorer host.\n\nTree of CascadeIDE.sln / open workspace — later.",
            "SemanticMap" => "Semantic Map host.\n\nGraph surface later (not adjacency dump).",
            "Editor" => "Editor page (when Forward=Intercom, docs live here — CIDE pattern).",
            "Chat" => "Chat/Intercom also on M when needed; primary Intercom is Forward.",
            _ => $"{page} page host.\n\nInstrument content peels later."
        };
        MfdHealth.Text = $"EICAS · page={page}";
    }

    void SendBtn_OnClick(object sender, RoutedEventArgs e)
    {
        /* reply latch later */
    }

    public sealed record ChatBubble(string Role, string Body, string When);
}
