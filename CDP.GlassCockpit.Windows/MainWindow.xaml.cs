#nullable enable
using System.Windows;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;
    readonly GlassSession _session;

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
        InitIntercomSlash();
        InitCommandPalette();
        InitCascadeChord();

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
}
