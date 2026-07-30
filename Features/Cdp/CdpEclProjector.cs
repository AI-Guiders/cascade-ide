#nullable enable
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Cockpit.Channels.Eicas;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent ECL checklist → CIDE EICAS advisory lines.
/// Watches ecl-LATEST.json; merges as source=ecl (after alert/qrh).
/// Idle (no hot checklist) stays silent.
/// </summary>
internal sealed class CdpEclProjector : IDisposable
{
    public const string Schema = "cide_ecl_latch/v1";
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

    public static CdpEclProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "ecl-LATEST.json");

    CdpEclProjector(LatchEicasFeed feed, string stateRoot)
    {
        _feed = feed;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "ecl-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpEclProjector Start(LatchEicasFeed feed)
    {
        Instance?.Dispose();
        Instance = new CdpEclProjector(feed, StateRoot);
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

        EclLatchDoc? doc;
        try
        {
            doc = JsonSerializer.Deserialize<EclLatchDoc>(raw, ReadOpts);
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

        _feed.ReplaceSource("ecl", MapMessages(doc));
    }

    internal static IReadOnlyList<EicasMessage> MapMessages(EclLatchDoc doc)
    {
        if (string.IsNullOrWhiteSpace(doc.HotId))
            return Array.Empty<EicasMessage>();

        var stamp = doc.StampedUtc == default ? DateTimeOffset.UtcNow : doc.StampedUtc;
        var list = new List<EicasMessage>();

        var head = !string.IsNullOrWhiteSpace(doc.Pulse)
            ? doc.Pulse!.Trim()
            : (doc.HotTitle ?? doc.HotId!).Trim();
        list.Add(new EicasMessage(EicasSeverity.Advisory, head, "cdp.ecl", stamp));

        foreach (var item in (doc.OpenItems ?? []).Take(4))
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Text) && string.IsNullOrWhiteSpace(item.Id))
                continue;
            var text = !string.IsNullOrWhiteSpace(item.Text) ? item.Text!.Trim() : item.Id!.Trim();
            list.Add(new EicasMessage(EicasSeverity.Advisory, text, "cdp.ecl", stamp));
        }

        return list;
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

    internal sealed class OpenItem
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
    }

    internal sealed class EclLatchDoc
    {
        public string Schema { get; set; } = CdpEclProjector.Schema;
        public string Origin { get; set; } = OriginAgent;
        public DateTimeOffset StampedUtc { get; set; }
        public bool Ok { get; set; } = true;
        public string? Pulse { get; set; }
        public string? HotId { get; set; }
        public string? HotTitle { get; set; }
        public int OpenRequired { get; set; }
        public int ActiveCount { get; set; }
        public OpenItem[]? OpenItems { get; set; }

        public string Fingerprint() =>
            string.Join('|',
                HotId ?? "",
                Pulse ?? "",
                OpenRequired.ToString(),
                string.Join(';', (OpenItems ?? []).Select(i => (i.Id ?? "") + "=" + (i.Text ?? ""))));
    }
}
