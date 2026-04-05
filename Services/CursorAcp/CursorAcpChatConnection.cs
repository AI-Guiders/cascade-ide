using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AgentClientProtocol;

namespace CascadeIDE.Services.CursorAcp;

/// <summary>Сопоставление пути из настроек с <c>cursor-agent.cmd</c> из пакета Cursor ACP.</summary>
public static class CursorAcpAgentPath
{
    /// <summary>Возвращает полный путь к cmd и рабочий каталог для процесса.</summary>
    public static bool TryResolve(string? configured, out string cmdPath, out string workingDirectory)
    {
        cmdPath = "";
        workingDirectory = "";
        if (string.IsNullOrWhiteSpace(configured))
            return false;

        var trimmed = configured.Trim();
        if (File.Exists(trimmed))
        {
            var ext = Path.GetExtension(trimmed);
            if (string.Equals(ext, ".cmd", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".bat", StringComparison.OrdinalIgnoreCase))
            {
                cmdPath = Path.GetFullPath(trimmed);
                workingDirectory = Path.GetDirectoryName(cmdPath) ?? "";
                return true;
            }
        }

        if (!Directory.Exists(trimmed))
            return false;

        var dir = Path.GetFullPath(trimmed);
        foreach (var rel in new[] { Path.Combine("dist-package", "cursor-agent.cmd"), "cursor-agent.cmd" })
        {
            var p = Path.Combine(dir, rel);
            if (File.Exists(p))
            {
                cmdPath = Path.GetFullPath(p);
                workingDirectory = Path.GetDirectoryName(cmdPath) ?? dir;
                return true;
            }
        }

        return false;
    }
}

/// <summary>stdio-процесс Cursor agent + <see cref="ClientSideConnection"/>: один сеанс ACP на жизненный цикл подключения.</summary>
public sealed class CursorAcpChatConnection : IDisposable
{
    private readonly CursorAcpIdeClient _client = new();
    private Action<string>? _appendTerminalUi;
    private Action? _showTerminalPanel;
    private Process? _process;
    private ClientSideConnection? _connection;
    private string? _sessionId;
    private string? _cachedWorkspaceRoot;
    private string? _cachedCmdPath;

    public bool IsDisposed { get; private set; }

    /// <summary>Зеркалирование вывода ACP-терминала в нижнюю панель (и опционально показ вкладки Terminal).</summary>
    public void SetIdeTerminalCallbacks(Action<string>? appendTerminalUi, Action? showTerminalPanel)
    {
        _appendTerminalUi = appendTerminalUi;
        _showTerminalPanel = showTerminalPanel;
    }

    public async Task PromptAsync(
        string workspaceRoot,
        string configuredAgentPath,
        string userText,
        Action<string> appendChunk,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!CursorAcpAgentPath.TryResolve(configuredAgentPath, out var cmdPath, out var workDir))
            throw new InvalidOperationException(
                "Укажи путь к cursor-agent.cmd или к каталогу с dist-package в настройках (Cursor ACP).");

        if (string.IsNullOrWhiteSpace(workspaceRoot))
            workspaceRoot = workDir;

        workspaceRoot = Path.GetFullPath(workspaceRoot);
        await EnsureSessionAsync(workspaceRoot, cmdPath, workDir, cancellationToken).ConfigureAwait(false);

        if (_connection is null || string.IsNullOrEmpty(_sessionId))
            throw new InvalidOperationException("ACP: нет активной сессии.");

        _client.SetChunkHandler(appendChunk);
        try
        {
            await _connection.PromptAsync(new PromptRequest
            {
                SessionId = _sessionId,
                Prompt = [new TextContentBlock { Text = userText }],
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _client.SetChunkHandler(null);
        }
    }

    private async Task EnsureSessionAsync(
        string workspaceRoot,
        string cmdPath,
        string agentWorkingDirectory,
        CancellationToken cancellationToken)
    {
        if (_connection is not null
            && _process is { HasExited: false }
            && !string.IsNullOrEmpty(_sessionId)
            && string.Equals(_cachedWorkspaceRoot, workspaceRoot, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_cachedCmdPath, cmdPath, StringComparison.OrdinalIgnoreCase))
            return;

        DisposeProcessAndConnection();
        _cachedWorkspaceRoot = workspaceRoot;
        _cachedCmdPath = cmdPath;

        var psi = new ProcessStartInfo
        {
            FileName = cmdPath,
            Arguments = "acp",
            WorkingDirectory = string.IsNullOrEmpty(agentWorkingDirectory) ? Path.GetDirectoryName(cmdPath) ?? "" : agentWorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // Иначе на Windows берётся OEM-кодировка консоли — JSON/UTF-8 от агента даёт «кракозябры» в чате.
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        _process = new Process { StartInfo = psi };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.WriteLine("[cursor-acp stderr] " + e.Data);
        };

        if (!_process.Start())
            throw new InvalidOperationException("Не удалось запустить cursor-agent (ACP).");

        _process.BeginErrorReadLine();

        _connection = new ClientSideConnection(_ => _client, _process.StandardOutput, _process.StandardInput);
        _connection.Open();

        _client.SetTerminalCallbacks(_appendTerminalUi, _showTerminalPanel, workspaceRoot);

        await _connection.InitializeAsync(new InitializeRequest
        {
            ProtocolVersion = 1,
            ClientInfo = new Implementation { Name = "CascadeIDE", Version = typeof(CursorAcpChatConnection).Assembly.GetName().Version?.ToString() ?? "0" },
            ClientCapabilities = new ClientCapabilities
            {
                Fs = new FileSystemCapability { ReadTextFile = true, WriteTextFile = true },
                Terminal = true,
            },
        }, cancellationToken).ConfigureAwait(false);

        var sessionResult = await _connection.NewSessionAsync(new NewSessionRequest
        {
            Cwd = workspaceRoot,
            McpServers = [],
        }, cancellationToken).ConfigureAwait(false);

        _sessionId = sessionResult.SessionId;
        _client.SetExpectedSessionId(_sessionId);
        _client.ConfigureWorkspaceRoot(workspaceRoot);
    }

    private void DisposeProcessAndConnection()
    {
        _client.DisposeTerminalSessions();
        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // ignore
        }

        _connection = null;
        _sessionId = null;

        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            _process?.Dispose();
        }
        catch
        {
            // ignore
        }

        _process = null;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        _client.SetChunkHandler(null);
        DisposeProcessAndConnection();
    }
}

internal sealed class CursorAcpIdeClient : IAcpClient
{
    private string _workspaceRoot = "";
    private Action<string>? _chunk;
    private AcpTerminalHost? _terminalHost;

    public void ConfigureWorkspaceRoot(string workspaceRoot) =>
        _workspaceRoot = Path.GetFullPath(workspaceRoot.Trim());

    public void SetChunkHandler(Action<string>? chunk) => _chunk = chunk;

    public void SetTerminalCallbacks(Action<string>? appendUi, Action? showTerminalPanel, string workspaceRoot) =>
        _terminalHost = new AcpTerminalHost(workspaceRoot, appendUi, showTerminalPanel);

    public void SetExpectedSessionId(string sessionId) =>
        _terminalHost?.SetExpectedSessionId(sessionId);

    public void DisposeTerminalSessions()
    {
        _terminalHost?.DisposeAllSessions();
        _terminalHost = null;
    }

    public ValueTask<RequestPermissionResponse> RequestPermissionAsync(
        RequestPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Options is not { Length: > 0 })
        {
            return ValueTask.FromResult(new RequestPermissionResponse
            {
                Outcome = new CancelledRequestPermissionOutcome(),
            });
        }

        PermissionOption? pick = null;
        foreach (var o in request.Options)
        {
            if (o.Kind is PermissionOptionKind.AllowOnce or PermissionOptionKind.AllowAlways)
            {
                pick = o;
                break;
            }
        }

        pick ??= request.Options[0];

        return ValueTask.FromResult(new RequestPermissionResponse
        {
            Outcome = new SelectedRequestPermissionOutcome
            {
                OptionId = pick.OptionId,
            },
        });
    }

    public ValueTask SessionNotificationAsync(SessionNotification notification, CancellationToken cancellationToken = default)
    {
        var update = notification.Update;
        switch (update)
        {
            case AgentMessageChunkSessionUpdate m when m.Content is TextContentBlock text:
                _chunk?.Invoke(text.Text);
                break;
            case AgentThoughtChunkSessionUpdate th when th.Content is TextContentBlock tt:
                _chunk?.Invoke(tt.Text);
                break;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<WriteTextFileResponse> WriteTextFileAsync(WriteTextFileRequest request, CancellationToken cancellationToken = default)
    {
        var path = SafeResolvePath(request.Path);
        if (path is null)
            return ValueTask.FromResult(new WriteTextFileResponse());

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, request.Content ?? "");
        return ValueTask.FromResult(new WriteTextFileResponse());
    }

    public ValueTask<ReadTextFileResponse> ReadTextFileAsync(ReadTextFileRequest request, CancellationToken cancellationToken = default)
    {
        var path = SafeResolvePath(request.Path);
        if (path is null || !File.Exists(path))
            return ValueTask.FromResult(new ReadTextFileResponse { Content = "" });
        var text = File.ReadAllText(path);
        return ValueTask.FromResult(new ReadTextFileResponse { Content = text });
    }

    private string? SafeResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        var full = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(_workspaceRoot, path));
        var root = Path.GetFullPath(_workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;
        return full;
    }

    public ValueTask<CreateTerminalResponse> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken cancellationToken = default)
    {
        if (_terminalHost is null)
            throw new InvalidOperationException("ACP terminal: хост не инициализирован.");
        return ValueTask.FromResult(_terminalHost.Create(request));
    }

    public ValueTask<TerminalOutputResponse> TerminalOutputAsync(TerminalOutputRequest request, CancellationToken cancellationToken = default)
    {
        if (_terminalHost is null)
        {
            return ValueTask.FromResult(new TerminalOutputResponse
            {
                Output = "",
                Truncated = false,
            });
        }

        return ValueTask.FromResult(_terminalHost.ReadOutput(request));
    }

    public ValueTask<ReleaseTerminalResponse> ReleaseTerminalAsync(ReleaseTerminalRequest request, CancellationToken cancellationToken = default)
    {
        if (_terminalHost is null)
            return ValueTask.FromResult(new ReleaseTerminalResponse());
        return ValueTask.FromResult(_terminalHost.Release(request));
    }

    public async ValueTask<WaitForTerminalExitResponse> WaitForTerminalExitAsync(
        WaitForTerminalExitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_terminalHost is null)
            return new WaitForTerminalExitResponse();
        return await _terminalHost.WaitForExitAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<KillTerminalCommandResponse> KillTerminalCommandAsync(KillTerminalCommandRequest request, CancellationToken cancellationToken = default)
    {
        if (_terminalHost is null)
            return ValueTask.FromResult(new KillTerminalCommandResponse());
        return ValueTask.FromResult(_terminalHost.Kill(request));
    }

    public ValueTask<JsonElement> ExtMethodAsync(string method, JsonElement request, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }));

    public ValueTask ExtNotificationAsync(string method, JsonElement notification, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
