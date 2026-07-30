#nullable enable
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Cockpit.Channels.Eicas;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent SA/alert → CIDE EICAS bar.
/// Watches alert-LATEST.json; maps clear/warn/fail → empty / Caution / Warning messages.
/// </summary>
internal sealed class CdpAlertProjector : IDisposable
{
    public const string Schema = "cide_alert_latch/v1";
    public const string OriginAgent = "agent";

    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    readonly LatchEicasFeed _feed;
    readonly FileSystemWatcher _watcher;
    readonly object _gate = new();
    DateTimeOffset _lastStamp = DateTimeOffset.MinValue;
    string? _lastFingerprint;
    bool _disposed;

    public static CdpAlertProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "alert-LATEST.json");

    CdpAlertProjector(LatchEicasFeed feed, string stateRoot)
    {
        _feed = feed;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "alert-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpAlertProjector Start(LatchEicasFeed feed)
    {
        Instance?.Dispose();
        Instance = new CdpAlertProjector(feed, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        string raw;
        try
        {
            if (!File.Exists(LatchPath))
                return;
            raw = File.ReadAllText(LatchPath);
        }
        catch
        {
            return;
        }

        AlertLatchDoc? doc;
        try
        {
            doc = JsonSerializer.Deserialize<AlertLatchDoc>(raw, ReadOpts);
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

        var fingerprint = doc.Fingerprint();
        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
                return;

            _lastStamp = doc.StampedUtc;
            _lastFingerprint = fingerprint;
        }

        _feed.ReplaceSource("alert", MapMessages(doc));
    }

    internal static IReadOnlyList<EicasMessage> MapMessages(AlertLatchDoc doc)
    {
        var level = (doc.Level ?? "clear").Trim().ToLowerInvariant();
        if (level is "clear" or "")
            return Array.Empty<EicasMessage>();

        var severity = level switch
        {
            "fail" => EicasSeverity.Warning,
            "warn" => EicasSeverity.Caution,
            _ => EicasSeverity.Advisory
        };

        var stamp = doc.StampedUtc == default ? DateTimeOffset.UtcNow : doc.StampedUtc;
        var lines = (doc.Lines ?? [])
            .Where(static l => !string.IsNullOrWhiteSpace(l))
            .Take(16)
            .ToArray();

        if (lines.Length == 0)
        {
            if (string.IsNullOrWhiteSpace(doc.Pulse))
                return Array.Empty<EicasMessage>();
            return [new EicasMessage(severity, doc.Pulse!.Trim(), "cdp.alert", stamp)];
        }

        return lines
            .Select(l => new EicasMessage(severity, l.Trim(), "cdp.alert", stamp))
            .ToArray();
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

    internal sealed class AlertLatchDoc
    {
        public string Schema { get; set; } = CdpAlertProjector.Schema;
        public string Origin { get; set; } = OriginAgent;
        public DateTimeOffset StampedUtc { get; set; }
        public string? Level { get; set; }
        public bool Ok { get; set; } = true;
        public string? Pulse { get; set; }
        public string[]? Lines { get; set; }

        public string Fingerprint() =>
            string.Join('|',
                Level ?? "",
                Pulse ?? "",
                string.Join(';', Lines ?? []));
    }
}
