using CascadeIDE.Contracts;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>Сжатие длинных путей/строк для строк HIS (как ECAM).</summary>
[PresentationProjection("presentation-his-shorten")]
public static class HybridIndexHisPathDisplayShortener
{
    public static string ShortenLikeEcam(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "—")
            return "—";

        var s = text.Trim();
        try
        {
            if (s.IndexOf(Path.DirectorySeparatorChar) >= 0 || s.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                var trimmed = s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var name = Path.GetFileName(trimmed);
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
        }
        catch
        {
            // ignore
        }

        const int max = 34;
        if (s.Length <= max)
            return s;
        return s[..(max - 1)] + "…";
    }
}
