#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Parse <c>git status --porcelain=v1</c> lines for Glass Git list host.</summary>
public static class GlassGitPorcelainParse
{
    public sealed record Row(string Xy, string Path, string? OrigPath)
    {
        public string Display => OrigPath is null ? $"{Xy} {Path}" : $"{Xy} {OrigPath} → {Path}";
        public bool IsStaged => Xy.Length > 0 && Xy[0] is not ' ' and not '?';
        public bool IsUnstaged => Xy.Length > 1 && Xy[1] is not ' ';
        /// <summary>List tint bucket — DataTrigger Tone=add|delete|change|untracked|plain.</summary>
        public string Tone => GlassGitStatusTone.Name(Xy);
        public override string ToString() => Display;
    }

    public static IReadOnlyList<Row> Parse(string text)
    {
        var rows = new List<Row>();
        if (string.IsNullOrWhiteSpace(text))
            return rows;

        foreach (var raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.TrimEnd();
            if (line.Length < 4)
                continue;
            var xy = line[..2];
            var rest = line[3..].Trim();
            if (rest.Length == 0)
                continue;

            string path = rest;
            string? orig = null;
            var arrow = rest.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrow >= 0)
            {
                orig = rest[..arrow].Trim().Trim('"');
                path = rest[(arrow + 4)..].Trim().Trim('"');
            }
            else
                path = path.Trim('"');

            rows.Add(new Row(xy, path, orig));
        }

        return rows;
    }
}
