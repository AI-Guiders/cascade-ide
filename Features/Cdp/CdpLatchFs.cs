#nullable enable
using Avalonia.Threading;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Avalonia latch apply: settle via <see cref="CdpLatchIo"/>, then marshal onto UI thread.
/// Never <see cref="Thread.Sleep"/> on the UI thread — that freezes glass (Not Responding).
/// </summary>
internal static class CdpLatchFs
{
    /// <summary>Debounce FS noise off-UI, then apply on dispatcher.</summary>
    public static void PostApply(Action apply, int settleMs = CdpLatchIo.DefaultSettleMs)
    {
        ArgumentNullException.ThrowIfNull(apply);
        CdpLatchIo.PostSettled(() => Dispatcher.UIThread.Post(apply), settleMs);
    }
}
