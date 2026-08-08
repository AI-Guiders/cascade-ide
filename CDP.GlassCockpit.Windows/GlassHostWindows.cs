#nullable enable
using System.Windows;
using System.Windows.Controls;
using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.Services.Presentation;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Secondary TopLevels for multi-screen presentation topologies (()()() / (P)(F)(M) / (P+F)(M) / (P/M)(F) …).
/// Reuses GlassCore <see cref="PresentationTopologyFlags"/> — same rules as Avalonia CIDE hosts.
/// </summary>
internal sealed partial class GlassHostWindows : IDisposable
{
    readonly MainWindow _main;
    ZoneHostWindow? _pfdHost;
    ZoneHostWindow? _mfdHost;
    ZoneHostWindow? _pmHost;
    ZoneHostWindow? _pmOneOfHost;
    PresentationAnchorKind _pmOneOfActive = PresentationAnchorKind.Pfd;
    string[] _pmOneOfStack = ["p", "m"];
    string _pmOneOfActiveSurface = "p";
    bool _syncing;
    /// <summary>True when topology is a single TopLevel OneOf — XOR columns on main (no satellite).</summary>
    bool _mainScanOneOf;

    public bool IsMainScanOneOf => _mainScanOneOf;

    public GlassHostWindows(MainWindow main) => _main = main;

    public bool IsPmOneOfActive => _pmOneOfHost is { IsVisible: true };

    public PresentationAnchorKind PmOneOfActiveKind => _pmOneOfActive;

    public string PmOneOfActiveSurface => _pmOneOfActiveSurface;

    public IReadOnlyList<string> PmOneOfStack => _pmOneOfStack;

    /// <summary>TopLevels for agent_surface layout Sense (roles match contract).</summary>
    public IReadOnlyList<(string Role, Window Window)> EnumerateRoleWindows()
    {
        var list = new List<(string, Window)> { ("main", _main) };
        if (_pfdHost is { IsVisible: true })
            list.Add(("pfd_host", _pfdHost));
        if (_mfdHost is { IsVisible: true })
            list.Add(("mfd_host", _mfdHost));
        if (_pmHost is { IsVisible: true })
            list.Add(("pm_host", _pmHost));
        if (_pmOneOfHost is { IsVisible: true })
            list.Add(("pm_oneof_host", _pmOneOfHost));
        return list;
    }

    public void Sync(PresentationTopologyFlags flags, PresentationSurfacePack? surfacePack = null)
    {
        if (_syncing)
            return;
        _syncing = true;
        try
        {
            var singleScanOneOf = surfacePack?.Slots is [{ Role: PresentationScanRole.PmOneOf }];
            // Preserve live XOR active across presentation latch re-apply (same topology).
            var preserveActive = _mainScanOneOf
                && _pmOneOfActiveSurface.Length > 0
                && _pmOneOfStack.Length > 0;

            // Arm main-XOR mode before SetPmOneOfStack so PreferSurface accepts F in stack.
            if (singleScanOneOf)
                _mainScanOneOf = true;
            else
                _mainScanOneOf = false;

            if (surfacePack?.Slots.FirstOrDefault(s => s.Role == PresentationScanRole.PmOneOf) is { } pmSlot
                && pmSlot.Stack.Count > 0)
            {
                var active = pmSlot.Active;
                if (preserveActive
                    && pmSlot.Stack.Contains(_pmOneOfActiveSurface, StringComparer.Ordinal))
                {
                    active = _pmOneOfActiveSurface;
                }

                SetPmOneOfStack(pmSlot.Stack, active);
            }

            // Tear down hosts we do not want first so zones return to main before remount.
            // Prior bug: EnsurePfdHost ran while OneOf still held/parked PfdZone → empty P host.
            if (singleScanOneOf)
            {
                ClosePmOneOfHost();
                ClosePmHost();
                ClosePfdHost();
                CloseMfdHost();
                ApplyMainScanOneOfColumns();
                return;
            }

            var wantOneOf = flags.PmOneOfHostTopology || flags.OneOfHostTopology;
            var wantPm = flags.PmHostTopology;
            var wantPfd = flags.PfdHostTopology;
            var wantMfd = flags.MfdHostTopology && !wantOneOf && !wantPm;

            if (!wantOneOf)
                ClosePmOneOfHost();
            if (!wantPm)
                ClosePmHost();
            if (!wantPfd)
                ClosePfdHost();
            if (!wantMfd)
                CloseMfdHost();

            if (wantOneOf)
            {
                EnsurePmOneOfHost();
            }
            else if (wantPm)
            {
                EnsurePmHost();
            }
            else
            {
                if (wantPfd)
                    EnsurePfdHost();
                if (wantMfd)
                    EnsureMfdHost();
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>Channel/surface stack on the P/M OneOf host (ND-style).</summary>
    public void SetPmOneOfStack(IReadOnlyList<string> stack, string? active = null)
    {
        if (stack.Count == 0)
            return;
        _pmOneOfStack = stack.Select(s => s.Trim().ToLowerInvariant()).Where(s => s.Length > 0).ToArray();
        if (_pmOneOfStack.Length == 0)
            return;
        var want = string.IsNullOrWhiteSpace(active) ? _pmOneOfStack[0] : active.Trim().ToLowerInvariant();
        if (!_pmOneOfStack.Contains(want, StringComparer.Ordinal))
            want = _pmOneOfStack[0];
        PreferSurface(want);
    }

    /// <summary>Chord: cycle active surface in the OneOf channel stack.</summary>
    public bool TogglePmOneOfRole()
    {
        if (!_mainScanOneOf && !IsPmOneOfActive && _pmOneOfHost is null)
            return false;
        if (_pmOneOfStack.Length == 0)
            return false;
        var idx = Array.FindIndex(_pmOneOfStack, s => s == _pmOneOfActiveSurface);
        if (idx < 0)
            idx = 0;
        PreferSurface(_pmOneOfStack[(idx + 1) % _pmOneOfStack.Length]);
        return true;
    }

    /// <summary>Prefer a surface/channel in the OneOf stack (maps to P/M/F zone paint).</summary>
    public void PreferSurface(string surface)
    {
        var s = surface.Trim().ToLowerInvariant();
        if (_pmOneOfStack.Length > 0 && !_pmOneOfStack.Contains(s, StringComparer.Ordinal))
            return;
        var zone = GlassPresentationLayout.ZoneForSurface(s);
        if (zone is null)
            return;

        if (_mainScanOneOf)
        {
            if (zone is not (PresentationAnchorKind.Pfd or PresentationAnchorKind.Mfd or PresentationAnchorKind.Forward))
                return;
            _pmOneOfActiveSurface = s;
            _pmOneOfActive = zone.Value;
            ApplyMainScanOneOfColumns();
            return;
        }

        if (zone is not (PresentationAnchorKind.Pfd or PresentationAnchorKind.Mfd))
            return;
        _pmOneOfActiveSurface = s;
        PreferPmOneOf(zone.Value);
    }

    /// <summary>Auto-switch / chord: show P or M full in OneOf host (or F on single-TopLevel).</summary>
    public void PreferPmOneOf(PresentationAnchorKind kind)
    {
        if (_mainScanOneOf)
        {
            if (kind is not (PresentationAnchorKind.Pfd or PresentationAnchorKind.Mfd or PresentationAnchorKind.Forward))
                return;
            AlignActiveSurfaceToZone(kind);
            _pmOneOfActive = kind;
            ApplyMainScanOneOfColumns();
            return;
        }

        if (kind is not (PresentationAnchorKind.Pfd or PresentationAnchorKind.Mfd))
            return;
        // Keep surface label aligned when called from MFD page path.
        AlignActiveSurfaceToZone(kind);

        _pmOneOfActive = kind;
        if (!IsPmOneOfActive && _pmOneOfHost is null)
            return;
        if (IsPmOneOfActive)
            RemountPmOneOfActive();
    }

    void AlignActiveSurfaceToZone(PresentationAnchorKind kind)
    {
        if (GlassPresentationLayout.ZoneForSurface(_pmOneOfActiveSurface) == kind)
            return;

        var match = _pmOneOfStack.FirstOrDefault(s => GlassPresentationLayout.ZoneForSurface(s) == kind);
        if (match is not null)
        {
            _pmOneOfActiveSurface = match;
            return;
        }

        // Stack may omit zone token — still XOR-paint the zone (MFD Editor Face / chords).
        _pmOneOfActiveSurface = kind switch
        {
            PresentationAnchorKind.Pfd => "p",
            PresentationAnchorKind.Mfd => "m",
            _ => "f",
        };
    }

    void ApplyMainScanOneOfColumns()
    {
        var cols = GlassPresentationLayout.ColumnDefsForScanOneOfActive(_pmOneOfActiveSurface);
        WpfMainGridColumns.Apply(_main.MainGrid, cols);
        _main.PatchScanOneOfActive(_pmOneOfActiveSurface);
        var stackLabel = string.Join('/', _pmOneOfStack);
        _main.StatusText.Text =
            $"glass · ({stackLabel}) · {_pmOneOfActiveSurface} active · single TopLevel OneOf · {DateTime.Now:HH:mm:ss}";
    }

    void EnsurePfdHost()
    {
        var zone = _main.PfdZone;
        if (_pfdHost is { IsVisible: true })
        {
            // Remount if prior Sync left an empty shell (OneOf still held the zone).
            if (!_pfdHost.HasMountedContent)
            {
                DetachFromParent(zone);
                _pfdHost.Mount(zone);
            }

            _pfdHost.Activate();
            return;
        }

        DetachFromMain(zone);
        _pfdHost = NewHost("P · PFD host", "topology · PfdHost · (P)…");
        var pfd = _pfdHost;
        pfd.Closed += (_, _) =>
        {
            if (!ReferenceEquals(_pfdHost, pfd))
                return;
            RestoreToMain(pfd.Dismount(), column: 0);
            _pfdHost = null;
        };
        pfd.Mount(zone);
        PlaceSatellite(_pfdHost, screenHint: 0, role: SatelliteRole.Pfd);
        _pfdHost.Show();
    }

    void EnsureMfdHost()
    {
        if (_mfdHost is { IsVisible: true })
        {
            _mfdHost.Activate();
            return;
        }

        var zone = _main.MfdZone;
        DetachFromMain(zone);
        _mfdHost = NewHost("M · MFD host", "topology · MfdHost · …(M)");
        var mfd = _mfdHost;
        mfd.Closed += (_, _) =>
        {
            if (!ReferenceEquals(_mfdHost, mfd))
                return;
            RestoreToMain(mfd.Dismount(), column: 4);
            _mfdHost = null;
        };
        mfd.Mount(zone);
        PlaceSatellite(_mfdHost, screenHint: 1, role: SatelliteRole.Mfd);
        _mfdHost.Show();
    }

    void EnsurePmHost()
    {
        if (_pmHost is { IsVisible: true })
        {
            _pmHost.Activate();
            return;
        }

        DetachFromMain(_main.PfdZone);
        DetachFromMain(_main.MfdZone);

        var split = new Grid();
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.45, GridUnitType.Star) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.55, GridUnitType.Star) });
        Grid.SetColumn(_main.PfdZone, 0);
        Grid.SetColumn(_main.MfdZone, 2);
        split.Children.Add(_main.PfdZone);
        split.Children.Add(new GridSplitter
        {
            Width = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = System.Windows.Media.Brushes.DimGray
        });
        Grid.SetColumn(split.Children[^1], 1);
        split.Children.Add(_main.MfdZone);

        _pmHost = NewHost("P+M · PM host", "topology · PmHost · (xP+yM)(F)");
        var pm = _pmHost;
        pm.Closed += (_, _) =>
        {
            if (!ReferenceEquals(_pmHost, pm))
                return;
            _ = pm.Dismount();
            DetachFromParent(_main.PfdZone);
            DetachFromParent(_main.MfdZone);
            RestoreToMain(_main.PfdZone, column: 0);
            RestoreToMain(_main.MfdZone, column: 4);
            _pmHost = null;
        };
        pm.Mount(split);
        PlaceSatellite(_pmHost, screenHint: 1, role: SatelliteRole.Pm);
        _pmHost.Show();
    }

    void EnsurePmOneOfHost()
    {
        if (_pmOneOfHost is { IsVisible: true })
        {
            RemountPmOneOfActive();
            _pmOneOfHost.Activate();
            return;
        }

        DetachFromMain(_main.PfdZone);
        DetachFromMain(_main.MfdZone);
        // Park inactive zone off-main in a hidden panel so restore is symmetric.
        EnsureOneOfPark();

        _pmOneOfHost = NewHost(
            OneOfTitle(_pmOneOfActive),
            "topology · PmOneOf · (P/M)(F)");
        var host = _pmOneOfHost;
        host.Closed += (_, _) =>
        {
            if (!ReferenceEquals(_pmOneOfHost, host))
                return;
            _ = host.Dismount();
            DetachFromParent(_main.PfdZone);
            DetachFromParent(_main.MfdZone);
            RestoreToMain(_main.PfdZone, column: 0);
            RestoreToMain(_main.MfdZone, column: 4);
            _oneOfPark = null;
            _pmOneOfHost = null;
        };

        RemountPmOneOfActive();
        PlaceSatellite(_pmOneOfHost, screenHint: 1, role: SatelliteRole.Pm);
        _pmOneOfHost.Show();
    }

    Panel? _oneOfPark;

    void EnsureOneOfPark()
    {
        _oneOfPark ??= new Grid { Visibility = Visibility.Collapsed };
    }

    void RemountPmOneOfActive()
    {
        if (_pmOneOfHost is null)
            return;

        EnsureOneOfPark();
        var active = _pmOneOfActive == PresentationAnchorKind.Mfd ? _main.MfdZone : _main.PfdZone;
        var idle = _pmOneOfActive == PresentationAnchorKind.Mfd ? _main.PfdZone : _main.MfdZone;

        DetachFromParent(active);
        DetachFromParent(idle);
        _oneOfPark!.Children.Clear();
        _oneOfPark.Children.Add(idle);

        _pmOneOfHost.Mount(active);
        RefreshOneOfChrome();
    }

    void RefreshOneOfChrome()
    {
        if (_pmOneOfHost is null)
            return;
        var stackLabel = string.Join('/', _pmOneOfStack);
        _pmOneOfHost.Title = $"{stackLabel} · {_pmOneOfActiveSurface} active · OneOf host";
        _pmOneOfHost.SetBadge($"topology · PmOneOf · scan P/M · {_pmOneOfActiveSurface}");
    }

    static string OneOfTitle(PresentationAnchorKind kind) =>
        kind == PresentationAnchorKind.Mfd
            ? "P/M · M active · OneOf host"
            : "P/M · P active · OneOf host";

    void ClosePfdHost() => CloseHost(ref _pfdHost, restoreColumn: 0);
    void CloseMfdHost() => CloseHost(ref _mfdHost, restoreColumn: 4);

    void ClosePmHost()
    {
        if (_pmHost is null)
            return;
        try { _pmHost.Close(); }
        catch { /* disposed */ }
    }

    void ClosePmOneOfHost()
    {
        if (_pmOneOfHost is null)
            return;
        try { _pmOneOfHost.Close(); }
        catch { /* disposed */ }
    }

    void CloseHost(ref ZoneHostWindow? host, int restoreColumn)
    {
        if (host is null)
            return;
        var h = host;
        host = null;
        var content = h.Dismount();
        try { h.Close(); }
        catch { /* disposed */ }
        RestoreToMain(content, restoreColumn);
    }

    static ZoneHostWindow NewHost(string title, string badge)
    {
        var w = new ZoneHostWindow { Title = title };
        w.SetBadge(badge);
        return w;
    }

    void DetachFromMain(FrameworkElement zone) => DetachFromParent(zone);

    static void DetachFromParent(FrameworkElement zone)
    {
        switch (zone.Parent)
        {
            case Panel panel:
                panel.Children.Remove(zone);
                break;
            case Decorator decorator:
                decorator.Child = null;
                break;
            case ContentControl cc:
                cc.Content = null;
                break;
        }
    }

    void RestoreToMain(UIElement? zone, int column)
    {
        if (zone is not FrameworkElement fe)
            return;
        DetachFromParent(fe);
        if (_main.MainGrid.Children.Contains(fe))
            return;
        Grid.SetColumn(fe, column);
        _main.MainGrid.Children.Add(fe);
    }

    public void Dispose()
    {
        ClosePmOneOfHost();
        ClosePmHost();
        ClosePfdHost();
        CloseMfdHost();
    }
}
