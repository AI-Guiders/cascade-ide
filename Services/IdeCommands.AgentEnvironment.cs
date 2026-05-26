namespace CascadeIDE.Services;

/// <summary>AEE native tools (ADR 0148 W6, human–agent parity).</summary>
public static partial class IdeCommands
{
    /// <summary>Verification ladder. args: policy:string, sandbox_profile:string, solution_path:string; returns: json; example: {"policy":"standard"}.</summary>
    public const string IdeAgentVerify = "ide_agent_verify";
    /// <summary>Cancel active verify. returns: json.</summary>
    public const string IdeAgentCancel = "ide_agent_cancel";
    /// <summary>AEE status snapshot: active, run_id, verify_snapshot_id, policy, sandbox_profile, sandbox_run_directory, execution_channel (supervised build host kind), writes_invalidated_verify_epoch. returns: json.</summary>
    public const string IdeAgentStatus = "ide_agent_status";
    /// <summary>Last verify run summary. returns: json.</summary>
    public const string IdeAgentLast = "ide_agent_last";
    /// <summary>Prepare sandbox. args: profile:string, workspace_root:string; returns: json; example: {"profile":"agent_ephemeral"}.</summary>
    public const string IdeAgentSandboxPrepare = "ide_agent_sandbox_prepare";
}
