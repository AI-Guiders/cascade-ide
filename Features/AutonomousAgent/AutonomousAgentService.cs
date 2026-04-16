using System.Text.Json;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.AutonomousAgent;

/// <summary>
/// Простейший автономный раннер: модель возвращает JSON-решение (tool_call или final),
/// раннер исполняет IDE/внешние MCP тул-вызовы и повторяет цикл.
///
/// В этом шаге:
/// - IDE инструменты исполняются через <see cref="IIdeMcpActions.ExecuteCommandAsync"/>
/// - внешние MCP инструменты вызываются через <see cref="McpClientService.CallToolAsync"/>
///
/// Важное замечание: в репозитории пока нет полноценного function-calling bridge для провайдеров,
/// поэтому используется “модель -> JSON -> парсинг”.
/// </summary>
public sealed class AutonomousAgentService
{
    private static readonly string[] ToolKindsForTrace = ["PLAN", "ACTION", "OBSERVATION", "NEXT"];

    private readonly AiProviderManager _aiProviderManager;
    private readonly IIdeMcpActions _ideActions;
    private readonly McpClientService _mcpClientService;

    private readonly Func<string> _getActiveAiProvider;
    private readonly Func<string?> _getSelectedOllamaModel;
    private readonly Func<bool> _getUseMinimizedContext;
    private readonly Func<string?> _getCurrentFilePath;
    private readonly Func<string> _getEditorText;

    private readonly Action<string, string, string, DateTimeOffset?> _appendTraceStep;
    private readonly Action<string> _appendEvent;

    public AutonomousAgentService(
        AiProviderManager aiProviderManager,
        IIdeMcpActions ideActions,
        McpClientService mcpClientService,
        Func<string> getActiveAiProvider,
        Func<string?> getSelectedOllamaModel,
        Func<bool> getUseMinimizedContext,
        Func<string?> getCurrentFilePath,
        Func<string> getEditorText,
        Action<string, string, string, DateTimeOffset?> appendTraceStep,
        Action<string> appendEvent)
    {
        _aiProviderManager = aiProviderManager;
        _ideActions = ideActions;
        _mcpClientService = mcpClientService;
        _getActiveAiProvider = getActiveAiProvider;
        _getSelectedOllamaModel = getSelectedOllamaModel;
        _getUseMinimizedContext = getUseMinimizedContext;
        _getCurrentFilePath = getCurrentFilePath;
        _getEditorText = getEditorText;
        _appendTraceStep = appendTraceStep;
        _appendEvent = appendEvent;
    }

    public async Task<string> RunAutonomousAsync(string objective, string safetyLevel, int maxSteps, CancellationToken cancellationToken)
    {
        return await RunAutonomousAsync(objective, safetyLevel, maxSteps, state: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> RunAutonomousAsync(
        string objective,
        string safetyLevel,
        int maxSteps,
        AutonomousRunState? state,
        CancellationToken cancellationToken)
    {
        objective = objective?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(objective))
            return "Error: empty objective.";

        maxSteps = Math.Clamp(maxSteps, 1, 40);

        state ??= new AutonomousRunState
        {
            Objective = objective,
            SafetyLevel = safetyLevel,
            MaxSteps = maxSteps,
            NextStep = 0
        };

        // Ensure state fields are consistent with latest call.
        state.Objective = objective;
        state.SafetyLevel = safetyLevel;
        state.MaxSteps = maxSteps;

        var isResume = state.NextStep > 0;

        if (!isResume)
        {
            state.History.Clear();
            _appendEvent($"{DateTime.Now:HH:mm:ss} — Autonomous started");
            _appendTraceStep("PLAN", objective, "PENDING", null);
        }

        var toolKeys = await SafeListExternalToolsAsync(cancellationToken).ConfigureAwait(false);

        // Initialize history header once.
        if (state.History.Count == 0)
        {
            state.History.Add($"Objective: {objective}");
            state.History.Add($"Safety: {safetyLevel}");
            state.History.Add(
                toolKeys.Count > 0
                    ? $"External MCP tools: {string.Join(", ", toolKeys.Take(40))}"
                    : "External MCP tools: none");
        }

        var startStep = Math.Clamp(state.NextStep, 0, maxSteps);

        for (var step = startStep; step < maxSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.NextStep = step; // if cancellation happens mid-step, resume repeats this step

            var progress = (int)((step * 100.0) / maxSteps);
            _appendEvent($"{DateTime.Now:HH:mm:ss} — Step {step + 1}/{maxSteps} (progress {progress}%)");

            var prompt = BuildPrompt(objective, safetyLevel, step + 1, maxSteps, state.History, toolKeys);
            _appendTraceStep("ACTION", $"Request tool decision (step {step + 1}).", "PENDING", null);

            var assistantRaw = await GetAssistantRawAsync(prompt, cancellationToken).ConfigureAwait(false);
            var decision = TryParseDecision(assistantRaw);

            if (decision.Type == "final")
            {
                _appendTraceStep("OBSERVATION", decision.FinalAnswer ?? assistantRaw, "SUCCESS", null);
                _appendEvent($"{DateTime.Now:HH:mm:ss} — Autonomous finished");
                state.NextStep = maxSteps;
                return decision.FinalAnswer ?? assistantRaw;
            }

            if (decision.Type != "tool_call")
            {
                _appendTraceStep("OBSERVATION", "Model returned invalid decision; stopping.", "WARNING", null);
                state.NextStep = step; // keep this step for retry if user resumes
                return "Autonomous stopped: invalid decision output from model.";
            }

            var observation = await ExecuteToolCallAsync(decision, safetyLevel, cancellationToken).ConfigureAwait(false);
            state.History.Add($"[{step + 1}] {observation}");
            state.NextStep = step + 1;

            _appendTraceStep("NEXT", "Continue.", "PENDING", null);
        }

        state.NextStep = maxSteps;
        return $"Max steps reached ({maxSteps}).";
    }

    private async Task<IReadOnlyList<string>> SafeListExternalToolsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tools = await _mcpClientService.ListToolsAsync(cancellationToken).ConfigureAwait(false);
            return tools.Select(t => t.ToolKey).ToList();
        }
        catch (Exception ex)
        {
            _appendEvent($"{DateTime.Now:HH:mm:ss} — MCP tool list error: {ex.Message}");
            return [];
        }
    }

    private static string BuildPrompt(
        string objective,
        string safetyLevel,
        int stepNumber,
        int maxSteps,
        IReadOnlyList<string> history,
        IReadOnlyList<string> externalToolKeys)
    {
        var ideCommands = new[]
        {
            "get_editor_state",
            "get_workspace_state",
            "get_current_file_diagnostics",
            "build_structured",
            "run_tests",
            "run_affected_tests",
            "run_code_cleanup",
            "get_solution_files",
            "open_file",
            "select",
            "go_to_position",
            "set_breakpoint",
            "remove_breakpoint",
            // destructive actions (будут gating по safety)
            "apply_edit",
            "git_status",
            "git_diff",
            "git_commit",
            "git_push",
            "write_agent_notes",
            "read_agent_notes"
        };

        var externalToolsLine = externalToolKeys.Count == 0
            ? "External tools: none."
            : "External tools: " + string.Join(", ", externalToolKeys.Take(80));

        // Schema is intentionally small to reduce model drift.
        return
$@"You are an autonomous agent inside CascadeIDE.
Your job: {objective}

Safety level: {safetyLevel} (L1/L2/L3).
Rules:
- Return ONLY valid JSON (no markdown).
- Decide ONE action per step: either a tool_call or final.

IDE tools (call via scope=""ide""): allowed command_id values:
{string.Join(", ", ideCommands)}

{externalToolsLine}

History (latest last):
{string.Join("\n", history.TakeLast(12))}

Now step {stepNumber}/{maxSteps}.

Return JSON in this exact shape:
1) Tool call:
{{
  ""type"": ""tool_call"",
  ""scope"": ""ide""|""external"",
  ""ide_command_id"": ""<one of ide command ids>"" (only when scope=""ide""),
  ""external_tool_key"": ""<toolKey from external tools>"" (only when scope=""external""),
  ""args"": {{ /* arbitrary JSON object */ }}
}}
2) Final:
{{
  ""type"": ""final"",
  ""final_answer"": ""<what to do next / summary>""
}}";
    }

    private async Task<string> GetAssistantRawAsync(string prompt, CancellationToken cancellationToken)
    {
        var providerKey = _getActiveAiProvider();
        var modelSentinel = MainWindowViewModel.InstallNewSentinel;
        if (providerKey == "Ollama" && string.IsNullOrWhiteSpace(_getSelectedOllamaModel()) || _getSelectedOllamaModel() == modelSentinel)
            return "Error: Ollama model not configured.";

        var messages = new List<ChatMessage>
        {
            new("user", prompt)
        };

        var sb = new System.Text.StringBuilder(4096);
        await foreach (var token in _aiProviderManager.StreamChatAsync(
                           providerKey,
                           messages,
                           _getCurrentFilePath(),
                           _getEditorText(),
                           _getUseMinimizedContext(),
                           cancellationToken).ConfigureAwait(false))
        {
            sb.Append(token);
        }
        return sb.ToString().Trim();
    }

    private static Decision TryParseDecision(string assistantRaw)
    {
        var json = ExtractFirstJsonObject(assistantRaw);
        if (string.IsNullOrWhiteSpace(json))
            return new Decision(Type: "final", FinalAnswer: assistantRaw);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var te) ? te.GetString() : null;
            if (!string.Equals(type, "tool_call", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, "final", StringComparison.OrdinalIgnoreCase))
                return new Decision(Type: "final", FinalAnswer: assistantRaw);

            if (string.Equals(type, "final", StringComparison.OrdinalIgnoreCase))
            {
                var finalAnswer = root.TryGetProperty("final_answer", out var fe) ? fe.GetString() : null;
                return new Decision(Type: "final", FinalAnswer: finalAnswer);
            }

            var scope = root.TryGetProperty("scope", out var se) ? se.GetString() : null;
            var ideCommandId = root.TryGetProperty("ide_command_id", out var ie) ? ie.GetString() : null;
            var externalToolKey = root.TryGetProperty("external_tool_key", out var ee) ? ee.GetString() : null;
            var argsRaw = root.TryGetProperty("args", out var ae) ? ae.GetRawText() : null;

            if (string.IsNullOrWhiteSpace(scope))
                return new Decision(Type: "final", FinalAnswer: assistantRaw);

            if (string.Equals(scope, "ide", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(ideCommandId))
                return new Decision(Type: "final", FinalAnswer: assistantRaw);

            if (string.Equals(scope, "external", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(externalToolKey))
                return new Decision(Type: "final", FinalAnswer: assistantRaw);

            return new Decision(
                Type: "tool_call",
                Scope: scope,
                IdeCommandId: ideCommandId,
                ExternalToolKey: externalToolKey,
                ArgsRawJson: argsRaw);
        }
        catch
        {
            return new Decision(Type: "final", FinalAnswer: assistantRaw);
        }
    }

    private async Task<string> ExecuteToolCallAsync(Decision decision, string safetyLevel, CancellationToken cancellationToken)
    {
        var scope = decision.Scope ?? "";
        if (string.Equals(scope, "ide", StringComparison.OrdinalIgnoreCase))
        {
            var cmd = decision.IdeCommandId ?? "";
            if (string.IsNullOrWhiteSpace(cmd))
                return "Blocked: missing ide_command_id.";

            if (!IsIdeCommandAllowed(cmd, safetyLevel, out var riskReason))
                return $"Blocked by safety ({safetyLevel}): {riskReason}";

            _appendTraceStep("ACTION", $"ide {cmd}", "PENDING", null);

            // Args are optional; pass only if it is an object.
            IReadOnlyDictionary<string, JsonElement>? argsDict = null;
            if (!string.IsNullOrWhiteSpace(decision.ArgsRawJson))
            {
                using var argsDoc = JsonDocument.Parse(decision.ArgsRawJson);
                var argsEl = argsDoc.RootElement;
                if (argsEl.ValueKind == JsonValueKind.Object)
                    argsDict = argsEl.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            }

            if (IsHighRiskIdeCommand(cmd) && !string.Equals(safetyLevel, "L3", StringComparison.OrdinalIgnoreCase))
            {
                var ok = await _ideActions.RequestConfirmationAsync(
                             $"Autonomous action: {cmd}\nSafety={safetyLevel}\nExecute tool call?",
                             cancellationToken).ConfigureAwait(false);
                if (!string.Equals(ok, ConfirmationResponses.Ok, StringComparison.OrdinalIgnoreCase))
                    return "Cancelled by user confirmation.";
            }

            var text = await _ideActions.ExecuteCommandAsync(cmd, argsDict, cancellationToken).ConfigureAwait(false);
            _appendTraceStep("OBSERVATION", $"{cmd} => {TrimForTrace(text)}", "SUCCESS", null);
            _appendEvent($"{DateTime.Now:HH:mm:ss} — ide call: {cmd}");
            return $"{cmd}: {text}";
        }

        if (string.Equals(scope, "external", StringComparison.OrdinalIgnoreCase))
        {
            var key = decision.ExternalToolKey ?? "";
            if (string.IsNullOrWhiteSpace(key))
                return "Blocked: missing external_tool_key.";

            if (!string.Equals(safetyLevel, "L3", StringComparison.OrdinalIgnoreCase))
            {
                _appendTraceStep("OBSERVATION", $"External tool calls require L3. Blocked: {key}", "WARNING", null);
                return $"Blocked by safety ({safetyLevel}): external tool calls require L3.";
            }

            _appendTraceStep("ACTION", $"external {key}", "PENDING", null);

            IReadOnlyDictionary<string, object?>? argsObj = null;
            if (!string.IsNullOrWhiteSpace(decision.ArgsRawJson))
            {
                using var argsDoc = JsonDocument.Parse(decision.ArgsRawJson);
                var argsEl = argsDoc.RootElement;
                if (argsEl.ValueKind == JsonValueKind.Object)
                    argsObj = argsEl.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value));
            }

            var text = await _mcpClientService.CallToolAsync(key, argsObj, cancellationToken).ConfigureAwait(false);
            _appendTraceStep("OBSERVATION", $"{key} => {TrimForTrace(text)}", "SUCCESS", null);
            _appendEvent($"{DateTime.Now:HH:mm:ss} — external call: {key}");
            return $"{key}: {text}";
        }

        return $"Blocked: unknown scope '{scope}'.";
    }

    private static bool IsIdeCommandAllowed(string cmd, string safetyLevel, out string reason)
    {
        reason = "";
        if (string.Equals(safetyLevel, "L3", StringComparison.OrdinalIgnoreCase))
            return true;

        // L1 is strictly read-only: no file edits and no git commit/push.
        if (string.Equals(safetyLevel, "L1", StringComparison.OrdinalIgnoreCase))
        {
            if (IsHighRiskIdeCommand(cmd))
            {
                reason = "high-risk (edit/git) blocked in L1";
                return false;
            }
        }

        // L2 allows tool calls, but high-risk ones still need confirmation.
        return true;
    }

    private static bool IsHighRiskIdeCommand(string cmd)
    {
        return cmd is "apply_edit"
            or "git_commit"
            or "git_push"
            or "write_agent_notes";
    }

    private static string TrimForTrace(string s, int maxLen = 500)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";
        if (s.Length <= maxLen)
            return s;
        return s[..maxLen] + "…";
    }

    private static object? JsonElementToObject(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.TryGetDouble(out var d) ? d : el.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
            _ => el.GetRawText()
        };
    }

    private static string? ExtractFirstJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = text.IndexOf('{');
        if (start < 0)
            return null;

        var end = text.LastIndexOf('}');
        if (end <= start)
            return null;

        return text.Substring(start, end - start + 1);
    }

    private sealed record Decision(
        string Type,
        string? Scope = null,
        string? IdeCommandId = null,
        string? ExternalToolKey = null,
        string? ArgsRawJson = null,
        string? FinalAnswer = null);
}

