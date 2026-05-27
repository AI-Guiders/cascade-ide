namespace CascadeIDE.Models;

/// <summary>AEE policy and runner (ADR 0148 §11). TOML: <c>[agent.environment]</c>.</summary>
public sealed class AgentEnvironmentSettings
{
    public string DefaultVerifyPolicy { get; set; } = "standard";

    public string DefaultSandboxProfile { get; set; } = "agent_ephemeral";

    public int RunnerMaxConcurrency { get; set; } = 2;

    public int CoalesceWindowMs { get; set; } = 1500;

    /// <summary>deny | l3_only | allow_with_audit</summary>
    public string ShellEscapeTier { get; set; } = "deny";

    /// <summary>agent_ephemeral | agent_worktree | in_place — для длинных autonomous run (W6).</summary>
    public string LongRunSandboxProfile { get; set; } = "agent_worktree";

    /// <summary>supervised-inproc | supervised-worker-process | supervised-worker-daemon</summary>
    public string BuildVerifyHost { get; set; } = "supervised-inproc";

    /// <summary>Абсолютный путь к <c>CascadeIDE.BuildVerifyWorker.dll</c>; пусто = авто рядом с exe / repo tools.</summary>
    public string? BuildVerifyWorkerAssemblyPath { get; set; }

    public AgentDevServiceContractSettings DevServices { get; set; } = new();

    public AgentEnvironmentLadderSettings Ladder { get; set; } = new();

    public AgentEnvironmentTimeAccountingSettings TimeAccounting { get; set; } = new();
}

public sealed class AgentEnvironmentLadderSettings
{
    public bool L0Enabled { get; set; } = true;

    public bool L4RequireExplicit { get; set; } = true;

    /// <summary>open_tabs | open_tabs_and_git_dirty_cs — см. <c>AgentL0CsScopeParser</c> (ADR 0148 L0).</summary>
    public string L0CsScope { get; set; } = "open_tabs_and_git_dirty_cs";

    /// <summary>Макс. доп. <c>.cs</c>-файлов из git diff (рабочее дерево + индекс).</summary>
    public int L0GitDirtyMaxFiles { get; set; } = 48;

    /// <summary>Добавлять в L0 пути из solution warm-up (открытые/активный .cs), читая с диска если не во вкладках.</summary>
    public bool L0IncludeWarmupCs { get; set; } = true;

    /// <summary>Лимит доп. warmup-путей; 0 = как <c>solution_warmup.max_open_document_files</c>.</summary>
    public int L0WarmupMaxFiles { get; set; }

    /// <summary>L3: только тесты, затронутые git-dirty <c>.cs</c> (ADR 0148 F).</summary>
    public bool L3TouchedTestsOnly { get; set; } = true;
}

public sealed class AgentEnvironmentTimeAccountingSettings
{
    public bool ShowInChat { get; set; } = true;

    public bool PfdInstrumentEnabled { get; set; } = true;

    /// <summary>Краткие строки build/test в чат (W3).</summary>
    public bool ShowTaskProgressInChat { get; set; } = true;

    /// <summary>0 = выкл; иначе порог без фокуса CIDE для idle_user (W3+).</summary>
    public int IdleUserThresholdMs { get; set; }
}
