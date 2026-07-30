#nullable enable
using System.Text.Json;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent desk → CIDE Intercom voice (@PF → @PM).
/// Watches %LocalAppData%/cdp-mcp/intercom-LATEST.json; applies origin=agent to=pm
/// via <see cref="Features.Chat.ChatPanelViewModel.AppendMessageFromMcpAsync"/>.
/// </summary>
internal sealed class CdpIntercomVoiceProjector : IDisposable
{
    public const string Schema = "cide_intercom_voice_latch/v0";
    public const string OriginAgent = "agent";
    public const string SeatPm = "pm";

    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    readonly MainWindowViewModel _vm;
    readonly FileSystemWatcher _watcher;
    readonly object _gate = new();
    DateTimeOffset _lastStamp = DateTimeOffset.MinValue;
    string? _lastId;
    bool _disposed;

    public static CdpIntercomVoiceProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "intercom-LATEST.json");

    CdpIntercomVoiceProjector(MainWindowViewModel vm, string stateRoot)
    {
        _vm = vm;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "intercom-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        TryApplyFromDisk(force: true);
    }

    public static CdpIntercomVoiceProjector Start(MainWindowViewModel vm)
    {
        Instance?.Dispose();
        Instance = new CdpIntercomVoiceProjector(vm, StateRoot);
        return Instance;
    }

    /// <summary>Skip echo when this process just published human→pf.</summary>
    public void SuppressEcho(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;
        lock (_gate)
            _lastId = id;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        var raw = CdpLatchIo.TryReadAllTextIfExists(LatchPath);
        if (raw is null)
            return;

        IntercomVoiceDoc? doc;
        try
        {
            doc = JsonSerializer.Deserialize<IntercomVoiceDoc>(raw, ReadOpts);
        }
        catch
        {
            return;
        }

        if (doc is null || string.IsNullOrWhiteSpace(doc.Body))
            return;
        if (!string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.Equals(doc.Origin, OriginAgent, StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.Equals(doc.ToSeat, SeatPm, StringComparison.OrdinalIgnoreCase))
            return;

        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && string.Equals(doc.Id, _lastId, StringComparison.OrdinalIgnoreCase))
                return;

            _lastStamp = doc.StampedUtc;
            _lastId = doc.Id;
        }

        var display = FormatDisplay(doc);
        _ = ApplyAsync(display);
    }

    static string FormatDisplay(IntercomVoiceDoc doc)
    {
        var body = doc.Body.Trim();
        if (body.StartsWith("@PM", StringComparison.OrdinalIgnoreCase)
            || body.StartsWith("@PF", StringComparison.OrdinalIgnoreCase))
            return body;
        return "@PM " + body;
    }

    async Task ApplyAsync(string display)
    {
        try
        {
            await _vm.ChatPanel.AppendMessageFromMcpAsync("assistant", display).ConfigureAwait(false);
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

    sealed class IntercomVoiceDoc
    {
        public string Schema { get; set; } = CdpIntercomVoiceProjector.Schema;
        public string Id { get; set; } = "";
        public string FromSeat { get; set; } = "pf";
        public string ToSeat { get; set; } = "pm";
        public string Body { get; set; } = "";
        public string Origin { get; set; } = OriginAgent;
        public DateTimeOffset StampedUtc { get; set; }
    }
}
