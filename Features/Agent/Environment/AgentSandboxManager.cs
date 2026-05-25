namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Ephemeral substrate per run (ADR 0148 W4): temp dirs under LocalAppData.</summary>
public sealed class AgentSandboxManager
{
    public string RunsRoot { get; }

    public AgentSandboxManager(string? runsRoot = null)
    {
        RunsRoot = runsRoot
            ?? Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "CascadeIDE",
                "agent-runs");
    }

    public AgentSandboxLease Prepare(string runId, AgentSandboxProfile profile, string? workspaceRoot = null)
    {
        var runDir = Path.Combine(RunsRoot, runId);
        Directory.CreateDirectory(RunsRoot);

        switch (profile)
        {
            case AgentSandboxProfile.AgentEphemeral:
                if (Directory.Exists(runDir))
                    Directory.Delete(runDir, recursive: true);
                Directory.CreateDirectory(runDir);
                Directory.CreateDirectory(Path.Combine(runDir, "temp"));
                Directory.CreateDirectory(Path.Combine(runDir, "substrate"));
                break;

            case AgentSandboxProfile.AgentWorktree:
                Directory.CreateDirectory(runDir);
                break;

            default:
                Directory.CreateDirectory(runDir);
                break;
        }

        return new AgentSandboxLease(runId, profile, runDir, workspaceRoot);
    }

    public void RecreateSubstrateBeforeTests(AgentSandboxLease lease)
    {
        if (lease.Profile != AgentSandboxProfile.AgentEphemeral)
            return;

        var substrate = Path.Combine(lease.RunDirectory, "substrate");
        if (Directory.Exists(substrate))
            Directory.Delete(substrate, recursive: true);
        Directory.CreateDirectory(substrate);
        Directory.CreateDirectory(Path.Combine(lease.RunDirectory, "temp"));
    }
}

public sealed record AgentSandboxLease(
    string RunId,
    AgentSandboxProfile Profile,
    string RunDirectory,
    string? WorkspaceRoot);
