using System.Diagnostics;
using System.Text;
using AgentClientProtocol;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.CursorAcp.DataAcquisition;

/// <summary>
/// Процессы ACP <c>terminal/*</c>: вывод в буфер для опроса агентом и зеркалирование в UI нижней панели.
/// </summary>
[IoBoundary]
internal sealed class AcpTerminalHost
{
    private readonly object _mapLock = new();
    private readonly Dictionary<string, AcpTerminalSession> _sessions = [];
    private readonly string _workspaceRoot;
    private readonly Action<string>? _appendUi;
    private readonly Action? _showTerminal;
    private string? _expectedSessionId;

    public AcpTerminalHost(string workspaceRoot, Action<string>? appendUi, Action? showTerminal)
    {
        _workspaceRoot = CanonicalFilePath.Normalize(workspaceRoot.Trim());
        _appendUi = appendUi;
        _showTerminal = showTerminal;
    }

    public void SetExpectedSessionId(string sessionId) =>
        _expectedSessionId = sessionId;

    public void DisposeAllSessions()
    {
        lock (_mapLock)
        {
            foreach (var s in _sessions.Values)
                s.Dispose();
            _sessions.Clear();
        }
    }

    public CreateTerminalResponse Create(CreateTerminalRequest request)
    {
        ThrowIfSessionMismatch(request.SessionId);

        var cwd = NormalizeCwdUnderWorkspace(request.Cwd);
        if (cwd is null)
            throw new InvalidOperationException("ACP terminal: cwd вне workspace.");

        var id = Guid.NewGuid().ToString("N");
        var limit = request.OutputByteLimit ?? 8_000_000;

        var psi = new ProcessStartInfo
        {
            FileName = request.Command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = cwd,
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };
        foreach (var a in request.Args ?? [])
            psi.ArgumentList.Add(a);

        foreach (var e in request.Env ?? [])
        {
            if (!string.IsNullOrEmpty(e.Name))
                psi.Environment[e.Name] = e.Value ?? "";
        }

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
            throw new InvalidOperationException("ACP terminal: не удалось запустить процесс.");

        var session = new AcpTerminalSession(id, proc, limit, _appendUi);
        lock (_mapLock)
            _sessions[id] = session;

        session.StartPump();

        _appendUi?.Invoke($"\r\n--- ACP terminal {id} ---\r\n{request.Command} {string.Join(" ", request.Args ?? [])}\r\n");
        _showTerminal?.Invoke();

        return new CreateTerminalResponse { TerminalId = id };
    }

    public TerminalOutputResponse ReadOutput(TerminalOutputRequest request)
    {
        ThrowIfSessionMismatch(request.SessionId);
        lock (_mapLock)
        {
            if (!_sessions.TryGetValue(request.TerminalId, out var s))
            {
                return new TerminalOutputResponse
                {
                    Output = "",
                    Truncated = false,
                    ExitStatus = null,
                };
            }

            return s.DequeueOutput();
        }
    }

    public ReleaseTerminalResponse Release(ReleaseTerminalRequest request)
    {
        ThrowIfSessionMismatch(request.SessionId);
        AcpTerminalSession? s;
        lock (_mapLock)
        {
            _sessions.Remove(request.TerminalId, out s);
        }

        s?.Dispose();
        return new ReleaseTerminalResponse();
    }

    public async ValueTask<WaitForTerminalExitResponse> WaitForExitAsync(
        WaitForTerminalExitRequest request,
        CancellationToken cancellationToken)
    {
        ThrowIfSessionMismatch(request.SessionId);
        AcpTerminalSession? s;
        lock (_mapLock)
            _sessions.TryGetValue(request.TerminalId, out s);
        if (s is null)
            return new WaitForTerminalExitResponse();

        await s.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return new WaitForTerminalExitResponse
        {
            ExitCode = (uint)Math.Max(0, s.ExitCode),
        };
    }

    public KillTerminalCommandResponse Kill(KillTerminalCommandRequest request)
    {
        ThrowIfSessionMismatch(request.SessionId);
        AcpTerminalSession? s;
        lock (_mapLock)
            _sessions.TryGetValue(request.TerminalId, out s);
        if (s is null)
            return new KillTerminalCommandResponse();

        try
        {
            if (s is { Process.HasExited: false })
                s.Process.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }

        return new KillTerminalCommandResponse();
    }

    private void ThrowIfSessionMismatch(string sessionId)
    {
        if (_expectedSessionId is null || !string.Equals(sessionId, _expectedSessionId, StringComparison.Ordinal))
            throw new InvalidOperationException("ACP terminal: неверный sessionId.");
    }

    private string? NormalizeCwdUnderWorkspace(string? cwd)
    {
        var root = _workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
        if (string.IsNullOrWhiteSpace(cwd))
            return _workspaceRoot;

        var full = Path.IsPathRooted(cwd)
            ? CanonicalFilePath.Normalize(cwd)
            : CanonicalFilePath.Normalize(Path.Combine(_workspaceRoot, cwd));
        var norm = CanonicalFilePath.Normalize(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                   + Path.DirectorySeparatorChar;
        if (!norm.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;
        return CanonicalFilePath.Normalize(full);
    }

    private sealed class AcpTerminalSession : IDisposable
    {
        private readonly object _bufLock = new();
        private readonly StringBuilder _buffer = new();
        private readonly ulong _byteLimit;
        private readonly Action<string>? _appendUi;
        private readonly TaskCompletionSource _exitTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _sentCharIndex;
        private ulong _bytesAccepted;
        private bool _truncated;
        private bool _disposed;
        private int _exitCode;
        private bool _exited;

        public AcpTerminalSession(string id, Process process, ulong byteLimit, Action<string>? appendUi)
        {
            Id = id;
            Process = process;
            _byteLimit = byteLimit;
            _appendUi = appendUi;
            process.Exited += OnExited;
        }

        public string Id { get; }
        public Process Process { get; }

        public int ExitCode => _exitCode;

        public void StartPump()
        {
            _ = Task.Run(() => PumpAsync(Process.StandardOutput));
            _ = Task.Run(() => PumpAsync(Process.StandardError));
        }

        private void OnExited(object? sender, EventArgs e)
        {
            lock (_bufLock)
            {
                _exited = true;
                try
                {
                    _exitCode = Process.ExitCode;
                }
                catch
                {
                    _exitCode = -1;
                }
            }

            _exitTcs.TrySetResult();
        }

        private async Task PumpAsync(StreamReader reader)
        {
            var buf = new char[4096];
            try
            {
                while (true)
                {
                    var n = await reader.ReadAsync(buf.AsMemory(0, buf.Length)).ConfigureAwait(false);
                    if (n <= 0)
                        break;
                    AppendChunk(new string(buf, 0, n));
                }
            }
            catch
            {
                // broken pipe / dispose
            }
        }

        private void AppendChunk(string chunk)
        {
            if (chunk.Length == 0)
                return;

            lock (_bufLock)
            {
                if (_bytesAccepted >= _byteLimit)
                {
                    _truncated = true;
                    return;
                }

                var byteCount = (ulong)Encoding.UTF8.GetByteCount(chunk);
                if (_bytesAccepted + byteCount <= _byteLimit)
                {
                    _buffer.Append(chunk);
                    _bytesAccepted += byteCount;
                    _appendUi?.Invoke(chunk);
                    return;
                }

                _truncated = true;
                var remaining = _byteLimit - _bytesAccepted;
                var low = 0;
                var high = chunk.Length;
                while (low < high)
                {
                    var mid = (low + high + 1) / 2;
                    var sub = chunk[..mid];
                    if ((ulong)Encoding.UTF8.GetByteCount(sub) <= remaining)
                        low = mid;
                    else
                        high = mid - 1;
                }

                if (low <= 0)
                    return;

                var fit = chunk[..low];
                _buffer.Append(fit);
                _bytesAccepted += (ulong)Encoding.UTF8.GetByteCount(fit);
                _appendUi?.Invoke(fit);
            }
        }

        public TerminalOutputResponse DequeueOutput()
        {
            lock (_bufLock)
            {
                var tail = _buffer.ToString(_sentCharIndex, _buffer.Length - _sentCharIndex);
                _sentCharIndex = _buffer.Length;

                TerminalExitStatus? exit = null;
                if (_exited)
                {
                    exit = new TerminalExitStatus
                    {
                        ExitCode = (uint)Math.Max(0, _exitCode),
                    };
                }

                return new TerminalOutputResponse
                {
                    Output = tail,
                    Truncated = _truncated,
                    ExitStatus = exit,
                };
            }
        }

        public async Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            await _exitTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try
            {
                if (Process is { HasExited: false })
                    Process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }

            try
            {
                Process.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}
