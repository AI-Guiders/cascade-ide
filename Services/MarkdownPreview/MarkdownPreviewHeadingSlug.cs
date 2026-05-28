#nullable enable

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>GitHub-подобный slug для заголовков и fragment-навигации.</summary>
public static partial class MarkdownPreviewHeadingSlug
{
    private static readonly Dictionary<string, int> SlugCounts = new(StringComparer.OrdinalIgnoreCase);

    public static string Create(string headingText)
    {
        var baseSlug = Normalize(headingText);
        if (baseSlug.Length == 0)
            baseSlug = "section";

        if (!SlugCounts.TryGetValue(baseSlug, out var count))
        {
            SlugCounts[baseSlug] = 1;
            return baseSlug;
        }

        count++;
        SlugCounts[baseSlug] = count;
        return $"{baseSlug}-{count}";
    }

    public static void ResetSlugCounts() => SlugCounts.Clear();

    private static string Normalize(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var ch in normalized)
        {
            if (char.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasDash = false;
                continue;
            }

            if (lastWasDash)
                continue;

            sb.Append('-');
            lastWasDash = true;
        }

        return sb.ToString().Trim('-');
    }

    [GeneratedRegex(@"<a\s+[^>]*\bid\s*=\s*[""'](?<id>[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlAnchorIdRegex();

    public static IEnumerable<string> ExtractHtmlAnchorIds(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            yield break;

        foreach (Match match in HtmlAnchorIdRegex().Matches(html))
        {
            var id = match.Groups["id"].Value.Trim();
            if (id.Length > 0)
                yield return id;
        }
    }
}
