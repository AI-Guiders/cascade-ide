#nullable enable

using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary><c>[intercom] feed_metrics</c> — плотность Skia-ленты (comfortable / compact).</summary>
public partial class MainWindowViewModel
{
    public IReadOnlyList<string> IntercomFeedMetricsOptionsList => IntercomFeedMetricsModes.All;

    public string IntercomFeedMetrics
    {
        get => _settings.Intercom.FeedMetrics;
        set
        {
            var normalized = normalizeIntercomFeedMetrics(value);
            if (string.Equals(_settings.Intercom.FeedMetrics, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            _settings.Intercom.FeedMetrics = normalized;
            OnPropertyChanged();
            ChatPanel.ApplyIntercomPresentationSettings(_settings.Intercom);
            SaveSettingsIfChanged();
        }
    }

    private static string normalizeIntercomFeedMetrics(string? value) =>
        IntercomFeedMetricsModes.IsComfortable(value)
            ? IntercomFeedMetricsModes.Comfortable
            : IntercomFeedMetricsModes.Compact;
}
