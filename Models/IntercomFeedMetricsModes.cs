namespace CascadeIDE.Models;

/// <summary>Значения <see cref="IntercomSettings.FeedMetrics"/> (TOML <c>[intercom] feed_metrics</c>).</summary>
public static class IntercomFeedMetricsModes
{
    public const string Comfortable = "comfortable";
    public const string Compact = "compact";

    public static readonly IReadOnlyList<string> All = [Comfortable, Compact];

    public static bool IsComfortable(string? value) =>
        !string.Equals(value?.Trim(), Compact, StringComparison.OrdinalIgnoreCase);
}
