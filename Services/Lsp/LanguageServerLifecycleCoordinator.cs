#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Services.Lsp;

/// <summary>Общий цикл detach → dispose → start LSP (Wave C).</summary>
public static class LanguageServerLifecycleCoordinator
{
    public static async Task<T?> RestartAsync<T>(
        Func<T> captureSnapshotOnUi,
        Action detachDiagnostics,
        Action disposeHost,
        Func<T, Task<T?>> startHostAsync,
        Action<T?> attachDiagnostics,
        Func<T, Task> reopenDocumentsAsync)
        where T : class
    {
        var snap = captureSnapshotOnUi();
        detachDiagnostics();
        disposeHost();
        var host = await startHostAsync(snap).ConfigureAwait(false);
        attachDiagnostics(host);
        if (host is not null)
            await reopenDocumentsAsync(host).ConfigureAwait(false);
        return host;
    }
}
