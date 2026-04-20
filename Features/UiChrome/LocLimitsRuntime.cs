using CascadeIDE.Models;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Пороги <c>[loc_limits]</c> из merged <c>workspace.toml</c> (бандл + <c>.cascade/workspace.toml</c>).
/// Low: loc &lt; <see cref="MediumMin"/>; Medium: <see cref="MediumMin"/> &lt;= loc &lt; <see cref="HighMin"/>; High: loc &gt;= <see cref="HighMin"/>.
/// </summary>
public static class LocLimitsRuntime
{
    public const int DefaultMediumMin = 300;
    public const int DefaultHighMin = 800;

    /// <summary>Нижняя граница Medium (непустые строки).</summary>
    public static int MediumMin { get; private set; } = DefaultMediumMin;

    /// <summary>Нижняя граница High (непустые строки).</summary>
    public static int HighMin { get; private set; } = DefaultHighMin;

    internal static void ResetToCodeDefaults()
    {
        MediumMin = DefaultMediumMin;
        HighMin = DefaultHighMin;
    }

    internal static void ApplyWorkspaceToml(UiWorkspaceToml? w)
    {
        ResetToCodeDefaults();
        var limits = w?.LocLimits;
        if (limits is null)
            return;

        var m = limits.MediumMin ?? DefaultMediumMin;
        var h = limits.HighMin ?? DefaultHighMin;
        if (m < 1 || h < 1 || m >= h)
        {
            global::System.Diagnostics.Debug.WriteLine(
                $"LocLimitsRuntime: invalid loc_limits (medium_min={m}, high_min={h}) — using defaults {DefaultMediumMin}/{DefaultHighMin}.");
            return;
        }

        MediumMin = m;
        HighMin = h;
    }

    public static LocSizeTier TierFor(int nonEmptyLineCount)
    {
        if (nonEmptyLineCount < MediumMin)
            return LocSizeTier.Low;
        if (nonEmptyLineCount < HighMin)
            return LocSizeTier.Medium;
        return LocSizeTier.High;
    }
}
