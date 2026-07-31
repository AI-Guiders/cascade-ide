using System.Text;
using System.Text.Json;
using DotnetDebug.Core;

namespace CascadeIDE.Services;

public sealed partial class IdeDapDebugSession
{
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
}

