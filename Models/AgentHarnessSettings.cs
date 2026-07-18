namespace CascadeIDE.Models;

/// <summary>Agent-centric harness (ADR 0166). TOML: <c>[agent.harness]</c>.</summary>
public sealed class AgentHarnessSettings
{
    /// <summary>После загрузки Intercom-сессии вызвать <c>read_hot_context</c> in-proc (P0.1 interim).</summary>
    public bool LoadHotContextOnSessionStart { get; set; } = true;

    /// <summary>После <c>/topic create</c> / fork — обновить hot block для ветки.</summary>
    public bool LoadHotContextOnTopicFork { get; set; } = true;

    /// <summary>Опциональный <c>active_scope</c> для read_hot_context.</summary>
    public string? HotContextActiveScope { get; set; }

    /// <summary>Видимый checkpoint в ленте (P0.2 parity с Cursor stop hook).</summary>
    public bool CheckpointEnabled { get; set; } = true;

    public int CheckpointThresholdUserTurns { get; set; } = 40;

    public int CheckpointRepeatEveryUserTurns { get; set; } = 40;

    /// <summary>Coalesced <c>ide_agent_verify</c> после записи <c>.cs</c> (P0.4).</summary>
    public bool AutoVerifyAfterCsWrite { get; set; } = true;

    /// <summary>До loopback MCP (0082): не поднимать второй CascadeIDE для Cursor ACP — MAF/cloud используют in-proc.</summary>
    public bool SuppressAcpIdeStdioInject { get; set; } = true;

    /// <summary>Длинная ветка темы — видимый ADCM pressure inject (P0.2).</summary>
    public bool CheckpointOnContextPressure { get; set; } = true;

    /// <summary>Сообщений в активной теме до ADCM context-pressure предупреждения.</summary>
    public int ContextPressureThreadMessageThreshold { get; set; } = 60;

    public int ContextPressureRepeatEveryMessages { get; set; } = 30;

    /// <summary>Блок harness telemetry в minimized/MAF context (P2.3).</summary>
    public bool InjectHarnessTelemetryInContext { get; set; } = true;

    /// <summary>Порог предупреждения: prompt tokens хода ≥ N% <c>max_model_len</c> (FM catalog).</summary>
    public int ContextWarnPct { get; set; } = 75;

    /// <summary>После <c>/topic create</c> — шаблон brief в поле ввода (P1.2).</summary>
    public bool InjectTopicForkBrief { get; set; } = true;

    /// <summary>Пусто = встроенный 3-line brief.</summary>
    public string? TopicForkBriefTemplate { get; set; }
}
