#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>ignite-wake-LATEST.json → StatusText + SoftOrgan tip (Autoi wake consumer).</summary>
internal static partial class LatchPaint
{
    public const string IgniteWakeSchema = "ignite_wake_latch/v0";

    public sealed record IgniteWakeView(
        string ArmId,
        string Channel,
        string Charge,
        string? Reason,
        string? Task,
        string StatusLine,
        string ChromeHint);

    /// <summary>Null when schema gate fails or JSON is unreadable.</summary>
    public static IgniteWakeView? PaintIgniteWake(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, IgniteWakeSchema, StringComparison.OrdinalIgnoreCase))
                return null;

            var armId = Prop(root, "arm_id")?.Trim() ?? "";
            var channel = Prop(root, "channel")?.Trim() ?? "?";
            var charge = Prop(root, "charge")?.Trim() ?? "";
            if (armId.Length == 0 || charge.Length == 0)
                return null;

            var reason = Prop(root, "reason");
            var task = Prop(root, "task");
            var reasonBit = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            var status = string.IsNullOrWhiteSpace(reasonBit)
                ? $"wake · {channel} · {armId}"
                : $"wake · {channel} · {reasonBit} · {armId}";
            var tip = string.IsNullOrWhiteSpace(reasonBit)
                ? $"wake·{channel}"
                : $"wake·{channel}·{reasonBit}";

            return new IgniteWakeView(
                armId,
                channel,
                charge,
                reasonBit,
                string.IsNullOrWhiteSpace(task) ? null : task.Trim(),
                status,
                tip);
        }
        catch
        {
            return null;
        }
    }
}
