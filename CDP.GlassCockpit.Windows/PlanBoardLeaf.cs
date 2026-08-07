#nullable enable

namespace CDP.GlassCockpit.Windows;

/// <summary>Plan leaf-board Face row — instrument card, not Consolas TM dump.</summary>
public sealed class PlanBoardLeaf
{
    public PlanBoardLeaf(
        string mark,
        string title,
        string kind,
        bool isFly,
        bool isOpen,
        bool isDone,
        bool isFeature)
    {
        Mark = mark;
        Title = title;
        Kind = kind;
        IsFly = isFly;
        IsOpen = isOpen;
        IsDone = isDone;
        IsFeature = isFeature;
    }

    public string Mark { get; }
    public string Title { get; }
    public string Kind { get; }
    public bool IsFly { get; }
    public bool IsOpen { get; }
    public bool IsDone { get; }
    public bool IsFeature { get; }

    public string AccentHex =>
        IsFly ? "#4A9EFF"
        : IsDone ? "#5A8F5A"
        : IsFeature ? "#888888"
        : IsOpen ? "#D7A33C"
        : "#666666";

    /// <summary>Parse one latch board line into a Face leaf card.</summary>
    public static PlanBoardLeaf? TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = raw.Trim();
        if (s.StartsWith("|---", StringComparison.Ordinal) || s.StartsWith("|--", StringComparison.Ordinal))
        {
            var cut = s.IndexOf(' ');
            s = cut > 0 ? s[(cut + 1)..].TrimStart() : s.TrimStart('|', '-', ' ');
        }

        s = s.TrimStart('|', ' ');
        if (s.Length == 0)
            return null;

        if (s.StartsWith('*'))
        {
            var feat = LatchPaint.HumanizeBoardLine(s.TrimStart('*').Trim());
            if (string.IsNullOrWhiteSpace(feat))
                return null;
            return new PlanBoardLeaf("COURSE", feat, "feature", false, false, false, true);
        }

        var mark = "OPEN";
        var isFly = false;
        var isOpen = true;
        var isDone = false;
        if (s.StartsWith('[') && s.Length >= 3 && s[2] == ']')
        {
            var code = s[1];
            s = s[3..].TrimStart();
            switch (code)
            {
                case 'x':
                case 'X':
                    mark = "DONE";
                    isDone = true;
                    isOpen = false;
                    break;
                case '>':
                    mark = "FLY";
                    isFly = true;
                    isOpen = true;
                    break;
                case '-':
                case ' ':
                case '.':
                    mark = "OPEN";
                    isOpen = true;
                    break;
                default:
                    mark = code.ToString();
                    isOpen = true;
                    break;
            }
        }

        var title = LatchPaint.HumanizeBoardLine(s);
        if (string.IsNullOrWhiteSpace(title) || title is "—" or "-" or ".")
            return null;

        return new PlanBoardLeaf(mark, title, "leaf", isFly, isOpen, isDone, false);
    }

    public static IReadOnlyList<PlanBoardLeaf> ParseAll(IEnumerable<string>? lines)
    {
        if (lines is null)
            return [];

        var list = new List<PlanBoardLeaf>();
        foreach (var line in lines)
        {
            var leaf = TryParse(line);
            if (leaf is null)
                continue;
            list.Add(leaf);
            if (list.Count >= 48)
                break;
        }

        return list;
    }

    /// <summary>Default Face list: COURSE + FLY + OPEN — hide DONE wall (counts on strip).</summary>
    public static IReadOnlyList<PlanBoardLeaf> FaceRows(
        IReadOnlyList<PlanBoardLeaf> all,
        string? filter)
    {
        filter = string.IsNullOrWhiteSpace(filter) ? "active" : filter.Trim().ToLowerInvariant();
        return filter switch
        {
            "done" => all.Where(x => x.IsDone || x.IsFeature).ToList(),
            "fly" => all.Where(x => x.IsFly || x.IsFeature).ToList(),
            "open" => all.Where(x => (x.IsOpen && !x.IsDone) || x.IsFeature).ToList(),
            "all" => all,
            _ => all.Where(x => x.IsFeature || x.IsFly || (x.IsOpen && !x.IsDone)).ToList(),
        };
    }
}
