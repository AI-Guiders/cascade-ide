using DotNetBuildTest.Core;

namespace CascadeIDE.Features.Agent.Environment;

public sealed class InProcessEnvironmentJobBackend : IEnvironmentJobBackend
{
    private readonly BuildTestJobCoordinator _coordinator;

    public InProcessEnvironmentJobBackend(BuildTestJobCoordinator coordinator, string hostKind = "supervised-inproc")
    {
        _coordinator = coordinator;
        HostKind = hostKind;
    }

    public string HostKind { get; }

    public BuildTestJobCoordinator Coordinator => _coordinator;

    public BuildTestEnqueueResult TryEnqueue(
        BuildTestJobKind kind,
        string path,
        bool includeRawOutput,
        int timeoutSeconds,
        DotnetExecutionOptions dotnetOptions) =>
        _coordinator.TryEnqueue(kind, path, includeRawOutput, timeoutSeconds, dotnetOptions);

    public Task<string?> WaitForCompletionAsync(string jobId, CancellationToken cancellationToken) =>
        _coordinator.WaitForCompletionAsync(jobId, cancellationToken);

    public object? GetJobStatus(string jobId) => _coordinator.GetJobStatus(jobId);

    public object CancelJob(string jobId) => _coordinator.CancelJob(jobId);
}
