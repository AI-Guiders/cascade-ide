using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Threading;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Services.Lsp;

/// <summary>
/// Один процесс C# LSP (stdio), <see cref="textDocument/publishDiagnostics"/> → полосы в UI.
/// didOpen / didChange (full sync) для открытых .cs.
/// </summary>
public sealed class CSharpLspDiagnosticsHost : ILspDiagnosticSource
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonDocument?>> _pending = new();
    private readonly ConcurrentDictionary<string, List<EditorDiagnosticStrip>> _strips = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _syncedText = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _openedNormalized = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private Process? _process;
    private Stream? _processStdIn;
    private CancellationTokenSource? _runCts;
    private Task? _readLoop;
    private int _nextRequestId = 1;
    private int _versionCounter;
    private volatile bool _handshakeDone;
    private bool _disposed;

    public bool IsActive => _handshakeDone && _process is { HasExited: false };

    public event Action? DiagnosticsChanged;

    public IReadOnlyList<EditorDiagnosticStrip> GetStripsForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];
        var key = NormalizePath(filePath);
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

        var solutionDir = Path.GetDirectoryName(Path.GetFullPath(solutionPath));
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

        _process = proc;
        _processStdIn = proc.StandardInput.BaseStream;
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var runToken = _runCts.Token;

        _ = Task.Run(() => DrainStdErrAsync(proc.StandardError, runToken), runToken);

        _readLoop = Task.Run(() => ReadLoopAsync(proc.StandardOutput.BaseStream, runToken), runToken);

        try
        {
            var rootUri = PathToFileUri(solutionDir);
            var initId = Interlocked.Increment(ref _nextRequestId);
            var tcs = new TaskCompletionSource<JsonDocument?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[initId] = tcs;

            await SendInitializeAsync(initId, rootUri, runToken).ConfigureAwait(false);

            JsonDocument initResult;
            try
            {
                var doc = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60), runToken).ConfigureAwait(false);
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

    public void EnsureOpened(string filePath, string text)
    {
        if (!IsActive || string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;
        var key = NormalizePath(filePath);
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
        var key = NormalizePath(filePath);

        if (_debounceByPath.TryGetValue(key, out var old))
        {
            try { old.Cancel(); } catch { }
            old.Dispose();
        }

        var cts = new CancellationTokenSource();
        _debounceByPath[key] = cts;
        _ = DebouncedSyncAsync(key, filePath, text, cts.Token);
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

    private async Task SendInitializeAsync(int id, string rootUri, CancellationToken ct)
    {
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
                        ["synchronization"] = new JsonObject { ["dynamicRegistration"] = false }
                    },
                    ["workspace"] = new JsonObject { ["workspaceFolders"] = true }
                },
                ["workspaceFolders"] = new JsonArray
                {
                    new JsonObject { ["uri"] = rootUri, ["name"] = "root" }
                }
            }
        };
        var bytes = Encoding.UTF8.GetBytes(msg.ToJsonString());
        if (_processStdIn is null)
            return;
        await LspStdioFraming.WriteMessageAsync(_processStdIn, bytes, ct).ConfigureAwait(false);
    }

    private async Task SendInitializedNotificationAsync(CancellationToken ct)
    {
        const string payload = """{"jsonrpc":"2.0","method":"initialized","params":{}}""";
        var bytes = Encoding.UTF8.GetBytes(payload);
        if (_processStdIn is null)
            return;
        await LspStdioFraming.WriteMessageAsync(_processStdIn, bytes, ct).ConfigureAwait(false);
    }

    private async Task SendDidOpenAsync(string filePath, string text, CancellationToken ct)
    {
        var uri = PathToFileUri(Path.GetFullPath(filePath));
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
        var bytes = Encoding.UTF8.GetBytes(msg.ToJsonString());
        if (_processStdIn is null)
            return;
        await LspStdioFraming.WriteMessageAsync(_processStdIn, bytes, ct).ConfigureAwait(false);
    }

    private async Task SendDidChangeFullAsync(string filePath, string text, CancellationToken ct)
    {
        var uri = PathToFileUri(Path.GetFullPath(filePath));
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
        var bytes = Encoding.UTF8.GetBytes(msg.ToJsonString());
        if (_processStdIn is null)
            return;
        await LspStdioFraming.WriteMessageAsync(_processStdIn, bytes, ct).ConfigureAwait(false);
    }

    private async Task ReadLoopAsync(Stream stdout, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                JsonDocument? doc;
                try
                {
                    doc = await LspStdioFraming.ReadMessageAsync(stdout, ct).ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                if (doc is null)
                    break;

                using (doc)
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
                    {
                        var id = idEl.GetInt32();
                        if (root.TryGetProperty("result", out _))
                        {
                            if (_pending.TryRemove(id, out var tcs))
                            {
                                var copy = JsonDocument.Parse(root.GetRawText());
                                tcs.TrySetResult(copy);
                            }

                            continue;
                        }

                        if (root.TryGetProperty("error", out var errorEl))
                        {
                            if (_pending.TryRemove(id, out var tcs))
                                tcs.TrySetException(new InvalidOperationException(errorEl.GetRawText()));
                            continue;
                        }
                    }

                    if (root.TryGetProperty("method", out var methodEl))
                    {
                        var method = methodEl.GetString();
                        if (method == "textDocument/publishDiagnostics" && root.TryGetProperty("params", out var p))
                            HandlePublishDiagnostics(p);
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            foreach (var kv in _pending)
            {
                if (_pending.TryRemove(kv.Key, out var tcs))
                    tcs.TrySetCanceled();
            }
        }
    }

    private void HandlePublishDiagnostics(JsonElement @params)
    {
        if (!@params.TryGetProperty("uri", out var uriEl))
            return;
        var uri = uriEl.GetString();
        if (string.IsNullOrEmpty(uri))
            return;
        if (!TryUriToPath(uri, out var path))
            return;
        var key = NormalizePath(path);

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
        UiScheduler.Default.Post(() => DiagnosticsChanged?.Invoke(), DispatcherPriority.Background);
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

    private static async Task DrainStdErrAsync(StreamReader err, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await err.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                    break;
                Debug.WriteLine("[csharp-lsp stderr] " + line);
            }
        }
        catch
        {
            // ignored
        }
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

        try
        {
            _processStdIn?.Dispose();
        }
        catch { }

        _processStdIn = null;

        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(2000);
            }
        }
        catch { }

        try
        {
            _process?.Dispose();
        }
        catch { }

        _process = null;
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

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path);

    private static string PathToFileUri(string fullPath)
    {
        var full = Path.GetFullPath(fullPath);
        return new Uri(full).AbsoluteUri;
    }

    private static bool TryUriToPath(string uri, out string path)
    {
        path = "";
        try
        {
            var u = new Uri(uri);
            path = u.LocalPath;
            if (OperatingSystem.IsWindows() && path.StartsWith('/') && path.Length > 2 && path[2] == ':')
                path = path.TrimStart('/');
            return !string.IsNullOrEmpty(path);
        }
        catch
        {
            return false;
        }
    }
}
