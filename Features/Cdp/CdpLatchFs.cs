#nullable enable
using Avalonia.Threading;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Shared settle for latch FileSystemWatchers.
/// Never <see cref="Thread.Sleep"/> on the UI thread — that freezes glass (Not Responding).
/// </summary>
internal static class CdpLatchFs
{
    /// <summary>Debounce FS noise off-UI, then apply on dispatcher.</summary>
    public static void PostApply(Action apply, int settleMs = 40)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (settleMs > 0)
                    await Task.Delay(settleMs).ConfigureAwait(false);
                Dispatcher.UIThread.Post(apply);
            }
            catch
            {
                /* best-effort projector */
            }
        });
    }
}
