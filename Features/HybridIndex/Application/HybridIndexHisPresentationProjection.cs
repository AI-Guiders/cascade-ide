using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>Статические строки лампы/сводки HIS (MFD) по событию DataBus без VM.</summary>
[ComputingUnit("hybrid-index-his")]
public static class HybridIndexHisPresentationProjection
{
    public static string LampText(HybridIndexStateChanged? last) =>
        last is null
            ? "NO DATA"
            : string.IsNullOrWhiteSpace(last.LastError)
                ? "OK"
                : "CAUTION";

    public static string StateShort(HybridIndexStateChanged? last) =>
        last is null
            ? "—"
            : string.IsNullOrWhiteSpace(last.LastError)
                ? "IDLE"
                : "ERROR";
}
