#nullable enable

using System.Text;
using System.Text.Json;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Agent.Harness;

/// <summary>L0 hot + session checkpoint (ADR 0166 P0.1–P0.2 interim product hooks).</summary>
public sealed class ChatHarnessCoordinator
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Func<CascadeIdeSettings> _getSettings;
    private readonly Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? _executeIdeCommand;
    private readonly object _gate = new();

    private Guid _sessionId = Guid.Empty;
    private int _userTurnCount;
    private int _lastCheckpointTurn;
    private int _lastContextPressureAtCount;
    private int _lastUsagePressureAtPct;
    private string? _hotContextBlock;
    private bool _hotContextLoaded;
    private string? _hotContextScope;
    private string? _pendingAgentContext;

    public ChatHarnessCoordinator(
        Func<CascadeIdeSettings> getSettings,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? executeIdeCommand)
    {
        _getSettings = getSettings;
        _executeIdeCommand = executeIdeCommand;
    }

    public string? HotContextBlock
    {
        get
        {
            lock (_gate)
                return _hotContextBlock;
        }
    }

    public void BindSession(Guid sessionId)
    {
        lock (_gate)
        {
            if (_sessionId == sessionId)
                return;

            _sessionId = sessionId;
            _userTurnCount = 0;
            _lastCheckpointTurn = 0;
            _lastContextPressureAtCount = 0;
            _lastUsagePressureAtPct = 0;
            _hotContextBlock = null;
            _hotContextLoaded = false;
            _hotContextScope = null;
            _pendingAgentContext = null;
        }
    }

    public async Task OnSessionInitializedAsync(CancellationToken cancellationToken = default)
    {
        var h = _getSettings().Agent.Harness;
        if (!h.LoadHotContextOnSessionStart)
            return;

        await RefreshHotContextAsync(h.HotContextActiveScope, cancellationToken).ConfigureAwait(false);
    }

    public async Task OnTopicForkedAsync(CancellationToken cancellationToken = default)
    {
        var h = _getSettings().Agent.Harness;
        if (!h.LoadHotContextOnTopicFork)
            return;

        await RefreshHotContextAsync(h.HotContextActiveScope, cancellationToken).ConfigureAwait(false);
    }

    public HarnessUserTurnResult OnUserMessageCommitted()
    {
        var h = _getSettings().Agent.Harness;
        lock (_gate)
            _userTurnCount++;

        if (!h.CheckpointEnabled)
            return HarnessUserTurnResult.None;

        var turn = _userTurnCount;
        var threshold = Math.Max(1, h.CheckpointThresholdUserTurns);
        var repeat = Math.Max(1, h.CheckpointRepeatEveryUserTurns);

        lock (_gate)
        {
            if (turn < threshold)
                return HarnessUserTurnResult.None;

            if (turn == threshold || (turn > threshold && (turn - _lastCheckpointTurn) >= repeat))
            {
                _lastCheckpointTurn = turn;
                QueueAgentContextReminder(turn, "user_turn_threshold");
                return HarnessUserTurnResult.CheckpointPrompt(BuildCheckpointUserMessage(turn));
            }
        }

        return HarnessUserTurnResult.None;
    }

    public HarnessContextPressureResult OnThreadMessageCommitted(int threadMessageCount)
    {
        var h = _getSettings().Agent.Harness;
        if (!h.CheckpointOnContextPressure)
            return HarnessContextPressureResult.None;

        var threshold = Math.Max(1, h.ContextPressureThreadMessageThreshold);
        var repeat = Math.Max(1, h.ContextPressureRepeatEveryMessages);
        var count = Math.Max(0, threadMessageCount);

        lock (_gate)
        {
            if (count < threshold)
                return HarnessContextPressureResult.None;

            if (count == threshold || (count > threshold && (count - _lastContextPressureAtCount) >= repeat))
            {
                _lastContextPressureAtCount = count;
                QueueAgentContextReminder(count, "context_pressure");
                return HarnessContextPressureResult.PreCompactPrompt(BuildPreCompactUserMessage(count));
            }
        }

        return HarnessContextPressureResult.None;
    }

    /// <summary>
    /// FM prompt tokens vs model max — usage-based pressure (Cursor Context Usage parity).
    /// Fires at <see cref="AgentHarnessSettings.ContextWarnPct"/> and every +10pp thereafter.
    /// </summary>
    public HarnessContextPressureResult OnContextUsagePct(int promptTokens, int maxModelLen)
    {
        var h = _getSettings().Agent.Harness;
        if (!h.CheckpointOnContextPressure || maxModelLen <= 0 || promptTokens <= 0)
            return HarnessContextPressureResult.None;

        var pct = (int)Math.Round(100.0 * promptTokens / maxModelLen);
        var warnPct = Math.Clamp(h.ContextWarnPct, 1, 100);
        if (pct < warnPct)
            return HarnessContextPressureResult.None;

        lock (_gate)
        {
            // First fire at warnPct; then every +10 percentage points of context fill.
            if (_lastUsagePressureAtPct == 0)
            {
                if (pct < warnPct)
                    return HarnessContextPressureResult.None;
                _lastUsagePressureAtPct = warnPct;
            }
            else if (pct < _lastUsagePressureAtPct + 10)
            {
                return HarnessContextPressureResult.None;
            }
            else
            {
                _lastUsagePressureAtPct = pct;
            }

            QueueAgentContextReminder(pct, "context_usage_pct");
            return HarnessContextPressureResult.PreCompactPrompt(BuildUsagePressureUserMessage(pct, promptTokens, maxModelLen));
        }
    }

    public string? TryConsumePendingAgentContext()
    {
        lock (_gate)
        {
            var pending = _pendingAgentContext;
            _pendingAgentContext = null;
            return pending;
        }
    }

    public string? BuildTelemetryContextBlock(AgentHarnessTelemetry telemetry, bool verifyEpochUiStale)
    {
        if (!_getSettings().Agent.Harness.InjectHarnessTelemetryInContext)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("<!-- harness telemetry (ide_agent_status parity) -->");
        sb.AppendLine($"session_user_turns: {telemetry.SessionUserTurnCount}");
        sb.AppendLine($"checkpoint_due: {telemetry.CheckpointDue.ToString().ToLowerInvariant()}");
        if (telemetry.NextCheckpointAtTurn is { } next)
            sb.AppendLine($"next_checkpoint_at_turn: {next}");
        sb.AppendLine($"hot_context_loaded: {telemetry.HotContextLoaded.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(telemetry.HotContextScope))
            sb.AppendLine($"hot_context_scope: {telemetry.HotContextScope.Trim()}");
        sb.AppendLine($"verify_epoch_ui_stale: {verifyEpochUiStale.ToString().ToLowerInvariant()}");
        sb.AppendLine("verify_habit: green diagnostics = current verify epoch; call ide_agent_status after edits.");
        return sb.ToString().Trim();
    }

    public AgentHarnessTelemetry GetTelemetry()
    {
        var h = _getSettings().Agent.Harness;
        lock (_gate)
        {
            int? next = null;
            if (h.CheckpointEnabled)
            {
                var threshold = Math.Max(1, h.CheckpointThresholdUserTurns);
                var repeat = Math.Max(1, h.CheckpointRepeatEveryUserTurns);
                if (_userTurnCount < threshold)
                    next = threshold;
                else
                    next = _lastCheckpointTurn + repeat;
            }

            var due = h.CheckpointEnabled
                      && _userTurnCount >= Math.Max(1, h.CheckpointThresholdUserTurns)
                      && (_userTurnCount == _lastCheckpointTurn);

            return new AgentHarnessTelemetry(
                _userTurnCount,
                due,
                next,
                _hotContextLoaded,
                _hotContextScope);
        }
    }

    private async Task RefreshHotContextAsync(string? activeScope, CancellationToken cancellationToken)
    {
        if (_executeIdeCommand is null)
            return;

        IReadOnlyDictionary<string, JsonElement>? args = null;
        if (!string.IsNullOrWhiteSpace(activeScope))
        {
            args = new Dictionary<string, JsonElement>
            {
                ["active_scope"] = JsonSerializer.SerializeToElement(activeScope.Trim(), Json),
            };
        }

        try
        {
            var raw = await _executeIdeCommand(
                IdeCommands.ReadHotContext,
                args,
                cancellationToken).ConfigureAwait(false);

            var block = FormatHotContextBlock(raw, activeScope);
            lock (_gate)
            {
                _hotContextBlock = block;
                _hotContextLoaded = !string.IsNullOrWhiteSpace(block);
                _hotContextScope = activeScope?.Trim();
            }
        }
        catch
        {
            // best-effort; agent may still call read_hot_context manually
        }
    }

    private static string? FormatHotContextBlock(string raw, string? scope)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.TryGetProperty("error", out var err))
                    return null;
                if (doc.RootElement.TryGetProperty("content", out var content))
                {
                    var text = content.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return WrapHotBlock(text, scope);
                }
            }
            catch (JsonException)
            {
                // fall through — treat as plain text
            }
        }

        return WrapHotBlock(trimmed, scope);
    }

    private static string WrapHotBlock(string body, string? scope)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!-- harness L0 hot (read_hot_context in-proc) -->");
        if (!string.IsNullOrWhiteSpace(scope))
            sb.AppendLine($"active_scope: {scope.Trim()}");
        sb.AppendLine(body.Trim());
        return sb.ToString().Trim();
    }

    private void QueueAgentContextReminder(int count, string reason)
    {
        _pendingAgentContext =
            "Session checkpoint (L1 harness · ADCM · agent-memory §9): длинная сессия или context pressure. " +
            "Предложи явно: (1) chat_export_readable, (2) краткое резюме решений/open items, " +
            "(3) согласование с пользователем. Не silent summary. Тактики: Prevent/Partition/Persist/Prune — " +
            $"playbook-agent-driven-context-management-v1.md " +
            $"(harness: {reason}, approx={count})";
    }

    private static string BuildPreCompactUserMessage(int threadMessages) =>
        $"[harness ADCM · context pressure · ~{threadMessages} messages in topic] " +
        "Длинная ветка — бюджет контекста. " +
        "Выбери тактику ADCM: Prevent/Partition/Persist/Prune (не silent rewrite чата). " +
        "По канону часто: export → резюме → согласование. Скажи «подведи итоги с экспортом», если важно сохранить нить.";

    private static string BuildUsagePressureUserMessage(int pct, int promptTokens, int maxModelLen) =>
        $"[harness ADCM · context usage · ~{pct}% prompt {promptTokens}/{maxModelLen}] " +
        "Контекст по токенам заполнен сильно. " +
        "Выбери тактику ADCM (часто Persist/export или Partition/fork). " +
        "Скажи «подведи итоги с экспортом», если важно сохранить нить.";

    private static string BuildCheckpointUserMessage(int turn) =>
        $"[harness checkpoint · ~{turn} user turns] Длинная сессия — прошу подвести итоги: " +
        "chat_export_readable → краткое резюме решений/open items → согласование. " +
        "Ответь «пропустить», если не сейчас.";
}

public readonly record struct HarnessContextPressureResult(bool InjectPreCompact, string? PreCompactUserMessage)
{
    public static HarnessContextPressureResult None => new(false, null);

    public static HarnessContextPressureResult PreCompactPrompt(string message) => new(true, message);
}

public readonly record struct HarnessUserTurnResult(bool InjectCheckpoint, string? CheckpointUserMessage)
{
    public static HarnessUserTurnResult None => new(false, null);

    public static HarnessUserTurnResult CheckpointPrompt(string message) => new(true, message);
}
