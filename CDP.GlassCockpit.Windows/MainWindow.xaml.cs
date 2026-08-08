#nullable enable
using System.Windows;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;
    readonly GlassSurfaceCommandHub _surface;
    readonly GlassSession _session;

    public MainWindow()
    {
        InitializeComponent();
        WireEicasSoftKeys();
        MfdHosts.Wire(this);
        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
        MessageFeed.ItemsSource = _feed;
        TopicCards.ItemsSource = _topics;
        TopicOverviewCards.ItemsSource = _topics;
        _hosts = new GlassHostWindows(this);

        _session = new GlassSession();
        InitUiScale();
        ApplyLayoutFromSession();
        ApplyPrimaryWorkSurface();

        LoadIntercomHistory();
        InitIntercomSlash();
        InitIntercomPresence();
        InitIntercomHud();
        InitCommandPalette();
        InitCascadeChord();
        InitOpenFamily();

        EnsureEditorChrome();
        TryOpenDogfoodFile();

        _latches = new LatchHub();
        _latches.IntercomChanged += OnIntercomChanged;
        _latches.CitizenDialogRequestChanged += OnCitizenDialogRequestChanged;
        _latches.PresenceChanged += OnPresenceChanged;
        _latches.PresentationChanged += OnPresentationChanged;
        _latches.PlanChanged += OnPlanChanged;
        _latches.SeatsChanged += OnSeatsChanged;
        _latches.LandChanged += OnLandChanged;
        _latches.SharedChanged += OnSharedChanged;
        _latches.DiskChanged += OnDiskChanged;
        _latches.IgniteWakeChanged += OnIgniteWakeChanged;
        _latches.IgniteChanged += OnIgniteChanged;
        _latches.SoftOrganChanged += OnSoftOrganChanged;
        _latches.AlertChanged += OnAlertChanged;
        _latches.QrhChanged += OnQrhChanged;
        _latches.EclChanged += OnEclChanged;
        _latches.Start();

        InitFilesDeskFace();
        InitFindDeskFace();

        _surface = new GlassSurfaceCommandHub(this);
        _surface.Start();

        StatusText.Text =
            $"glass · {_session.Layout.Topology} · cols={_session.Layout.ColumnDefinitions} · {_latches.StateRoot}";
        Loaded += (_, _) =>
        {
            _hostsReady = true;
            SyncHostWindows();
        };
        Closed += (_, _) =>
        {
            _composingDebounce?.Stop();
            _presenceStaleTimer?.Stop();
            DisposeTerminalSession();
            DisposeBuildSession();
            DisposeTestsSession();
            DisposeGitSession();
            DisposeEditorChrome();
            _surface.Dispose();
            _hosts.Dispose();
            _latches.Dispose();
        };
        UpdateMfdBody();
        RefreshEicasHealth();
    }

    internal IReadOnlyList<(string Role, Window Window)> EnumerateSurfaceWindows() =>
        _hosts.EnumerateRoleWindows();
}
