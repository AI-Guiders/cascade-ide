#nullable enable

using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>SKTypeface/SKFont из списка семейств (comma-separated) с fallback.</summary>
internal static class SkiaChatFeedFontResolver
{
    public static SKTypeface ResolveTypeface(string familyList, SKFontStyle style)
    {
        foreach (var name in SplitFamilies(familyList))
        {
            var tf = SKTypeface.FromFamilyName(name, style);
            if (IsUsable(tf, name))
                return tf;
        }

        return SKTypeface.FromFamilyName("Segoe UI", style) ?? SKTypeface.Default;
    }

    public static SKFont CreateFont(string familyList, float size, SKFontStyle? style = null)
    {
        var font = new SKFont(ResolveTypeface(familyList, style ?? SKFontStyle.Normal), size);
        SkiaKitFonts.ApplyTextQuality(font);
        return font;
    }

    private static IEnumerable<string> SplitFamilies(string familyList)
    {
        if (string.IsNullOrWhiteSpace(familyList))
        {
            yield return "Segoe UI";
            yield break;
        }

        foreach (var part in familyList.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            yield return part;
    }

    private static bool IsUsable(SKTypeface? typeface, string requested)
    {
        if (typeface is null)
            return false;

        var family = typeface.FamilyName ?? "";
        if (family.Length == 0)
            return false;

        return family.Contains(requested, StringComparison.OrdinalIgnoreCase)
               || !string.Equals(family, "sans-serif", StringComparison.OrdinalIgnoreCase);
    }
}
