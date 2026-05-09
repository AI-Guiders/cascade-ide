using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Threading;
using CascadeIDE.Services.Lsp;
using Microsoft.CodeAnalysis;
using CascadeIDE.Contracts;

#nullable enable

namespace CascadeIDE.Features.Lsp.DataAcquisition;

/// <summary>
/// Один процесс C# LSP (stdio): <see cref="textDocument/publishDiagnostics"/> → полосы в UI,
/// <see cref="textDocument/hover"/> → Quick Info в тултипе при активном процессе.
/// didOpen / didChange (full sync) для открытых .cs.
/// Транспорт и JSON-RPC — <see cref="LspStdioSession"/>.
/// </summary>
[IoBoundary("lsp-csharp-stdio")]
[UiThreadMarshal("diagnostics coalesce → UI Post")]
public sealed class CSharpLspDiagnosticsHost : ILspDiagnosticSource
{
    private readonly ConcurrentDictionary<string, List<EditorDiagnosticStrip>> _strips = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _syncedText = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _openedNormalized = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private LspStdioSession? _session;
    private CancellationTokenSource? _runCts;
    private int _versionCounter;
    private volatile bool _handshakeDone;
    private bool _disposed;
    private readonly object _diagNotifyLock = new();
    private bool _diagNotifyPosted;

    public bool IsActive => _handshakeDone && _session?.Process is { HasExited: false };

    public event Action? DiagnosticsChanged;

    public IReadOnlyList<EditorDiagnosticStrip> GetStripsForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];
        var key = LspFileUri.NormalizePath(filePath);
        return _strips.TryGetValue(key, out var list) ? list : [];
    }

    /// <summary>Запуск процесса, initialize / initialized. Возвращает false при ошибке.</summary>
    public async Task<bool> TryStartAsync(string providerId, string? solutionPath, string? userExecutable, string? userArguments, CancellationToken ct)
    {
        DisposeProcess();

        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return false;

        if (string.Equals(providerId, CSharpLspProviderIds.ParseOnly, StringComparison.OrdinalIgnoreCase))
            return false;

        var (exe, args) = CSharpLspProviderIds.ResolveProcessArgs(providerId, solutionPath, userExecutable, userArguments);
        if (string.IsNullOrWhiteSpace(exe))
            return false;

        var solutionDir = Path.GetDirectoryName(CanonicalFilePath.Normalize(solutionPath));
        if (string.IsNullOrEmpty(solutionDir))
            return false;

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = solutionDir,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
        };

        Process? proc;
        try
        {
            proc = Process.Start(psi);
        }
        catch
        {
            return false;
        }

        if (proc is null)
            return false;

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var runToken = _runCts.Token;

        _session = new LspStdioSession(proc, proc.StandardInput.BaseStream, OnLspNotification);
        _session.StartReadLoop(runToken);

        try
        {
            var rootUri = LspFileUri.PathToFileUri(solutionDir);
            var initId = _session.AllocateRequestId();

            JsonDocument initResult;
            try
            {
                var doc = await SendInitializeAsync(initId, rootUri, runToken).ConfigureAwait(false);
                if (doc is null)
                    return false;
                initResult = doc;
            }
            catch
            {
                return false;
            }

            using (initResult)
            {
                var root = initResult.RootElement;
                if (root.TryGetProperty("error", out _))
                    return false;
                if (!root.TryGetProperty("result", out _))
                    return false;
            }

            await SendInitializedNotificationAsync(runToken).ConfigureAwait(false);
            _handshakeDone = true;
            return true;
        }
        catch
        {
            DisposeProcess();
            return false;
        }
    }

    private void OnLspNotification(string method, JsonElement @params)
    {
        if (string.Equals(method, "textDocument/publishDiagnostics", StringComparison.Ordinal))
            HandlePublishDiagnostics(@params);
    }

    private async Task<JsonDocument?> SendInitializeAsync(int id, string rootUri, CancellationToken ct)
    {
        if (_session is null)
            return null;
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = "initialize",
            ["params"] = new JsonObject
            {
                ["processId"] = Environment.ProcessId,
                ["clientInfo"] = new JsonObject { ["name"] = "CascadeIDE", ["version"] = "1" },
                ["rootUri"] = rootUri,
                ["capabilities"] = new JsonObject
                {
                    ["textDocument"] = new JsonObject
                    {
                        ["synchronization"] = new JsonObject { ["dynamicRegistration"] = false },
                        ["hover"] = new JsonObject { ["dynamicRegistration"] = false }
                    },
                    ["workspace"] = new JsonObject { ["workspaceFolders"] = true }
                },
                ["workspaceFolders"] = new JsonArray
                {
                    new JsonObject { ["uri"] = rootUri, ["name"] = "root" }
                }
            }
        };
        return await _session.SendRequestAsync(msg, id, TimeSpan.FromSeconds(60), ct).ConfigureAwait(false);
    }

    public void EnsureOpened(string filePath, string text)
    {
        if (!IsActive || string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;
        var key = LspFileUri.NormalizePath(filePath);
        lock (_gate)
        {
            if (!_openedNormalized.Add(key))
                return;
        }

        _syncedText[key] = text;
        _ = SendDidOpenAsync(filePath, text, CancellationToken.None);
    }

    public void ScheduleDocumentSync(string filePath, string text)
    {
        if (!IsActive || string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;
        var key = LspFileUri.NormalizePath(filePath);

        if (_debounceByPath.TryGetValue(key, out var old))
        {
            try { old.Cancel(); } catch { }
            old.Dispose();
        }

        var cts = new CancellationTokenSource();
        _debounceByPath[key] = cts;
        _ = DebouncedSyncAsync(key, filePath, text, cts.Token);
    }

    /// <summary>
    /// Полная синхронизация текста и <c>textDocument/hover</c> (позиция в редакторе — 1-based line/column).
    /// Перед запросом буфер отправляется в LSP (без debounce), чтобы подсказка соответствовала открытому тексту.
    /// </summary>
    public async Task<string?> RequestHoverAsync(string filePath, string text, int line1, int col1, CancellationToken ct)
    {
        if (!IsActive || string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return null;
        if (line1 < 1 || col1 < 1)
            return null;
        if (_session is null)
            return null;

        await SyncFullTextForRequestAsync(filePath, text, ct).ConfigureAwait(false);

        var uri = LspFileUri.PathToFileUri(CanonicalFilePath.Normalize(filePath));
        var line0 = line1 - 1;
        var char0 = col1 - 1;

        var id = _session.AllocateRequestId();
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = "textDocument/hover",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = uri },
                ["position"] = new JsonObject { ["line"] = line0, ["character"] = char0 }
            }
        };

        try
        {
            using var doc = await _session.SendRequestAsync(msg, id, TimeSpan.FromSeconds(8), ct).ConfigureAwait(false);
            return ParseHoverResponse(doc);
        }
        catch
        {
            return null;
        }
    }

    /// <remarks>
    /// Порядок сообщений с <see cref="EnsureOpened"/> без общей очереди на stdin теоретически может перепутаться;
    /// на практике после первого debounce-синка состояние стабильно.
    /// </remarks>
    private async Task SyncFullTextForRequestAsync(string filePath, string text, CancellationToken ct)
    {
        var key = LspFileUri.NormalizePath(filePath);
        _syncedText[key] = text;
        bool needOpen;
        lock (_gate)
        {
            needOpen = !_openedNormalized.Contains(key);
            if (needOpen)
                _openedNormalized.Add(key);
        }

        if (needOpen)
            await SendDidOpenAsync(filePath, text, ct).ConfigureAwait(false);
        else
            await SendDidChangeFullAsync(filePath, text, ct).ConfigureAwait(false);
    }

    private static string? ParseHoverResponse(JsonDocument? doc)
    {
        if (doc is null)
            return null;
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
            return null;
        if (!root.TryGetProperty("result", out var result))
            return null;
        if (result.ValueKind == JsonValueKind.Null)
            return null;
        if (!result.TryGetProperty("contents", out var contents))
            return null;
        return LspHoverContentFormatter.Format(contents);
    }

    private async Task DebouncedSyncAsync(string key, string filePath, string text, CancellationToken ct)
    {
        try
        {
            await Task.Delay(400, ct).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        _syncedText[key] = text;
        if (!_openedNormalized.Contains(key))
            EnsureOpened(filePath, text);
        else
            await SendDidChangeFullAsync(filePath, text, ct).ConfigureAwait(false);
    }

    private async Task SendInitializedNotificationAsync(CancellationToken ct)
    {
        if (_session is null)
            return;
        const string payload = """{"jsonrpc":"2.0","method":"initialized","params":{}}""";
        var bytes = Encoding.UTF8.GetBytes(payload);
        await _session.SendRawUtf8Async(bytes, ct).ConfigureAwait(false);
    }

    private async Task SendDidOpenAsync(string filePath, string text, CancellationToken ct)
    {
        if (_session is null)
            return;
        var uri = LspFileUri.PathToFileUri(CanonicalFilePath.Normalize(filePath));
        var ver = Interlocked.Increment(ref _versionCounter);
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "textDocument/didOpen",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject
                {
                    ["uri"] = uri,
                    ["languageId"] = "csharp",
                    ["version"] = ver,
                    ["text"] = text
                }
            }
        };
        await _session.SendEnvelopeAsync(msg, ct).ConfigureAwait(false);
    }

    private async Task SendDidChangeFullAsync(string filePath, string text, CancellationToken ct)
    {
        if (_session is null)
            return;
        var uri = LspFileUri.PathToFileUri(CanonicalFilePath.Normalize(filePath));
        var ver = Interlocked.Increment(ref _versionCounter);
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "textDocument/didChange",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = uri, ["version"] = ver },
                ["contentChanges"] = new JsonArray { new JsonObject { ["text"] = text } }
            }
        };
        await _session.SendEnvelopeAsync(msg, ct).ConfigureAwait(false);
    }

    private void HandlePublishDiagnostics(JsonElement @params)
    {
        if (!@params.TryGetProperty("uri", out var uriEl))
            return;
        var uri = uriEl.GetString();
        if (string.IsNullOrEmpty(uri))
            return;
        if (!LspFileUri.TryUriToPath(uri, out var path))
            return;
        var key = LspFileUri.NormalizePath(path);

        if (!@params.TryGetProperty("diagnostics", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return;

        _syncedText.TryGetValue(key, out var sourceText);
        sourceText ??= "";

        var list = new List<EditorDiagnosticStrip>();
        foreach (var d in arr.EnumerateArray())
        {
            if (!d.TryGetProperty("range", out var range))
                continue;
            if (!TryGetRangeOffsets(sourceText, range, out var start, out var len, out var line1, out var col1))
                continue;
            var sev = MapSeverity(d);
            if (sev is null)
                continue;
            var id = FormatCode(d);
            var msg = d.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            list.Add(new EditorDiagnosticStrip(start, len, sev.Value, id, msg, line1, col1));
        }

        _strips[key] = list;
        ScheduleDiagnosticsNotify();
    }

    /// <summary>
    /// Коалесит <see cref="DiagnosticsChanged"/> в один <see cref="IUiScheduler.Post"/> на серию
    /// <c>textDocument/publishDiagnostics</c> (как <see cref="global::CascadeIDE.Features.Build.BuildOutputPanelViewModel.Append"/>).
    /// </summary>
    private void ScheduleDiagnosticsNotify()
    {
        lock (_diagNotifyLock)
        {
            if (_diagNotifyPosted)
                return;
            _diagNotifyPosted = true;
        }

        UiScheduler.Default.Post(() =>
        {
            lock (_diagNotifyLock)
            {
                _diagNotifyPosted = false;
            }

            DiagnosticsChanged?.Invoke();
        }, DispatcherPriority.Background);
    }

    private static DiagnosticSeverity? MapSeverity(JsonElement d)
    {
        if (!d.TryGetProperty("severity", out var s) || s.ValueKind != JsonValueKind.Number)
            return DiagnosticSeverity.Warning;
        return s.GetInt32() switch
        {
            1 => DiagnosticSeverity.Error,
            2 => DiagnosticSeverity.Warning,
            _ => (DiagnosticSeverity?)null
        };
    }

    private static string FormatCode(JsonElement d)
    {
        if (!d.TryGetProperty("code", out var c))
            return "lsp";
        return c.ValueKind switch
        {
            JsonValueKind.String => c.GetString() ?? "lsp",
            JsonValueKind.Number => c.GetRawText(),
            JsonValueKind.Object when c.TryGetProperty("value", out var v) => v.GetString() ?? v.GetRawText(),
            _ => "lsp"
        };
    }

    private static bool TryGetRangeOffsets(string text, JsonElement range, out int start, out int length, out int line1, out int col1)
    {
        start = 0;
        length = 1;
        line1 = 1;
        col1 = 1;
        if (!range.TryGetProperty("start", out var startEl) || !range.TryGetProperty("end", out var endEl))
            return false;
        if (!TryGetPosition(startEl, out var sl, out var sc))
            return false;
        if (!TryGetPosition(endEl, out var el, out var ec))
            return false;
        line1 = sl + 1;
        col1 = sc + 1;
        var sOff = GetOffset(text, sl, sc);
        var eOff = GetOffset(text, el, ec);
        if (eOff < sOff)
            eOff = sOff;
        start = sOff;
        length = Math.Max(1, eOff - sOff);
        return start <= text.Length;
    }

    private static bool TryGetPosition(JsonElement pos, out int line0, out int char0)
    {
        line0 = 0;
        char0 = 0;
        if (!pos.TryGetProperty("line", out var l) || l.ValueKind != JsonValueKind.Number)
            return false;
        if (!pos.TryGetProperty("character", out var c) || c.ValueKind != JsonValueKind.Number)
            return false;
        line0 = l.GetInt32();
        char0 = c.GetInt32();
        return true;
    }

    private static int GetOffset(string text, int line0, int char0)
    {
        var line = 0;
        var i = 0;
        while (i < text.Length && line < line0)
        {
            if (text[i] == '\n')
                line++;
            i++;
        }

        if (line != line0)
            return Math.Min(i, text.Length);

        var col = 0;
        while (i < text.Length && col < char0)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                i++;
                continue;
            }

            if (ch == '\n')
                break;
            if (char.IsSurrogatePair(text, i))
            {
                i += 2;
                col++;
            }
            else
            {
                i++;
                col++;
            }
        }

        return i;
    }

    private void DisposeProcess()
    {
        _handshakeDone = false;
        foreach (var kv in _debounceByPath)
        {
            if (_debounceByPath.TryRemove(kv.Key, out var c))
            {
                try { c.Cancel(); } catch { }
                c.Dispose();
            }
        }

        lock (_gate)
        {
            _openedNormalized.Clear();
        }

        _syncedText.Clear();
        _strips.Clear();

        try
        {
            _runCts?.Cancel();
        }
        catch { }

        _session?.Dispose();
        _session = null;

        _runCts?.Dispose();
        _runCts = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        DisposeProcess();
    }
}
