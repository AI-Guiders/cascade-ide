using DotNetBuildTest.Core;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Build/test host port (ADR 0148 §5.2).</summary>
public interface IBuildTestHostHealth
{
    bool IsHealthy { get; }

    void MarkUnhealthy();

    void MarkHealthy();
}

/// <summary>Build/test host port (ADR 0148 §5.2). MLP: in-proc coordinator или out-of-proc worker.</summary>
public interface IBuildTestHost : IBuildTestHostHealth
{
    string HostKind { get; }

    IEnvironmentJobBackend JobBackend { get; }
}

/// <summary>In-process supervised coordinator (MLP stand-in, ADR 0148 §5.2 W2).</summary>
public sealed class InProcessBuildTestHost : IBuildTestHost
{
    private volatile bool _healthy = true;

    public InProcessBuildTestHost(BuildTestJobCoordinator coordinator)
    {
        JobBackend = new InProcessEnvironmentJobBackend(coordinator);
    }

    public string HostKind => JobBackend.HostKind;

    public IEnvironmentJobBackend JobBackend { get; }

    public bool IsHealthy => _healthy;

    public void MarkUnhealthy() => _healthy = false;

    public void MarkHealthy() => _healthy = true;
}

/// <summary>Out-of-process <c>BuildVerifyWorker</c> (ADR 0148 out-of-proc MLP).</summary>
public sealed class WorkerProcessBuildTestHost : IBuildTestHost
{
    private volatile bool _healthy = true;

    public WorkerProcessBuildTestHost(IEnvironmentJobBackend jobBackend)
    {
        JobBackend = jobBackend;
    }

    public string HostKind => JobBackend.HostKind;

    public IEnvironmentJobBackend JobBackend { get; }

    public bool IsHealthy => _healthy;

    public void MarkUnhealthy() => _healthy = false;

    public void MarkHealthy() => _healthy = true;
}

/// <summary>Создаёт <see cref="IBuildTestHost"/> из настроек AEE.</summary>
public static class BuildTestHostFactory
{
    public const string InProcessHostKind = "supervised-inproc";
    public const string WorkerProcessHostKind = "supervised-worker-process";
    public const string WorkerDaemonHostKind = "supervised-worker-daemon";

    public static IBuildTestHost Create(AgentEnvironmentSettings settings, BuildTestJobCoordinator coordinator)
    {
        var host = settings.BuildVerifyHost?.Trim() ?? InProcessHostKind;

        if (string.Equals(host, InProcessHostKind, StringComparison.OrdinalIgnoreCase))
            return new InProcessBuildTestHost(coordinator);

        BuildVerifyWorkerAssemblyLocator.TryResolve(settings.BuildVerifyWorkerAssemblyPath, out var dll);

        if (string.Equals(host, WorkerDaemonHostKind, StringComparison.OrdinalIgnoreCase))
            return new WorkerProcessBuildTestHost(new DaemonBuildVerifyWorkerBackend(dll));

        if (string.Equals(host, WorkerProcessHostKind, StringComparison.OrdinalIgnoreCase))
            return new WorkerProcessBuildTestHost(new OutOfProcessBuildVerifyWorkerBackend(dll));

        return new InProcessBuildTestHost(coordinator);
    }
}
