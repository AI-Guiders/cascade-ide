using System.Text;
using System.Text.Json;
using DotnetDebug.Core;

namespace CascadeIDE.Services;

/// <summary>Одна активная DAP-сессия (netcoredbg) для IDE: launch/attach, шаги, обновление панели отладки при stopped.</summary>
public sealed class IdeDapDebugSession
{
    private readonly Action<string?, int, IReadOnlyList<(string Name, string? File, int Line)>, IReadOnlyList<(string Name, string Value)>> _onStoppedUi;

    private DapClient? _client;
    private int _lastStoppedThreadId;
    private string? _lastExceptionText;
    private TaskCompletionSource? _stoppedWaitTcs;
    private readonly object _stoppedLock = new();

    public IdeDapDebugSession(
        Action<string?, int, IReadOnlyList<(string Name, string? File, int Line)>, IReadOnlyList<(string Name, string Value)>> onStoppedUi)
    {
        _onStoppedUi = onStoppedUi;
    }

    /// <summary>Есть активный DAP-клиент (launch или attach).</summary>
    public bool HasActiveSession => _client != null;

    /// <summary>Выполнение остановлено (есть threadId от последнего stopped).</summary>
    public bool IsExecutionStopped => _lastStoppedThreadId != 0;

    /// <summary>Смена клиента или stopped/continued — для обновления CanExecute у команд UI.</summary>
    public event EventHandler? StateChanged;

    private void PrepareStoppedWait()
    {
        lock (_stoppedLock)
            _stoppedWaitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void OnStoppedEvent(int threadId, string? exceptionText)
    {
        _lastStoppedThreadId = threadId;
        _lastExceptionText = exceptionText;
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

    /// <summary>Текст для MCP-ответа; UI обновляется в событии stopped.</summary>
    public async Task<string> LaunchAsync(
        string workspacePath,
        string targetPath,
        string? netcoredbgPath,
        IReadOnlyList<string>? programArgs,
        CancellationToken cancellationToken = default)
    {
        var netcoredbg = ResolveNetcoreDbgPath(netcoredbgPath);
        var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
        if (File.Exists(workspaceRoot))
            workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
        var programPath = Path.IsPathRooted(targetPath.Trim())
            ? Path.GetFullPath(targetPath)
            : Path.GetFullPath(Path.Combine(workspaceRoot, targetPath.Trim()));
        if (!File.Exists(programPath))
            throw new ArgumentException($"Target not found: {programPath}");

        var breakpoints = BreakpointsStorage.GetBreakpoints(workspacePath, targetPath).ToList();
        var byFile = breakpoints
            .GroupBy(b => DapShared.ResolveBreakpointFilePath(workspaceRoot, b.File))
            .ToDictionary(g => g.Key, g => g.Select(b => (b.Line, b.Condition)).ToList());

        await StopInternalAsync().ConfigureAwait(false);

        var client = await DapClient.StartAsync(netcoredbg, cancellationToken, clientId: "cascade-ide", clientName: "CascadeIDE").ConfigureAwait(false);
        client.OnConnectionLost = () =>
        {
            if (_client == client)
            {
                _client = null;
                _lastStoppedThreadId = 0;
                _lastExceptionText = null;
                NotifySessionClientChanged();
            }
        };
        PrepareStoppedWait();
        var stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnEvent = (eventName, body) =>
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
                OnContinuedEvent();
        };

        var exceptionBpsOk = false;
        try
        {
            var launchCwd = ResolveLaunchWorkingDirectory(programPath, workspaceRoot, programArgs);
            await client.LaunchAsync(programPath, launchCwd, programArgs, cancellationToken).ConfigureAwait(false);
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
        var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
        if (File.Exists(workspaceRoot))
            workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;

        var breakpoints = new List<BreakpointsStorage.BreakpointEntry>();
        var byFile = new Dictionary<string, List<(int Line, string? Condition)>>();
        if (!string.IsNullOrWhiteSpace(targetPath))
        {
            breakpoints = BreakpointsStorage.GetBreakpoints(workspacePath, targetPath).ToList();
            byFile = breakpoints
                .GroupBy(b => DapShared.ResolveBreakpointFilePath(workspaceRoot, b.File))
                .ToDictionary(g => g.Key, g => g.Select(b => (b.Line, b.Condition)).ToList());
        }

        await StopInternalAsync().ConfigureAwait(false);

        var client = await DapClient.StartAsync(netcoredbg, cancellationToken, clientId: "cascade-ide", clientName: "CascadeIDE").ConfigureAwait(false);
        client.OnConnectionLost = () =>
        {
            if (_client == client)
            {
                _client = null;
                _lastStoppedThreadId = 0;
                _lastExceptionText = null;
                NotifySessionClientChanged();
            }
        };
        PrepareStoppedWait();
        var stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnEvent = (eventName, body) =>
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
                OnContinuedEvent();
        };

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
            var (stackFrames, variables) = await BuildStackAndVariablesAsync(client, threadId, frameIndex: 0).ConfigureAwait(false);
            string? file = null;
            var line = 0;
            if (stackFrames.Count > 0)
            {
                file = stackFrames[0].File;
                line = stackFrames[0].Line;
            }
            _onStoppedUi(file, line, stackFrames, variables);
        }
        catch
        {
            // UI refresh is best-effort
        }
    }

    private static async Task<(List<(string Name, string? File, int Line)> Stack, List<(string Name, string Value)> Vars)> BuildStackAndVariablesAsync(
        DapClient client,
        int threadId,
        int frameIndex)
    {
        var stackFrames = new List<(string Name, string? File, int Line)>();
        var variables = new List<(string Name, string Value)>();

        JsonElement? stackBody;
        try
        {
            stackBody = await DapShared.WithRetryAsync(() => client.StackTraceAsync(threadId)).ConfigureAwait(false);
        }
        catch
        {
            return (stackFrames, variables);
        }
        if (stackBody == null || !stackBody.Value.TryGetProperty("stackFrames", out var frames))
            return (stackFrames, variables);

        foreach (var f in frames.EnumerateArray())
        {
            var name = f.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
            var line = f.TryGetProperty("line", out var ln) ? ln.GetInt32() : 0;
            string? path = null;
            if (f.TryGetProperty("source", out var src) && src.TryGetProperty("path", out var p))
                path = p.GetString();
            stackFrames.Add((name, path, line));
        }

        if (stackFrames.Count == 0 || frameIndex < 0 || frameIndex >= stackFrames.Count)
            return (stackFrames, variables);

        var frameList = frames.EnumerateArray().ToList();
        if (frameIndex >= frameList.Count)
            return (stackFrames, variables);
        var frame = frameList[frameIndex];
        if (!frame.TryGetProperty("id", out var idEl))
            return (stackFrames, variables);
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
                    variables.Add(($"{scopeName}:", ""));
                    foreach (var v in vars.EnumerateArray())
                    {
                        var vn = v.TryGetProperty("name", out var nn) ? nn.GetString() ?? "?" : "?";
                        var vv = v.TryGetProperty("value", out var val) ? val.GetString() ?? "?" : "?";
                        var vt = v.TryGetProperty("type", out var t) ? t.GetString() : null;
                        variables.Add((vn, vv + (vt != null ? $" ({vt})" : "")));
                    }
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
                {
                    foreach (var v in vars.EnumerateArray())
                    {
                        var vn = v.TryGetProperty("name", out var nn) ? nn.GetString() ?? "?" : "?";
                        var vv = v.TryGetProperty("value", out var val) ? val.GetString() ?? "?" : "?";
                        var vt = v.TryGetProperty("type", out var t) ? t.GetString() : null;
                        variables.Add((vn, vv + (vt != null ? $" ({vt})" : "")));
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        return (stackFrames, variables);
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
        NotifySessionClientChanged();
        if (threadId != 0)
        {
            try { await client.ContinueAsync(threadId).ConfigureAwait(false); }
            catch { /* ignore */ }
        }
        await client.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<string> StackTraceAsync(CancellationToken cancellationToken = default)
    {
        try { await WaitForStoppedAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch (TimeoutException) { return "# Timeout (5s) waiting for execution to stop."; }
        var (client, threadId) = GetSessionAndThreadId();
        JsonElement? body;
        try
        {
            body = await DapShared.WithRetryAsync(() => client.StackTraceAsync(threadId, 0, 20, cancellationToken)).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return "# " + ex.Message;
        }
        if (body == null || !body.Value.TryGetProperty("stackFrames", out var frames))
            return "# No stack frames.";
        var sb = new StringBuilder();
        sb.AppendLine("# Stack trace");
        var i = 0;
        foreach (var f in frames.EnumerateArray())
        {
            var name = f.TryGetProperty("name", out var n) ? n.GetString() : "?";
            var line = f.TryGetProperty("line", out var ln) ? ln.GetInt32() : 0;
            var path = "";
            if (f.TryGetProperty("source", out var src) && src.TryGetProperty("path", out var p))
                path = p.GetString() ?? "";
            var id = f.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            sb.AppendLine($"  [{i}] {name} — {path}:{line} (id={id})");
            i++;
        }
        return sb.ToString();
    }

    public async Task<string> VariablesAsync(int frameIndex, CancellationToken cancellationToken = default)
    {
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
        var sb = new StringBuilder();
        sb.AppendLine($"# Variables (frame {frameIndex})");

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
                    sb.AppendLine($"## {scopeName}");
                    foreach (var v in vars.EnumerateArray())
                    {
                        var name = v.TryGetProperty("name", out var n) ? n.GetString() : "?";
                        var value = v.TryGetProperty("value", out var val) ? val.GetString() : "?";
                        var type = v.TryGetProperty("type", out var t) ? t.GetString() : null;
                        sb.AppendLine($"  {name} = {value}" + (type != null ? $" ({type})" : ""));
                    }
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
            foreach (var v in vars.EnumerateArray())
            {
                var name = v.TryGetProperty("name", out var n) ? n.GetString() : "?";
                var value = v.TryGetProperty("value", out var val) ? val.GetString() : "?";
                var type = v.TryGetProperty("type", out var t) ? t.GetString() : null;
                sb.AppendLine($"  {name} = {value}" + (type != null ? $" ({type})" : ""));
            }
        }

        return sb.ToString();
    }

    public static string Ping() =>
        $"# debug_ping OK at {DateTime.UtcNow:O} (UTC)";
}
