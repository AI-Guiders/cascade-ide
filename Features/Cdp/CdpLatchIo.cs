#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Features.Settings.DataAcquisition;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Toolkit-agnostic latch FS helpers: settle after writer temp+rename, safe text read.
/// Avalonia UI marshalling stays in host <c>CdpLatchFs</c>.
/// </summary>
[IoBoundary]
public static class CdpLatchIo
{
    public const int DefaultSettleMs = 40;

    /// <summary>Run <paramref name="afterSettle"/> off the calling thread after a short settle.</summary>
    public static void PostSettled(Action afterSettle, int settleMs = DefaultSettleMs)
    {
        ArgumentNullException.ThrowIfNull(afterSettle);
        _ = Task.Run(async () =>
        {
            try
            {
                if (settleMs > 0)
                    await Task.Delay(settleMs).ConfigureAwait(false);
                afterSettle();
            }
            catch
            {
                // best-effort latch
            }
        });
    }

    /// <summary>Settle, then invoke only if <paramref name="path"/> still exists.</summary>
    public static void PostSettledIfExists(string path, Action<string> afterSettleIfExists, int settleMs = DefaultSettleMs)
    {
        ArgumentNullException.ThrowIfNull(afterSettleIfExists);
        PostSettled(() =>
        {
            if (File.Exists(path))
                afterSettleIfExists(path);
        }, settleMs);
    }

    public static string? TryReadAllTextIfExists(string path) =>
        TextFileReadWrite.TryReadAllTextIfExists(path);
}
