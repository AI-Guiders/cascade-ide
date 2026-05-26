using DotNetBuildTest.Core;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Port для build/test jobs AEE (in-proc coordinator или out-of-proc worker).</summary>
public interface IEnvironmentJobBackend
{
    string HostKind { get; }

    BuildTestEnqueueResult TryEnqueue(
        BuildTestJobKind kind,
        string path,
        bool includeRawOutput,
        int timeoutSeconds,
        DotnetExecutionOptions dotnetOptions);

    Task<string?> WaitForCompletionAsync(string jobId, CancellationToken cancellationToken);

    object? GetJobStatus(string jobId);

    object CancelJob(string jobId);
}
