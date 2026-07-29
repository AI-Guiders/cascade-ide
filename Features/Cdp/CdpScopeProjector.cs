#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent Project Switch (PRIMARY/SCOPE) → quiet chrome band.
/// Watches scope-LATEST.json; paints <see cref="MainWindowViewModel.AgentScopeChromeHint"/>.
/// Empty (no chrome_hint) stays silent — not EICAS.
/// </summary>
internal sealed class CdpScopeProjector : IDisposable
{
    public const string Schema = "cide_scope_latch/v1";
    public const string OriginAgent = "agent";

    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    readonly MainWindowViewModel _vm;
    readonly FileSystemWatcher _watcher;
    readonly object _gate = new();
    DateTimeOffset _lastStamp = DateTimeOffset.MinValue;
    string? _lastHint;
    bool _disposed;

    public static CdpScopeProjector? Instance { get; private set; }

    public static string StateRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "cdp-mcp");

    public static string LatchPath => Path.Combine(StateRoot, "scope-LATEST.json");

    CdpScopeProjector(MainWindowViewModel vm, string stateRoot)
    {
        _vm = vm;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "scope-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpScopeProjector Start(MainWindowViewModel vm)
    {
        Instance?.Dispose();
        Instance = new CdpScopeProjector(vm, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        ScopeLatchDoc? doc;
        try
        {
            if (!File.Exists(LatchPath))
            {
                _vm.ApplyScopeChromeHint(null);
                return;
            }

            var raw = File.ReadAllText(LatchPath);
            doc = JsonSerializer.Deserialize<ScopeLatchDoc>(raw, ReadOpts);
        }
        catch
        {
            return;
        }

        if (doc is null)
            return;
        if (!string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.Equals(doc.Origin, OriginAgent, StringComparison.OrdinalIgnoreCase))
            return;

        var hint = string.IsNullOrWhiteSpace(doc.ChromeHint) ? null : doc.ChromeHint.Trim();

        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && string.Equals(hint, _lastHint, StringComparison.Ordinal))
                return;
            _lastStamp = doc.StampedUtc;
            _lastHint = hint;
        }

        try
        {
            _vm.ApplyScopeChromeHint(hint);
        }
        catch
        {
            /* best-effort */
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _watcher.Dispose();
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    public sealed class ScopeLatchDoc
    {
        public string? Schema { get; set; }
        public string? Origin { get; set; }
        public bool Active { get; set; }
        public string? Pulse { get; set; }
        public string? Primary { get; set; }
        public string? Scope { get; set; }
        public string? ChromeHint { get; set; }

        [JsonPropertyName("stamped_utc")]
        public DateTimeOffset StampedUtc { get; set; }
    }
}
