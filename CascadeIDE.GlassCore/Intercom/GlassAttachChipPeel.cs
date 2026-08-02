#nullable enable

using System.Text.Json;
using System.Text.RegularExpressions;

namespace CascadeIDE.Intercom;

/// <summary>Thin ADR 0128 peel for Glass journal: chip label + file/lines (not full Avalonia Skia).</summary>
public sealed record GlassAttachChip(
    string Label,
    string File,
    int? LineStart = null,
    int? LineEnd = null)
{
    public string Bracket => GlassAttachChipPeel.FormatBracket(File, LineStart, LineEnd);
}

/// <summary>Peel attach chips from journal <c>attachments</c> JSON and/or body <c>[path:line]</c> markers.</summary>
public static partial class GlassAttachChipPeel
{
    public static string FormatBracket(string file, int? lineStart, int? lineEnd = null)
    {
        var path = (file ?? "").Trim().Replace('\\', '/');
        if (path.Length == 0)
            return "[]";

        if (lineStart is int a && a > 0)
        {
            if (lineEnd is int b && b > a)
                return $"[{path}:{a}-{b}]";
            return $"[{path}:{a}]";
        }

        return $"[{path}]";
    }

    public static IReadOnlyList<GlassAttachChip> Peel(
        string? body,
        JsonElement? attachmentsArray = null)
    {
        var list = new List<GlassAttachChip>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (attachmentsArray is { ValueKind: JsonValueKind.Array } arr)
        {
            foreach (var el in arr.EnumerateArray())
            {
                if (TryFromJsonObject(el, out var chip) && seen.Add(chip.Bracket))
                    list.Add(chip);
            }
        }

        foreach (var chip in FromBody(body))
        {
            if (seen.Add(chip.Bracket))
                list.Add(chip);
        }

        return list;
    }

    public static IReadOnlyList<GlassAttachChip> FromBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return [];

        var list = new List<GlassAttachChip>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in BodyBracket().Matches(body))
        {
            if (!TryParseBracketInner(m.Groups[1].Value, out var chip))
                continue;
            if (!seen.Add(chip.Bracket))
                continue;
            list.Add(chip);
        }

        return list;
    }

    public static bool TryFromJsonObject(JsonElement root, out GlassAttachChip chip)
    {
        chip = null!;
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        var file = Prop(root, "file")
                   ?? Prop(root, "path");
        if (string.IsNullOrWhiteSpace(file))
            return false;

        var label = Prop(root, "display_label")
                    ?? Prop(root, "displayLabel")
                    ?? Prop(root, "label");
        var lineStart = PropInt(root, "line_start") ?? PropInt(root, "lineStart");
        var lineEnd = PropInt(root, "line_end") ?? PropInt(root, "lineEnd");

        chip = Make(file!, label, lineStart, lineEnd);
        return true;
    }

    public static bool TryParseBracketInner(string inner, out GlassAttachChip chip)
    {
        chip = null!;
        var raw = (inner ?? "").Trim().Trim('"');
        if (raw.Length == 0)
            return false;

        // Skip member-only markers [M:Foo] — no file to open on Glass thin peel.
        if (raw.StartsWith("M:", StringComparison.OrdinalIgnoreCase))
            return false;

        int? lineStart = null;
        int? lineEnd = null;
        var path = raw;

        var colon = raw.LastIndexOf(':');
        if (colon > 1
            && colon < raw.Length - 1
            && !raw[(colon + 1)..].Contains('\\')
            && !raw[(colon + 1)..].Contains('/'))
        {
            var linePart = raw[(colon + 1)..];
            var dash = linePart.IndexOf('-');
            if (dash > 0
                && int.TryParse(linePart[..dash], out var a)
                && int.TryParse(linePart[(dash + 1)..], out var b)
                && a > 0
                && b >= a)
            {
                path = raw[..colon];
                lineStart = a;
                lineEnd = b;
            }
            else if (int.TryParse(linePart, out var ln) && ln > 0)
            {
                path = raw[..colon];
                lineStart = ln;
            }
        }

        if (path.Length == 0)
            return false;

        // Require a path-ish token (extension or separator) to avoid prose false positives.
        if (!LooksLikePath(path))
            return false;

        chip = Make(path, label: null, lineStart, lineEnd);
        return true;
    }

    static GlassAttachChip Make(string file, string? label, int? lineStart, int? lineEnd)
    {
        var leaf = file.Replace('\\', '/');
        var slash = leaf.LastIndexOf('/');
        if (slash >= 0 && slash < leaf.Length - 1)
            leaf = leaf[(slash + 1)..];

        var auto = lineStart is int a
            ? lineEnd is int b && b > a ? $"{leaf}:{a}-{b}" : $"{leaf}:{a}"
            : leaf;

        return new GlassAttachChip(
            string.IsNullOrWhiteSpace(label) ? auto : label.Trim(),
            file.Trim(),
            lineStart,
            lineEnd);
    }

    static bool LooksLikePath(string path)
    {
        if (path.Contains('/') || path.Contains('\\'))
            return true;
        var dot = path.LastIndexOf('.');
        return dot > 0 && dot < path.Length - 1;
    }

    static string? Prop(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    static int? PropInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Number)
            return null;
        return el.TryGetInt32(out var v) ? v : null;
    }

    // [path.ext] · [path.ext:12] · [path/file.cs:12-20] — not [M:Member]
    [GeneratedRegex(@"\[([^\[\]]+)\]", RegexOptions.CultureInvariant)]
    private static partial Regex BodyBracket();
}
