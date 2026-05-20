#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Парсер multi-segment хвостов <c>[a;b] [c;d]</c> (ADR 0138) с legacy contiguous <c>3:5</c>, <c>3 5</c>, <c>5</c>.
/// </summary>
public static class ParametricSegmentListParser
{
    public static bool TryParse(
        string? argsTail,
        out IReadOnlyList<ParametricIntRange> segments,
        out string error)
    {
        segments = [];
        error = "";

        var tail = (argsTail ?? "").Trim();
        if (tail.Length == 0)
        {
            error = "Укажи диапазон: «5», «5 10», «5:10» или «[3;5] [8;15] [20]».";
            return false;
        }

        if (tail.Contains('['))
            return TryParseBracketSegments(tail, out segments, out error);

        if (!ChatSlashParametricArgsBuilder.TryParseLineRangeTail(tail, out var start, out var end, out error))
            return false;

        segments = [new ParametricIntRange(start, end)];
        return true;
    }

    public static bool TryParseSingleContiguous(
        string? argsTail,
        out ParametricIntRange range,
        out string error)
    {
        range = default;
        if (!TryParse(argsTail, out var segments, out error))
            return false;

        if (segments.Count != 1)
        {
            error = segments.Count == 0
                ? "Пустой диапазон."
                : "Ожидается один contiguous сегмент (например 3:5). Для disjoint используйте [3;5] [8;15].";
            return false;
        }

        range = segments[0];
        return true;
    }

    public static string FormatSummary(IReadOnlyList<ParametricIntRange> segments, string unitLabel)
    {
        if (segments.Count == 0)
            return "";

        var parts = new List<string>(segments.Count);
        var total = 0;
        foreach (var segment in segments)
        {
            total += segment.InclusiveCount;
            parts.Add(segment.Start == segment.End
                ? $"{segment.Start}"
                : $"{segment.Start}–{segment.End}");
        }

        var joined = string.Join(", ", parts);
        return segments.Count == 1 && segments[0].Start == segments[0].End
            ? $"{unitLabel}: {joined}"
            : $"{unitLabel}: {joined} ({total} {PluralUnit(total, unitLabel)})";
    }

    private static string PluralUnit(int count, string unitLabel)
    {
        var key = unitLabel.Trim().ToLowerInvariant();
        return key switch
        {
            "строки" or "строка" => count == 1 ? "строка" : "строк",
            "сообщения" or "сообщение" => count == 1 ? "сообщение" : "сообщений",
            _ => count == 1 ? unitLabel.TrimEnd('и') : unitLabel,
        };
    }

    private static bool TryParseBracketSegments(
        string tail,
        out IReadOnlyList<ParametricIntRange> segments,
        out string error)
    {
        var list = new List<ParametricIntRange>();
        var i = 0;
        while (i < tail.Length)
        {
            while (i < tail.Length && char.IsWhiteSpace(tail[i]))
                i++;

            if (i >= tail.Length)
                break;

            if (tail[i] != '[')
            {
                error = "Сегменты disjoint задаются только в скобках: [3;5] [8;15]. Legacy «3:5» — без скобок.";
                segments = [];
                return false;
            }

            var close = tail.IndexOf(']', i + 1);
            if (close < 0)
            {
                error = "Незакрытая «[» в сегменте.";
                segments = [];
                return false;
            }

            var inner = tail[(i + 1)..close].Trim();
            if (!TryParseBracketInner(inner, out var range, out error))
            {
                segments = [];
                return false;
            }

            list.Add(range);
            i = close + 1;
        }

        if (list.Count == 0)
        {
            error = "Нет сегментов в скобках.";
            segments = [];
            return false;
        }

        segments = list;
        error = "";
        return true;
    }

    private static bool TryParseBracketInner(string inner, out ParametricIntRange range, out string error)
    {
        range = default;
        error = "";

        if (inner.Length == 0)
        {
            error = "Пустой сегмент [].";
            return false;
        }

        var parts = inner.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
        {
            error = "Внутри [] ожидается одно число [20] или пара [3;5].";
            return false;
        }

        if (!int.TryParse(parts[0], out var start) || start < 1)
        {
            error = $"Некорректное число «{parts[0]}».";
            return false;
        }

        if (parts.Length == 1)
        {
            range = new ParametricIntRange(start, start);
            return true;
        }

        if (!int.TryParse(parts[1], out var end) || end < 1)
        {
            error = $"Некорректное число «{parts[1]}».";
            return false;
        }

        if (end < start)
        {
            error = "Конец сегмента не может быть меньше начала.";
            return false;
        }

        range = new ParametricIntRange(start, end);
        return true;
    }
}
