#nullable enable
using System.Windows;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;
    readonly GlassSession _session;
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

}
