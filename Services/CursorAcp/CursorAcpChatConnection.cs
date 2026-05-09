using System.Diagnostics;
using System.Text.Json;
using AgentClientProtocol;

namespace CascadeIDE.Services.CursorAcp;

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
    private string? _cachedExternalMcpJson;
    private bool _cachedAcpAutoInjectIdeMcp = true;
    private string? _cachedProcessPathForMcp;
    private string? _cachedPreferredCursorAcpModelId;
    private string? _lastAppliedPreferredCursorAcpModelId;
    private SessionModelState? _lastSessionModels;

    public bool IsDisposed { get; private set; }

    /// <summary>Снимок <c>session/new</c> (модели), обновляется при новой сессии.</summary>
    public SessionModelState? LastSessionModels => _lastSessionModels;

    /// <summary>Зеркалирование вывода ACP-терминала в нижнюю панель (и опционально показ вкладки Terminal).</summary>
    public void SetIdeTerminalCallbacks(Action<string>? appendTerminalUi, Action? showTerminalPanel)
    {
        _appendTerminalUi = appendTerminalUi;
        _showTerminalPanel = showTerminalPanel;
    }

    public async Task PromptAsync(
        string workspaceRoot,
        string configuredAgentPath,
        string externalMcpServersJson,
        bool acpAutoInjectIdeMcp,
        string? preferredCursorAcpModelId,
        string userText,
        Action<string>? appendMessageChunk,
        Action<string>? appendThoughtChunk,
        Action<CursorAcpStreamStage>? onStage,
        Action<SessionModelState?>? onSessionModels,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!CursorAcpAgentPath.TryResolve(configuredAgentPath, out var cmdPath, out var workDir))
            throw new InvalidOperationException(
                "Укажи путь к cursor-agent.cmd/каталогу dist-package в настройках или добавь cursor-agent в PATH.");

        if (string.IsNullOrWhiteSpace(workspaceRoot))
            workspaceRoot = workDir;

        workspaceRoot = CanonicalFilePath.Normalize(workspaceRoot);
        await EnsureSessionAsync(
            workspaceRoot,
            cmdPath,
            workDir,
            externalMcpServersJson,
            acpAutoInjectIdeMcp,
            preferredCursorAcpModelId,
            onSessionModels,
            cancellationToken).ConfigureAwait(false);

        if (_connection is null || string.IsNullOrEmpty(_sessionId))
            throw new InvalidOperationException("ACP: нет активной сессии.");

        _client.SetChunkHandlers(appendMessageChunk, appendThoughtChunk, onStage);
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
            _client.SetChunkHandlers(null, null, null);
        }
    }

    private async Task EnsureSessionAsync(
        string workspaceRoot,
        string cmdPath,
        string agentWorkingDirectory,
        string externalMcpServersJson,
        bool acpAutoInjectIdeMcp,
        string? preferredCursorAcpModelId,
        Action<SessionModelState?>? onSessionModels,
        CancellationToken cancellationToken)
    {
        var mcpJson = externalMcpServersJson ?? "";
        var processPath = Environment.ProcessPath ?? "";
        var pref = preferredCursorAcpModelId?.Trim() ?? "";
        _cachedPreferredCursorAcpModelId = pref;

        if (_connection is not null
            && _process is { HasExited: false }
            && !string.IsNullOrEmpty(_sessionId)
            && string.Equals(_cachedWorkspaceRoot, workspaceRoot, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_cachedCmdPath, cmdPath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_cachedExternalMcpJson, mcpJson, StringComparison.Ordinal)
            && _cachedAcpAutoInjectIdeMcp == acpAutoInjectIdeMcp
            && string.Equals(_cachedProcessPathForMcp, processPath, StringComparison.OrdinalIgnoreCase))
        {
            await TryApplyPreferredModelAndNotifyAsync(pref, onSessionModels, cancellationToken).ConfigureAwait(false);
            return;
        }

        DisposeProcessAndConnection();
        _cachedWorkspaceRoot = workspaceRoot;
        _cachedCmdPath = cmdPath;
        _cachedExternalMcpJson = mcpJson;
        _cachedAcpAutoInjectIdeMcp = acpAutoInjectIdeMcp;
        _cachedProcessPathForMcp = processPath;

        _process = CursorAcpChatAgentProcess.Start(
            cmdPath,
            agentWorkingDirectory,
            line => Debug.WriteLine("[cursor-acp stderr] " + line));

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

        var mcpServers = CascadeAcpMcpServerCatalog.MergeForAcpNewSession(mcpJson, acpAutoInjectIdeMcp);
        var sessionResult = await _connection.NewSessionAsync(new NewSessionRequest
        {
            Cwd = workspaceRoot,
            McpServers = mcpServers,
        }, cancellationToken).ConfigureAwait(false);

        _sessionId = sessionResult.SessionId;
        _lastSessionModels = sessionResult.Models;
        _client.SetExpectedSessionId(_sessionId);
        _client.ConfigureWorkspaceRoot(workspaceRoot);
        await TryApplyPreferredModelAndNotifyAsync(pref, onSessionModels, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Выбор модели после <c>session/new</c> (например из UI).</summary>
    public async Task<bool> TrySetSessionModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_connection is null || string.IsNullOrEmpty(_sessionId) || string.IsNullOrWhiteSpace(modelId))
            return false;

        await _connection.SetSessionModelAsync(
            new SetSessionModelRequest { SessionId = _sessionId, ModelId = modelId.Trim() },
            cancellationToken).ConfigureAwait(false);
        _lastAppliedPreferredCursorAcpModelId = modelId.Trim();
        return true;
    }

    private async Task TryApplyPreferredModelAndNotifyAsync(
        string preferredModelId,
        Action<SessionModelState?>? onSessionModels,
        CancellationToken cancellationToken)
    {
        if (_connection is null || string.IsNullOrEmpty(_sessionId))
            return;

        if (string.IsNullOrEmpty(preferredModelId))
        {
            onSessionModels?.Invoke(_lastSessionModels);
            return;
        }

        if (string.Equals(_lastAppliedPreferredCursorAcpModelId, preferredModelId, StringComparison.Ordinal))
        {
            onSessionModels?.Invoke(_lastSessionModels);
            return;
        }

        if (_lastSessionModels?.AvailableModels is not { Length: > 0 } list
            || !list.Any(m => string.Equals(m.ModelId, preferredModelId, StringComparison.Ordinal)))
        {
            onSessionModels?.Invoke(_lastSessionModels);
            return;
        }

        try
        {
            await _connection.SetSessionModelAsync(
                new SetSessionModelRequest { SessionId = _sessionId, ModelId = preferredModelId },
                cancellationToken).ConfigureAwait(false);
            _lastAppliedPreferredCursorAcpModelId = preferredModelId;
        }
        catch
        {
            // оставляем сессию; UI может сменить модель вручную
        }

        onSessionModels?.Invoke(_lastSessionModels);
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
        _cachedExternalMcpJson = null;
        _lastSessionModels = null;
        _lastAppliedPreferredCursorAcpModelId = null;

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
        _client.SetChunkHandlers(null, null, null);
        DisposeProcessAndConnection();
    }
}

internal sealed class CursorAcpIdeClient : IAcpClient
{
    private string _workspaceRoot = "";
    private Action<string>? _messageChunk;
    private Action<string>? _thoughtChunk;
    private Action<CursorAcpStreamStage>? _onStage;
    private AcpTerminalHost? _terminalHost;

    public void ConfigureWorkspaceRoot(string workspaceRoot) =>
        _workspaceRoot = CanonicalFilePath.Normalize(workspaceRoot.Trim());

    public void SetChunkHandlers(
        Action<string>? messageChunk,
        Action<string>? thoughtChunk,
        Action<CursorAcpStreamStage>? onStage)
    {
        _messageChunk = messageChunk;
        _thoughtChunk = thoughtChunk;
        _onStage = onStage;
    }

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
                _onStage?.Invoke(CursorAcpStreamStage.MessageChunk);
                _messageChunk?.Invoke(text.Text);
                break;
            case AgentThoughtChunkSessionUpdate th when th.Content is TextContentBlock tt:
                _onStage?.Invoke(CursorAcpStreamStage.ThoughtChunk);
                _thoughtChunk?.Invoke(tt.Text);
                break;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<WriteTextFileResponse> WriteTextFileAsync(WriteTextFileRequest request, CancellationToken cancellationToken = default)
    {
        CursorAcpWorkspaceFileAccess.WriteTextFileUnderWorkspace(_workspaceRoot, request.Path, request.Content);
        return ValueTask.FromResult(new WriteTextFileResponse());
    }

    public ValueTask<ReadTextFileResponse> ReadTextFileAsync(ReadTextFileRequest request, CancellationToken cancellationToken = default)
    {
        var text = CursorAcpWorkspaceFileAccess.ReadTextFileOrEmpty(_workspaceRoot, request.Path);
        return ValueTask.FromResult(new ReadTextFileResponse { Content = text });
    }

    public ValueTask<CreateTerminalResponse> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken cancellationToken = default)
    {
        _onStage?.Invoke(CursorAcpStreamStage.ToolCall);
        if (_terminalHost is null)
            throw new InvalidOperationException("ACP terminal: хост не инициализирован.");
        return ValueTask.FromResult(_terminalHost.Create(request));
    }

    public ValueTask<TerminalOutputResponse> TerminalOutputAsync(TerminalOutputRequest request, CancellationToken cancellationToken = default)
    {
        _onStage?.Invoke(CursorAcpStreamStage.ToolCall);
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
        _onStage?.Invoke(CursorAcpStreamStage.ToolCall);
        if (_terminalHost is null)
            return new WaitForTerminalExitResponse();
        return await _terminalHost.WaitForExitAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<KillTerminalCommandResponse> KillTerminalCommandAsync(KillTerminalCommandRequest request, CancellationToken cancellationToken = default)
    {
        _onStage?.Invoke(CursorAcpStreamStage.ToolCall);
        if (_terminalHost is null)
            return ValueTask.FromResult(new KillTerminalCommandResponse());
        return ValueTask.FromResult(_terminalHost.Kill(request));
    }

    public ValueTask<JsonElement> ExtMethodAsync(string method, JsonElement request, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }));

    public ValueTask ExtNotificationAsync(string method, JsonElement notification, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}

public enum CursorAcpStreamStage
{
    ThoughtChunk = 0,
    MessageChunk = 1,
    ToolCall = 2
}
