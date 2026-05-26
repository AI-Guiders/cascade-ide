using DotNetBuildTest.Core;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Build/test host port (ADR 0148 §5.2). MLP: in-proc coordinator; future: supervised child process.</summary>
public interface IBuildTestHost
{
    string HostKind { get; }

    BuildTestJobCoordinator Coordinator { get; }

    bool IsHealthy { get; }
}

/// <summary>In-process supervised coordinator (MLP stand-in for separate build host process, ADR 0148 §5.2 W2).</summary>
public sealed class InProcessBuildTestHost : IBuildTestHost
{
    private volatile bool _healthy = true;

    public InProcessBuildTestHost(BuildTestJobCoordinator coordinator)
    {
        Coordinator = coordinator;
    }

    public string HostKind => "supervised-inproc";

    public BuildTestJobCoordinator Coordinator { get; }

    public bool IsHealthy => _healthy;

    /// <summary>Отметить host unhealthy после <see cref="AgentEnvironmentTaskDied"/> (W2).</summary>
    public void MarkUnhealthy() => _healthy = false;

    public void MarkHealthy() => _healthy = true;
}
