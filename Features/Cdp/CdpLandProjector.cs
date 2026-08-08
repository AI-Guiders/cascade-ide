#nullable enable
using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Operator GUI projector for agent <c>cdp_land</c> open|goto.
/// Watches %LocalAppData%/cdp-mcp/land-LATEST.json (written by CDP NavigationLandLatch).
/// Applies <see cref="IIdeMcpActions.OpenFile"/> or, when line present,
/// <see cref="IIdeMcpActions.GoToPosition"/> (waits for Monaco — SelectInEditor races open).
/// Does not touch Intent Melody / CascadeIdeSettings.
/// </summary>
internal sealed class CdpLandProjector : IDisposable
{
    public const string Schema = "navigation_land_latch/v1";

    readonly IIdeMcpActions _actions;
    readonly FileSystemWatcher _watcher;
    readonly object _gate = new();
    DateTimeOffset _lastStamp = DateTimeOffset.MinValue;
    string? _lastPath;
    int? _lastLine;
    bool _disposed;

    CdpLandProjector(IIdeMcpActions actions, string stateRoot)
    {
        _actions = actions;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "land-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;

        // Catch latch written while GUI was down.
        TryApplyFromDisk(force: true);
    }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "land-LATEST.json");

    public static CdpLandProjector Start(IIdeMcpActions actions) =>
        new(actions, StateRoot);

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        var raw = CdpLatchIo.TryReadAllTextIfExists(LatchPath);
        if (raw is null)
            return;

        LandLatchDoc? doc;
        try
        {
            doc = JsonSerializer.Deserialize<LandLatchDoc>(raw, JsonOpts);
        }
        catch
        {
            return;
        }

        if (doc is null || string.IsNullOrWhiteSpace(doc.Path))
            return;
        if (!string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
            return;

        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && string.Equals(doc.Path, _lastPath, StringComparison.OrdinalIgnoreCase)
                && doc.Line == _lastLine)
                return;

            _lastStamp = doc.StampedUtc;
            _lastPath = doc.Path;
            _lastLine = doc.Line;
        }

        try
        {
            if (!File.Exists(doc.Path))
                return;

            // Quiet land (default): Agent-Side locus only — do not steal Human editor Face.
            if (!doc.ShowFace)
                return;

            if (doc.Line is > 0)
            {
                var line = doc.Line.Value;
                // GoToPosition waits for Monaco dock — SelectInEditor races ScheduleOpenFile
                // and leaves caret at top of a freshly opened tab.
                CdpFocusLatchPublisher.Instance?.SuppressEcho();
                _actions.GoToPosition(doc.Path, line, 1, line, 1);
            }
            else
            {
                CdpFocusLatchPublisher.Instance?.SuppressEcho();
                _actions.OpenFile(doc.Path);
            }
        }
        catch
        {
            /* best-effort projector */
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFsEvent;
        _watcher.Created -= OnFsEvent;
        _watcher.Renamed -= OnFsEvent;
        _watcher.Dispose();
    }

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    sealed class LandLatchDoc
    {
        public string? Schema { get; set; }
        public string? Command { get; set; }
        public string? Path { get; set; }
        public int? Line { get; set; }
        public string? Member { get; set; }
        public bool ShowFace { get; set; }
        public DateTimeOffset StampedUtc { get; set; }
    }
}
