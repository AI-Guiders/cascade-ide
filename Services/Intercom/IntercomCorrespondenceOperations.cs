#nullable enable

namespace CascadeIDE.Services.Intercom;

/// <summary>Find по коду в активной ветке (ADR 0137).</summary>
internal static class IntercomCorrespondenceOperations
{
    internal sealed record FindError(string Kind, string Message);

    internal sealed record FindExecutionResult(
        IReadOnlyList<IntercomMessageCodeCorrespondenceProjector.MatchHit>? Hits,
        IReadOnlyList<int>? Ordinals,
        int? SelectedOrdinal,
        int BranchMessageCount,
        FindError? Error)
    {
        public static FindExecutionResult Fail(string kind, string message) =>
            new(null, null, null, 0, new FindError(kind, message));
    }

    internal static FindExecutionResult ExecuteFind(
        IntercomCodeRefQuery query,
        string? workspace,
        bool isOverviewMode,
        int laneMessageCount,
        IReadOnlyList<IntercomMessageCodeCorrespondenceProjector.LaneMessage> lane,
        IReadOnlyList<IntercomMessageRangeRelatedProjector.ExplicitRelate> explicitForThread,
        Func<int, int, string> selectOrdinalRange)
    {
        if (isOverviewMode)
        {
            return FindExecutionResult.Fail(
                "overview_mode",
                "Открой тему (detail): /intercom topic open или клик по карточке.");
        }

        if (laneMessageCount == 0)
            return FindExecutionResult.Fail("empty_lane", "В активной ветке нет сообщений.");

        var entries = IntercomMessageCodeCorrespondenceProjector.BuildCombined(lane, explicitForThread);
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, workspace);
        if (hits.Count == 0)
        {
            return FindExecutionResult.Fail(
                "no_hits",
                "Нет сообщений в ветке, связанных с этим фрагментом кода.");
        }

        var ordinals = hits.Select(h => h.Ordinal).OrderBy(o => o).ToList();
        int? selected = null;
        if (string.Equals(selectOrdinalRange(ordinals[0], ordinals[^1]), "OK", StringComparison.Ordinal))
            selected = ordinals[^1];

        return new FindExecutionResult(hits, ordinals, selected, laneMessageCount, null);
    }

    internal static string FormatFindResult(FindExecutionResult result)
    {
        if (result.Error is { } err)
            return err.Message;

        var ordinals = result.Ordinals!;
        var label = ordinals.Count == 1
            ? $"#{ordinals[0]}"
            : $"#{ordinals[0]}–#{ordinals[^1]}";
        return $"Связанные сообщения: {label} ({result.Hits!.Count}). Активно #{ordinals[^1]}.";
    }
}
