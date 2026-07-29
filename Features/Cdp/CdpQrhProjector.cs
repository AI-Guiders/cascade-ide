#nullable enable
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Cockpit.Channels.Eicas;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent eQRH suggest → CIDE EICAS advisory lines.
/// Watches qrh-LATEST.json; merges as source=qrh (below alert severity).
/// </summary>
internal sealed class CdpQrhProjector : IDisposable
{
    public const string Schema = "cide_qrh_latch/v1";
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

    public static CdpQrhProjector? Instance { get; private set; }

    public static string StateRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "cdp-mcp");

    public static string LatchPath => Path.Combine(StateRoot, "qrh-LATEST.json");

    CdpQrhProjector(LatchEicasFeed feed, string stateRoot)
    {
        _feed = feed;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "qrh-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpQrhProjector Start(LatchEicasFeed feed)
    {
        Instance?.Dispose();
        Instance = new CdpQrhProjector(feed, StateRoot);
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

        QrhLatchDoc? doc;
        try
        {
            doc = JsonSerializer.Deserialize<QrhLatchDoc>(raw, ReadOpts);
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

        _feed.ReplaceSource("qrh", MapMessages(doc));
    }

    internal static IReadOnlyList<EicasMessage> MapMessages(QrhLatchDoc doc)
    {
        if (string.IsNullOrWhiteSpace(doc.HotId))
            return Array.Empty<EicasMessage>();

        var stamp = doc.StampedUtc == default ? DateTimeOffset.UtcNow : doc.StampedUtc;
        var list = new List<EicasMessage>();

        var head = !string.IsNullOrWhiteSpace(doc.Pulse)
            ? doc.Pulse!.Trim()
            : (doc.HotTitle ?? doc.HotId!).Trim();
        list.Add(new EicasMessage(EicasSeverity.Advisory, head, "cdp.qrh", stamp));

        foreach (var rel in (doc.Related ?? []).Take(4))
        {
            if (rel is null || string.IsNullOrWhiteSpace(rel.Title) && string.IsNullOrWhiteSpace(rel.Id))
                continue;
            var text = !string.IsNullOrWhiteSpace(rel.Title) ? rel.Title!.Trim() : rel.Id!.Trim();
            list.Add(new EicasMessage(EicasSeverity.Advisory, text, "cdp.qrh", stamp));
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

    internal sealed class RelatedPage
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
    }

    internal sealed class QrhLatchDoc
    {
        public string Schema { get; set; } = CdpQrhProjector.Schema;
        public string Origin { get; set; } = OriginAgent;
        public DateTimeOffset StampedUtc { get; set; }
        public bool Ok { get; set; } = true;
        public string? Pulse { get; set; }
        public string? HotId { get; set; }
        public string? HotTitle { get; set; }
        public RelatedPage[]? Related { get; set; }

        public string Fingerprint() =>
            string.Join('|',
                HotId ?? "",
                Pulse ?? "",
                string.Join(';', (Related ?? []).Select(r => (r.Id ?? "") + "=" + (r.Title ?? ""))));
    }
}
