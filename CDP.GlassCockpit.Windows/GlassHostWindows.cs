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
internal sealed class GlassHostWindows : IDisposable
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
            if (surfacePack?.Slots.FirstOrDefault(s => s.Role == PresentationScanRole.PmOneOf) is { } pmSlot
                && pmSlot.Stack.Count > 0)
            {
                SetPmOneOfStack(pmSlot.Stack, pmSlot.Active);
            }

            if (flags.PfdHostTopology)
                EnsurePfdHost();
            else
                ClosePfdHost();

            if (flags.PmOneOfHostTopology || flags.OneOfHostTopology)
            {
                ClosePmHost();
                CloseMfdHost();
                EnsurePmOneOfHost();
            }
            else if (flags.PmHostTopology)
            {
                ClosePmOneOfHost();
                EnsurePmHost();
                CloseMfdHost(); // PM host owns M; dedicated Mfd host not simultaneous
            }
            else
            {
                ClosePmHost();
                ClosePmOneOfHost();
                if (flags.MfdHostTopology)
                    EnsureMfdHost();
                else
                    CloseMfdHost();
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
        if (!IsPmOneOfActive && _pmOneOfHost is null)
            return false;
        if (_pmOneOfStack.Length == 0)
            return false;
        var idx = Array.FindIndex(_pmOneOfStack, s => s == _pmOneOfActiveSurface);
        if (idx < 0)
            idx = 0;
        PreferSurface(_pmOneOfStack[(idx + 1) % _pmOneOfStack.Length]);
        return true;
    }

    /// <summary>Prefer a surface/channel in the OneOf stack (maps to P/M zone paint).</summary>
    public void PreferSurface(string surface)
    {
        var s = surface.Trim().ToLowerInvariant();
        if (_pmOneOfStack.Length > 0 && !_pmOneOfStack.Contains(s, StringComparer.Ordinal))
            return;
        var zone = GlassPresentationLayout.ZoneForSurface(s);
        if (zone is not (PresentationAnchorKind.Pfd or PresentationAnchorKind.Mfd))
            return;
        _pmOneOfActiveSurface = s;
        PreferPmOneOf(zone.Value);
    }

    /// <summary>Auto-switch / chord: show P or M full in OneOf host.</summary>
    public void PreferPmOneOf(PresentationAnchorKind kind)
    {
        if (kind is not (PresentationAnchorKind.Pfd or PresentationAnchorKind.Mfd))
            return;
        // Keep surface label aligned when called from MFD page path.
        if (GlassPresentationLayout.ZoneForSurface(_pmOneOfActiveSurface) != kind)
        {
            var match = _pmOneOfStack.FirstOrDefault(s => GlassPresentationLayout.ZoneForSurface(s) == kind);
            if (match is not null)
                _pmOneOfActiveSurface = match;
        }

        _pmOneOfActive = kind;
        if (!IsPmOneOfActive && _pmOneOfHost is null)
            return;
        if (IsPmOneOfActive)
            RemountPmOneOfActive();
    }

    void EnsurePfdHost()
    {
        if (_pfdHost is { IsVisible: true })
        {
            _pfdHost.Activate();
            return;
        }

        var zone = _main.PfdZone;
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
        PlaceSatellite(_pfdHost, screenHint: 0);
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
        PlaceSatellite(_mfdHost, screenHint: 1);
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
        PlaceSatellite(_pmHost, screenHint: 1);
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
        PlaceSatellite(_pmOneOfHost, screenHint: 1);
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

    void PlaceSatellite(Window host, int screenHint)
    {
        host.Width = Math.Max(640, _main.Width * 0.7);
        host.Height = Math.Max(480, _main.Height * 0.85);

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var primaryW = SystemParameters.PrimaryScreenWidth;
        var virtualW = SystemParameters.VirtualScreenWidth;

        if (screenHint > 0 && virtualW > primaryW + 80)
        {
            host.Left = virtualLeft + primaryW + 24;
            host.Top = virtualTop + 48;
        }
        else
        {
            host.Left = _main.Left + 36 + screenHint * 28;
            host.Top = _main.Top + 36 + screenHint * 28;
        }
    }

    public void Dispose()
    {
        ClosePmOneOfHost();
        ClosePmHost();
        ClosePfdHost();
        CloseMfdHost();
    }
}
