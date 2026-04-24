using System.Text;
using System.Text.Json;
using DotnetDebug.Core;

namespace CascadeIDE.Services;

/// <summary>Одна активная DAP-сессия (netcoredbg) для IDE: launch/attach, шаги, канонический <see cref="DebugSessionSnapshot"/>.</summary>
public sealed class IdeDapDebugSession
{
    private readonly Action? _onSnapshotChanged;

    private DapClient? _client;
    private int _lastStoppedThreadId;
    private string? _lastExceptionText;
    private TaskCompletionSource? _stoppedWaitTcs;
    private readonly object _stoppedLock = new();
    private readonly object _snapshotLock = new();
    private DebugSessionSnapshot _snapshot = DebugSessionSnapshot.Empty;
    private string? _sessionWorkspacePath;
    private string? _sessionTargetPath;
    /// <summary>Кадр стека, для которого в снимке загружены <see cref="DebugSessionSnapshot.VariableRootScopes"/> (UI / MCP).</summary>
    private int _variablesFrameIndex;

    public IdeDapDebugSession(Action? onSnapshotChanged)
    {
        _onSnapshotChanged = onSnapshotChanged;
    }

    /// <summary>Есть активный DAP-клиент (launch или attach).</summary>
    public bool HasActiveSession => _client != null;

    /// <summary>Выполнение остановлено (есть threadId от последнего stopped).</summary>
    public bool IsExecutionStopped => _lastStoppedThreadId != 0;

    /// <summary>Канонический снимок для UI, omni, MCP (ADR 0002).</summary>
    public DebugSessionSnapshot GetSnapshot()
    {
        lock (_snapshotLock)
            return _snapshot;
    }

    /// <summary>Смена клиента или stopped/continued — для обновления CanExecute у команд UI.</summary>
    public event EventHandler? StateChanged;

    private void SetSnapshot(DebugSessionSnapshot snapshot)
    {
        lock (_snapshotLock)
            _snapshot = snapshot;
        _onSnapshotChanged?.Invoke();
    }

    private void UpdateSnapshot(Func<DebugSessionSnapshot, DebugSessionSnapshot> update)
    {
        var current = GetSnapshot();
        SetSnapshot(update(current));
    }

    private static IReadOnlyList<DebugBreakpointSnapshot> LoadBreakpointSnapshot(string workspacePath, string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return Array.Empty<DebugBreakpointSnapshot>();

        var workspaceRoot = GetWorkspaceRootFromPath(workspacePath);

        return BreakpointsStorage.GetBreakpoints(workspacePath, targetPath)
            .Select(b => new DebugBreakpointSnapshot(
                DapShared.ResolveBreakpointFilePath(workspaceRoot, b.File),
                b.Line,
                b.Condition))
            .OrderBy(b => b.File, StringComparer.OrdinalIgnoreCase)
            .ThenBy(b => b.Line)
            .ToList();
    }

    public void RefreshBreakpointSnapshotFromStorage(string? workspacePath, string? targetPath = null)
    {
        var breakpoints = string.IsNullOrWhiteSpace(workspacePath)
            ? Array.Empty<DebugBreakpointSnapshot>()
            : LoadBreakpointSnapshot(workspacePath!, targetPath);
        UpdateSnapshot(current => current with { Breakpoints = breakpoints });
    }

    private void PrepareStoppedWait()
    {
        lock (_stoppedLock)
            _stoppedWaitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void OnStoppedEvent(int threadId, string? exceptionText)
    {
        _lastStoppedThreadId = threadId;
        _lastExceptionText = exceptionText;
        _variablesFrameIndex = 0;
        lock (_stoppedLock)
        {
            var t = _stoppedWaitTcs;
            _stoppedWaitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            t?.TrySetResult();
        }
        RaiseStateChanged();
    }

    private void OnContinuedEvent()
    {
        _lastStoppedThreadId = 0;
        _lastExceptionText = null;
        _variablesFrameIndex = 0;
        if (HasActiveSession)
        {
            var breakpoints = GetSnapshot().Breakpoints;
            SetSnapshot(new DebugSessionSnapshot(
                HasActiveSession: true,
                IsExecutionStopped: false,
                StoppedFile: null,
                StoppedLine: 0,
                ExceptionText: null,
                Breakpoints: breakpoints,
                StackFrames: Array.Empty<(string Name, string? File, int Line)>(),
                VariableRootScopes: Array.Empty<DebugVariableRootScope>(),
                VariablesFrameIndex: 0));
        }
        else
            UpdateSnapshot(current => current with
            {
                HasActiveSession = false,
                IsExecutionStopped = false,
                StoppedFile = null,
                StoppedLine = 0,
                ExceptionText = null,
                StackFrames = Array.Empty<(string Name, string? File, int Line)>(),
                VariableRootScopes = Array.Empty<DebugVariableRootScope>(),
                VariablesFrameIndex = 0
            });
        RaiseStateChanged();
    }

    /// <summary>RelayCommands и Avalonia требуют UI-поток; DAP и <c>ConfigureAwait(false)</c> продолжают на фоне.</summary>
    private void NotifySessionClientChanged() => RaiseStateChanged();

    private void RaiseStateChanged()
    {
        UiScheduler.Default.Post(() => StateChanged?.Invoke(this, EventArgs.Empty));
    }

    private async Task WaitForStoppedAsync(TimeSpan timeout)
    {
        if (_client == null)
            throw new InvalidOperationException("No active debug session.");
        Task waitTask;
        lock (_stoppedLock)
        {
            if (_lastStoppedThreadId != 0)
                return;
            waitTask = _stoppedWaitTcs?.Task ?? Task.CompletedTask;
        }
        await waitTask.WaitAsync(timeout).ConfigureAwait(false);
    }

    private (DapClient client, int threadId) GetSessionAndThreadId()
    {
        var client = _client ?? throw new InvalidOperationException("No active debug session.");
        var threadId = _lastStoppedThreadId;
        if (threadId == 0)
            throw new InvalidOperationException("Execution is not stopped (no stopped event yet).");
        return (client, threadId);
    }

    private static string ResolveNetcoreDbgPath(string? fromArgs)
    {
        var p = Environment.GetEnvironmentVariable("NETCOREDBG_PATH")?.Trim();
        if (!string.IsNullOrWhiteSpace(fromArgs))
            p = fromArgs;
        return string.IsNullOrWhiteSpace(p) ? "netcoredbg" : p!;
    }

    /// <summary>
    /// <c>dotnet</c> с аргументами <c>test</c> / <c>exec</c> должен стартовать с рабочим каталогом проекта (workspace),
    /// иначе cwd по умолчанию — каталог самого <c>dotnet.exe</c> и netcoredbg может завершить конфигурацию с ошибкой.
    /// </summary>
    private static string ResolveLaunchWorkingDirectory(string programPath, string workspaceRoot, IReadOnlyList<string>? programArgs)
    {
        var dir = Path.GetDirectoryName(programPath);
        var fallback = string.IsNullOrEmpty(dir) ? workspaceRoot : dir;
        if (programArgs is not { Count: > 0 })
            return fallback;
        var head = programArgs[0];
        if (!programPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            return fallback;
        if (string.Equals(head, "test", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(head, "exec", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(head, "run", StringComparison.OrdinalIgnoreCase))
            return workspaceRoot;
        return fallback;
    }

    private static string GetWorkspaceRootFromPath(string workspacePath)
    {
        var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
        if (File.Exists(workspaceRoot))
            workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
        return workspaceRoot;
    }

    private static List<BreakpointsStorage.BreakpointEntry> LoadBreakpointEntries(
        string workspacePath,
        string workspaceRoot,
        string? targetPath,
        out Dictionary<string, List<(int Line, string? Condition)>> byFile)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            byFile = new Dictionary<string, List<(int Line, string? Condition)>>(StringComparer.OrdinalIgnoreCase);
            return new List<BreakpointsStorage.BreakpointEntry>();
        }

        var breakpoints = BreakpointsStorage.GetBreakpoints(workspacePath, targetPath).ToList();
        byFile = breakpoints
            .GroupBy(b => DapShared.ResolveBreakpointFilePath(workspaceRoot, b.File))
            .ToDictionary(g => g.Key, g => g.Select(b => (b.Line, b.Condition)).ToList());
        return breakpoints;
    }

    private void RegisterDapSessionHandlers(DapClient client, TaskCompletionSource stoppedTcs)
    {
        client.OnConnectionLost = () => HandleDapConnectionLost(client);
        client.OnEvent = (eventName, body) => HandleDapSessionEvent(eventName, body, client, stoppedTcs);
    }

    private void HandleDapConnectionLost(DapClient client)
    {
        if (_client != client)
            return;
        _client = null;
        _lastStoppedThreadId = 0;
        _lastExceptionText = null;
        _sessionWorkspacePath = null;
        _sessionTargetPath = null;
        UpdateSnapshot(current => current with
        {
            HasActiveSession = false,
            IsExecutionStopped = false,
            StoppedFile = null,
            StoppedLine = 0,
            ExceptionText = null,
            StackFrames = Array.Empty<(string Name, string? File, int Line)>(),
            VariableRootScopes = Array.Empty<DebugVariableRootScope>(),
            VariablesFrameIndex = 0
        });
        NotifySessionClientChanged();
    }

    private void HandleDapSessionEvent(string eventName, JsonElement body, DapClient client, TaskCompletionSource stoppedTcs)
    {
        if (eventName == "stopped" && body.TryGetProperty("threadId", out var tid))
        {
            var reason = body.TryGetProperty("reason", out var r) ? r.GetString() : null;
            var exceptionText = (reason == "exception" && body.TryGetProperty("text", out var txt)) ? txt.GetString() : null;
            OnStoppedEvent(tid.GetInt32(), exceptionText);
            stoppedTcs.TrySetResult();
            var tidVal = tid.GetInt32();
            _ = RefreshStoppedUiAsync(client, tidVal);
        }
        else if (eventName == "continued")
        {
            OnContinuedEvent();
        }
    }

    /// <summary>Текст для MCP-ответа; UI обновляется в событии stopped.</summary>
    public async Task<string> LaunchAsync(
        string workspacePath,
        string targetPath,
        string? netcoredbgPath,
        IReadOnlyList<string>? programArgs,
        IReadOnlyDictionary<string, string>? environment = null,
        string? workingDirectoryOverride = null,
        CancellationToken cancellationToken = default)
    {
        var netcoredbg = ResolveNetcoreDbgPath(netcoredbgPath);
        var workspaceRoot = GetWorkspaceRootFromPath(workspacePath);
        var programPath = Path.IsPathRooted(targetPath.Trim())
            ? Path.GetFullPath(targetPath)
            : Path.GetFullPath(Path.Combine(workspaceRoot, targetPath.Trim()));
        if (!File.Exists(programPath))
            throw new ArgumentException($"Target not found: {programPath}");

        var breakpoints = LoadBreakpointEntries(workspacePath, workspaceRoot, targetPath, out var byFile);

        await StopInternalAsync().ConfigureAwait(false);

        var client = await DapClient.StartAsync(netcoredbg, cancellationToken, clientId: "cascade-ide", clientName: "CascadeIDE").ConfigureAwait(false);
        PrepareStoppedWait();
        var stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterDapSessionHandlers(client, stoppedTcs);

        var exceptionBpsOk = false;
        try
        {
            string launchCwd;
            if (!string.IsNullOrWhiteSpace(workingDirectoryOverride))
            {
                var w = workingDirectoryOverride.Trim();
                launchCwd = Path.IsPathRooted(w)
                    ? Path.GetFullPath(w)
                    : Path.GetFullPath(Path.Combine(workspaceRoot, w));
            }
            else
                launchCwd = ResolveLaunchWorkingDirectory(programPath, workspaceRoot, programArgs);
            await client.LaunchAsync(programPath, launchCwd, programArgs, environment, cancellationToken).ConfigureAwait(false);
            foreach (var (file, list) in byFile)
            {
                if (list.Count > 0)
                    await client.SetBreakpointsAsync(file, list, cancellationToken).ConfigureAwait(false);
            }
            exceptionBpsOk = await DapShared.TrySetUnhandledExceptionBreakpointsAsync(client).ConfigureAwait(false);
            await client.ConfigurationDoneAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await stoppedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                stoppedTcs.TrySetResult();
            }
        }
        catch
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        _client = client;
        _sessionWorkspacePath = workspacePath;
        _sessionTargetPath = targetPath;
        RefreshBreakpointSnapshotFromStorage(workspacePath, targetPath);
        NotifySessionClientChanged();
        await EnsureThreadIdAfterLaunchAsync(client).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("# Debug session started");
        sb.AppendLine($"# Program: {programPath}");
        sb.AppendLine($"# Breakpoints: {breakpoints.Count} applied");
        sb.AppendLine(exceptionBpsOk
            ? "# Exception breakpoints: unhandled (stop on throw)"
            : "# Exception breakpoints: skipped (adapter rejected setExceptionBreakpoints)");
        if (_lastExceptionText is { } exMsg)
            sb.AppendLine($"# Stopped on exception: {exMsg}");
        return sb.ToString();
    }

    public async Task<string> AttachAsync(
        string workspacePath,
        int processId,
        string? targetPath,
        string? netcoredbgPath,
        CancellationToken cancellationToken = default)
    {
        var netcoredbg = ResolveNetcoreDbgPath(netcoredbgPath);
        var workspaceRoot = GetWorkspaceRootFromPath(workspacePath);
        var breakpoints = LoadBreakpointEntries(workspacePath, workspaceRoot, targetPath, out var byFile);

        await StopInternalAsync().ConfigureAwait(false);

        var client = await DapClient.StartAsync(netcoredbg, cancellationToken, clientId: "cascade-ide", clientName: "CascadeIDE").ConfigureAwait(false);
        PrepareStoppedWait();
        var stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterDapSessionHandlers(client, stoppedTcs);

        var attachExceptionBpsOk = false;
        try
        {
            await client.AttachAsync(processId, cancellationToken).ConfigureAwait(false);
            foreach (var (file, list) in byFile)
            {
                if (list.Count > 0)
                    await client.SetBreakpointsAsync(file, list, cancellationToken).ConfigureAwait(false);
            }
            attachExceptionBpsOk = await DapShared.TrySetUnhandledExceptionBreakpointsAsync(client).ConfigureAwait(false);
            await client.ConfigurationDoneAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await stoppedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                stoppedTcs.TrySetResult();
            }
        }
        catch
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        _client = client;
        _sessionWorkspacePath = workspacePath;
        _sessionTargetPath = string.IsNullOrWhiteSpace(targetPath)
            ? BreakpointsFileService.GetBundledSampleDebugTargetDllPath(workspacePath)
            : targetPath;
        RefreshBreakpointSnapshotFromStorage(workspacePath, _sessionTargetPath);
        NotifySessionClientChanged();
        await EnsureThreadIdAfterLaunchAsync(client).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("# Debug session started (attach)");
        sb.AppendLine($"# Process ID: {processId}");
        sb.AppendLine($"# Breakpoints: {breakpoints.Count} applied");
        sb.AppendLine(attachExceptionBpsOk
            ? "# Exception breakpoints: unhandled (stop on throw)"
            : "# Exception breakpoints: skipped (adapter rejected setExceptionBreakpoints)");
        if (_lastExceptionText is { } exMsg)
            sb.AppendLine($"# Stopped on exception: {exMsg}");
        return sb.ToString();
    }

    private async Task EnsureThreadIdAfterLaunchAsync(DapClient client)
    {
        if (_lastStoppedThreadId != 0)
            return;
        try
        {
            var threadsBody = await client.ThreadsAsync().ConfigureAwait(false);
            if (threadsBody != null && threadsBody.Value.TryGetProperty("threads", out var threadsArr))
            {
                foreach (var t in threadsArr.EnumerateArray())
                {
                    if (t.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var tid))
                    {
                        _lastStoppedThreadId = tid;
                        NotifySessionClientChanged();
                        break;
                    }
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task RefreshStoppedUiAsync(DapClient client, int threadId)
    {
        try
        {
            var (stackFrames, variableRootScopes, resolvedIdx) =
                await BuildStackAndVariablesAsync(client, threadId, _variablesFrameIndex).ConfigureAwait(false);
            _variablesFrameIndex = resolvedIdx;
            string? file = null;
            var line = 0;
            if (stackFrames.Count > 0)
            {
                file = stackFrames[0].File;
                line = stackFrames[0].Line;
            }
            SetSnapshot(new DebugSessionSnapshot(
                HasActiveSession: true,
                IsExecutionStopped: true,
                StoppedFile: file,
                StoppedLine: line,
                ExceptionText: _lastExceptionText,
                Breakpoints: GetSnapshot().Breakpoints,
                StackFrames: stackFrames,
                VariableRootScopes: variableRootScopes,
                VariablesFrameIndex: resolvedIdx));
        }
        catch
        {
            // UI refresh is best-effort
        }
    }

    /// <summary>Выбрать кадр стека для Locals (панель Mfd) и обновить снимок. Игнор без активной остановки.</summary>
    public async Task SetVariablesFrameIndexAsync(int frameIndex, CancellationToken cancellationToken = default)
    {
        var client = _client;
        if (client == null)
            return;
        var threadId = _lastStoppedThreadId;
        if (threadId == 0)
            return;
        _variablesFrameIndex = frameIndex;
        await RefreshStoppedUiAsync(client, threadId).ConfigureAwait(false);
    }

    /// <summary>Дети переменной по DAP (ленивый expand в панели Locals).</summary>
    public async Task<IReadOnlyList<DebugVariableRow>> ExpandVariableChildrenAsync(
        int variablesReference,
        int? indexedHint,
        int? namedHint,
        CancellationToken cancellationToken = default)
    {
        var client = _client;
        if (client == null || variablesReference == 0)
            return Array.Empty<DebugVariableRow>();
        if (_lastStoppedThreadId == 0)
            return Array.Empty<DebugVariableRow>();

        var body = await DapVariableExpansion.FetchChildVariablesBodyAsync(
            client,
            variablesReference,
            namedHint,
            indexedHint,
            DapVariableExpansion.DefaultMaxChildrenPerNode,
            cancellationToken).ConfigureAwait(false);
        if (body == null || !body.Value.TryGetProperty("variables", out var vars))
            return Array.Empty<DebugVariableRow>();
        return MapTopLevelVariableRoots(vars);
    }

    private static List<DebugVariableRow> MapTopLevelVariableRoots(JsonElement variablesArray)
    {
        var list = new List<DebugVariableRow>();
        foreach (var v in variablesArray.EnumerateArray())
        {
            var d = DapVariableDescriptor.FromVariableJson(v);
            list.Add(
                new DebugVariableRow(
                    d.Name,
                    d.Value,
                    d.Type,
                    d.VariablesReference,
                    d.NamedVariables,
                    d.IndexedVariables));
        }

        return list;
    }

    private static async Task<(
        List<(string Name, string? File, int Line)> Stack,
        List<DebugVariableRootScope> VariableRootScopes,
        int ResolvedFrameIndex)> BuildStackAndVariablesAsync(
        DapClient client,
        int threadId,
        int frameIndex)
    {
        var stackFrames = new List<(string Name, string? File, int Line)>();
        var rootScopes = new List<DebugVariableRootScope>();

        JsonElement? stackBody;
        try
        {
            stackBody = await DapShared.WithRetryAsync(() => client.StackTraceAsync(threadId)).ConfigureAwait(false);
        }
        catch
        {
            return (stackFrames, rootScopes, 0);
        }
        if (stackBody == null || !stackBody.Value.TryGetProperty("stackFrames", out var frames))
            return (stackFrames, rootScopes, 0);

        foreach (var f in frames.EnumerateArray())
        {
            var name = f.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
            var line = f.TryGetProperty("line", out var ln) ? ln.GetInt32() : 0;
            string? path = null;
            if (f.TryGetProperty("source", out var src) && src.TryGetProperty("path", out var p))
                path = p.GetString();
            stackFrames.Add((name, path, line));
        }

        if (stackFrames.Count == 0)
            return (stackFrames, rootScopes, 0);

        var idx = Math.Clamp(frameIndex, 0, stackFrames.Count - 1);
        var frameList = frames.EnumerateArray().ToList();
        var frame = frameList[idx];
        if (!frame.TryGetProperty("id", out var idEl))
            return (stackFrames, rootScopes, idx);
        var frameId = idEl.GetInt32();

        var usedScopes = false;
        try
        {
            var scopesBody = await DapShared.WithRetryAsync(() => client.ScopesAsync(frameId)).ConfigureAwait(false);
            if (scopesBody != null && scopesBody.Value.TryGetProperty("scopes", out var scopesArr))
            {
                foreach (var scope in scopesArr.EnumerateArray())
                {
                    if (!scope.TryGetProperty("variablesReference", out var vrefEl) || !vrefEl.TryGetInt32(out var vref) || vref == 0)
                        continue;
                    var scopeName = scope.TryGetProperty("name", out var sn) ? sn.GetString() : "?";
                    var varsBody = await DapShared.WithRetryAsync(() => client.VariablesAsync(vref)).ConfigureAwait(false);
                    if (varsBody == null || !varsBody.Value.TryGetProperty("variables", out var vars))
                        continue;
                    usedScopes = true;
                    rootScopes.Add(new DebugVariableRootScope(scopeName ?? "?", MapTopLevelVariableRoots(vars)));
                }
            }
        }
        catch
        {
            // fall through
        }

        if (!usedScopes)
        {
            try
            {
                var varsBody = await DapShared.WithRetryAsync(() => client.VariablesAsync(frameId)).ConfigureAwait(false);
                if (varsBody != null && varsBody.Value.TryGetProperty("variables", out var vars))
                    rootScopes.Add(new DebugVariableRootScope("Variables", MapTopLevelVariableRoots(vars)));
            }
            catch
            {
                // ignore
            }
        }

        return (stackFrames, rootScopes, idx);
    }

    /// <summary>Повторно применить брейкпоинты из <see cref="BreakpointsStorage"/> к активной сессии (изменения в JSON / UI во время отладки).</summary>
    public async Task ResyncBreakpointsFromStorageAsync(CancellationToken cancellationToken = default)
    {
        var client = _client;
        var ws = _sessionWorkspacePath;
        if (string.IsNullOrEmpty(ws))
            return;
        var target = _sessionTargetPath;
        if (string.IsNullOrEmpty(target))
            target = BreakpointsFileService.GetBundledSampleDebugTargetDllPath(ws);

        var previousFiles = GetSnapshot().Breakpoints
            .Select(b => b.File)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        RefreshBreakpointSnapshotFromStorage(ws, target);
        if (client == null)
            return;

        var workspaceRoot = GetWorkspaceRootFromPath(ws);
        var breakpoints = BreakpointsStorage.GetBreakpoints(ws, target).ToList();
        var byFile = breakpoints
            .GroupBy(b => DapShared.ResolveBreakpointFilePath(workspaceRoot, b.File))
            .ToDictionary(g => g.Key, g => g.Select(b => (b.Line, b.Condition)).ToList());

        foreach (var file in previousFiles.Union(byFile.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var list = byFile.TryGetValue(file, out var resolved)
                ? resolved
                : [];
            await client.SetBreakpointsAsync(file, list, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> ContinueAsync(CancellationToken cancellationToken = default)
    {
        var (client, threadId) = GetSessionAndThreadId();
        await client.ContinueAsync(threadId, cancellationToken).ConfigureAwait(false);
        return "# Continued execution.";
    }

    public async Task<string> StepOverAsync(CancellationToken cancellationToken = default)
    {
        try { await WaitForStoppedAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch (TimeoutException) { return "# Timeout (5s) waiting for execution to stop."; }
        var (client, threadId) = GetSessionAndThreadId();
        try { await DapShared.WithRetryVoidAsync(() => client.NextAsync(threadId, cancellationToken)).ConfigureAwait(false); }
        catch (InvalidOperationException ex) { return "# " + ex.Message; }
        return "# Step over sent.";
    }

    public async Task<string> StepIntoAsync(CancellationToken cancellationToken = default)
    {
        try { await WaitForStoppedAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch (TimeoutException) { return "# Timeout (5s) waiting for execution to stop."; }
        var (client, threadId) = GetSessionAndThreadId();
        try { await DapShared.WithRetryVoidAsync(() => client.StepInAsync(threadId, cancellationToken)).ConfigureAwait(false); }
        catch (InvalidOperationException ex) { return "# " + ex.Message; }
        return "# Step into sent.";
    }

    public async Task<string> StepOutAsync(CancellationToken cancellationToken = default)
    {
        try { await WaitForStoppedAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch (TimeoutException) { return "# Timeout (5s) waiting for execution to stop."; }
        var (client, threadId) = GetSessionAndThreadId();
        try { await DapShared.WithRetryVoidAsync(() => client.StepOutAsync(threadId, cancellationToken)).ConfigureAwait(false); }
        catch (InvalidOperationException ex) { return "# " + ex.Message; }
        return "# Step out sent.";
    }

    public async Task<string> StopAsync(CancellationToken cancellationToken = default)
    {
        await StopInternalAsync().ConfigureAwait(false);
        return "# Debug session stopped.";
    }

    private async Task StopInternalAsync()
    {
        var client = _client;
        if (client == null)
            return;
        var threadId = _lastStoppedThreadId;
        _client = null;
        _lastStoppedThreadId = 0;
        _lastExceptionText = null;
        _sessionWorkspacePath = null;
        _sessionTargetPath = null;
        _variablesFrameIndex = 0;
        UpdateSnapshot(current => new DebugSessionSnapshot(
            HasActiveSession: false,
            IsExecutionStopped: false,
            StoppedFile: null,
            StoppedLine: 0,
            ExceptionText: null,
            Breakpoints: current.Breakpoints,
            StackFrames: Array.Empty<(string Name, string? File, int Line)>(),
            VariableRootScopes: Array.Empty<DebugVariableRootScope>(),
            VariablesFrameIndex: 0));
        NotifySessionClientChanged();
        if (threadId != 0)
        {
            try { await client.ContinueAsync(threadId).ConfigureAwait(false); }
            catch { /* ignore */ }
        }
        await client.DisposeAsync().ConfigureAwait(false);
    }

    public Task<string> StackTraceFromSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snap = GetSnapshot();
        if (!snap.HasActiveSession)
            return Task.FromResult("# No active debug session.");
        if (!snap.IsExecutionStopped)
            return Task.FromResult("# Execution is not stopped.");
        var sb = new StringBuilder();
        sb.AppendLine("# Stack trace");
        var i = 0;
        foreach (var (name, path, line) in snap.StackFrames)
        {
            sb.AppendLine($"  [{i}] {name} — {path ?? ""}:{line}");
            i++;
        }
        return Task.FromResult(sb.ToString());
    }

    public async Task<string> VariablesAsync(int frameIndex, CancellationToken cancellationToken = default)
    {
        var snap0 = GetSnapshot();
        if (snap0.HasActiveSession && snap0.IsExecutionStopped && frameIndex == snap0.VariablesFrameIndex)
        {
            var snap = snap0;
            var sb = new StringBuilder();
            sb.AppendLine($"# Variables (frame {frameIndex} — снимок IDE, корневой уровень; дети — в UI по expand).");
            foreach (var g in snap.VariableRootScopes)
            {
                sb.AppendLine($"## {g.ScopeName}");
                foreach (var r in g.Roots)
                {
                    var typePart = string.IsNullOrEmpty(r.Type) ? "" : $" :: {r.Type}";
                    var expand = r.VariablesReference != 0 ? " [+]" : "";
                    sb.AppendLine($"{r.Name} = {r.Value}{typePart}{expand}");
                }
            }
            return sb.ToString();
        }

        if (!snap0.HasActiveSession)
            return "# No active debug session.";
        if (!snap0.IsExecutionStopped)
            return "# Execution is not stopped.";

        try { await WaitForStoppedAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch (TimeoutException) { return "# Timeout (5s) waiting for execution to stop."; }
        var (client, threadId) = GetSessionAndThreadId();
        JsonElement? stackBody;
        try
        {
            stackBody = await DapShared.WithRetryAsync(() => client.StackTraceAsync(threadId, 0, 20, cancellationToken)).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return "# " + ex.Message;
        }
        if (stackBody == null || !stackBody.Value.TryGetProperty("stackFrames", out var frames))
            return "# No stack.";
        var frameList = frames.EnumerateArray().ToList();
        if (frameIndex < 0 || frameIndex >= frameList.Count)
            return $"# frame_index {frameIndex} out of range (0..{frameList.Count - 1}).";
        var frame = frameList[frameIndex];
        if (!frame.TryGetProperty("id", out var idEl))
            return "# Frame has no id.";
        var frameId = idEl.GetInt32();
        var sb2 = new StringBuilder();
        sb2.AppendLine($"# Variables (frame {frameIndex})");

        var usedScopes = false;
        try
        {
            var scopesBody = await DapShared.WithRetryAsync(() => client.ScopesAsync(frameId, cancellationToken)).ConfigureAwait(false);
            if (scopesBody != null && scopesBody.Value.TryGetProperty("scopes", out var scopesArr))
            {
                foreach (var scope in scopesArr.EnumerateArray())
                {
                    if (!scope.TryGetProperty("variablesReference", out var vrefEl) || !vrefEl.TryGetInt32(out var vref) || vref == 0)
                        continue;
                    var scopeName = scope.TryGetProperty("name", out var sn) ? sn.GetString() : "?";
                    var varsBody = await DapShared.WithRetryAsync(() => client.VariablesAsync(vref, cancellationToken)).ConfigureAwait(false);
                    if (varsBody == null || !varsBody.Value.TryGetProperty("variables", out var vars))
                        continue;
                    usedScopes = true;
                    sb2.AppendLine($"## {scopeName}");
                    await DapVariableExpansion.AppendExpandedVariablesAsync(
                        client,
                        sb2,
                        vars,
                        indent: "  ",
                        depth: 0,
                        maxDepth: DapVariableExpansion.DefaultMaxDepth,
                        maxChildrenPerNode: DapVariableExpansion.DefaultMaxChildrenPerNode,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // try direct variables
        }

        if (!usedScopes)
        {
            JsonElement? varsBody;
            try
            {
                varsBody = await DapShared.WithRetryAsync(() => client.VariablesAsync(frameId, cancellationToken)).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                return "# " + ex.Message;
            }
            if (varsBody == null || !varsBody.Value.TryGetProperty("variables", out var vars))
                return "# No variables for this frame.";
            await DapVariableExpansion.AppendExpandedVariablesAsync(
                client,
                sb2,
                vars,
                indent: "  ",
                depth: 0,
                maxDepth: DapVariableExpansion.DefaultMaxDepth,
                maxChildrenPerNode: DapVariableExpansion.DefaultMaxChildrenPerNode,
                cancellationToken).ConfigureAwait(false);
        }

        return sb2.ToString();
    }

    public static string Ping() =>
        $"# debug_ping OK at {DateTime.UtcNow:O} (UTC)";
}
