#nullable enable

using CascadeIDE.SoftInstrument;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Avalonia façade — SoftInstrument density SSOT lives in GlassCore
/// (<see cref="SoftInstrumentChromeDensityPolicy"/>).
/// </summary>
public static class AgentChromeHintDensityPolicy
{
    public const int DefaultMaxVisible = SoftInstrumentChromeDensityPolicy.DefaultMaxVisible;
    public const string CollapseLabel = SoftInstrumentChromeDensityPolicy.CollapseLabel;

    public readonly record struct Hint(string Id, string Text, int Priority);

    public readonly record struct Result(
        IReadOnlyList<string> VisibleLines,
        int HiddenCount,
        string? OverflowLine,
        bool IsExpanded = false)
    {
        public bool HasOverflow => !string.IsNullOrWhiteSpace(OverflowLine);
    }

    public static int PriorityFor(string id) => SoftInstrumentChromeDensityPolicy.PriorityFor(id);

    public static Result Collapse(
        IEnumerable<Hint> hints,
        int maxVisible = DefaultMaxVisible,
        bool expanded = false)
    {
        var mapped = hints.Select(h => new SoftInstrumentChromeDensityPolicy.Hint(h.Id, h.Text, h.Priority));
        var r = SoftInstrumentChromeDensityPolicy.Collapse(mapped, maxVisible, expanded);
        return new Result(r.VisibleLines, r.HiddenCount, r.OverflowLine, r.IsExpanded);
    }

    public static bool ToggleExpanded(
        bool currentlyExpanded,
        int totalHintCount,
        int maxVisible = DefaultMaxVisible) =>
        SoftInstrumentChromeDensityPolicy.ToggleExpanded(currentlyExpanded, totalHintCount, maxVisible);

    public static Hint? From(string id, string? text)
    {
        var h = SoftInstrumentChromeDensityPolicy.From(id, text);
        return h is null ? null : new Hint(h.Value.Id, h.Value.Text, h.Value.Priority);
    }
}
