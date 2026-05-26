namespace CascadeIDE.BuildVerifyWorker;

/// <summary>Маркер сборки для тестов и deploy layout.</summary>
public static class BuildVerifyWorkerManifest
{
    public const string Name = "CascadeIDE.BuildVerifyWorker";
}

/// <summary>
/// Verify worker вне CascadeIDE (ADR 0148): one-shot CLI или long-lived <c>serve</c> (JSON-lines IPC).
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length >= 1 && string.Equals(args[0], "serve", StringComparison.OrdinalIgnoreCase))
            return await BuildVerifyWorkerServeLoop.RunAsync().ConfigureAwait(false);

        return await BuildVerifyWorkerOneShot.RunAsync(args).ConfigureAwait(false);
    }
}
