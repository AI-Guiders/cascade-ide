#nullable enable

using CascadeIDE.SoftOrgan;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Avalonia façade — SoftOrgan density SSOT lives in GlassCore
/// (<see cref="SoftOrganChromeDensityPolicy"/>).
/// </summary>
public static class AgentChromeHintDensityPolicy
{
    public const int DefaultMaxVisible = SoftOrganChromeDensityPolicy.DefaultMaxVisible;
    public const string CollapseLabel = SoftOrganChromeDensityPolicy.CollapseLabel;

    public readonly record struct Hint(string Id, string Text, int Priority);

    public readonly record struct Result(
        IReadOnlyList<string> VisibleLines,
        int HiddenCount,
        string? OverflowLine,
        bool IsExpanded = false)
    {
        public bool HasOverflow => !string.IsNullOrWhiteSpace(OverflowLine);
    }

    public static int PriorityFor(string id) => SoftOrganChromeDensityPolicy.PriorityFor(id);

    public static Result Collapse(
        IEnumerable<Hint> hints,
        int maxVisible = DefaultMaxVisible,
        bool expanded = false)
    {
        var mapped = hints.Select(h => new SoftOrganChromeDensityPolicy.Hint(h.Id, h.Text, h.Priority));
        var r = SoftOrganChromeDensityPolicy.Collapse(mapped, maxVisible, expanded);
        return new Result(r.VisibleLines, r.HiddenCount, r.OverflowLine, r.IsExpanded);
    }

    public static bool ToggleExpanded(
        bool currentlyExpanded,
        int totalHintCount,
        int maxVisible = DefaultMaxVisible) =>
        SoftOrganChromeDensityPolicy.ToggleExpanded(currentlyExpanded, totalHintCount, maxVisible);

    public static Hint? From(string id, string? text)
    {
        var h = SoftOrganChromeDensityPolicy.From(id, text);
        return h is null ? null : new Hint(h.Value.Id, h.Value.Text, h.Value.Priority);
    }
}
