#nullable enable

using System.Text.Json;
using System.Text.RegularExpressions;

namespace CascadeIDE.Intercom;

/// <summary>Thin ADR 0128 peel for Glass journal: chip label + file/lines (not full Avalonia Skia).</summary>
public sealed record GlassAttachChip(
    string Label,
    string File,
    int? LineStart = null,
    int? LineEnd = null,
    bool Resolved = true)
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

    /// <summary>Disk resolve for feed chip chrome (Avalonia IntercomAttachLinkVisualStatus thin).</summary>
    public static GlassAttachChip ResolveAgainstDisk(GlassAttachChip chip, string? workspaceRoot)
    {
        var path = ResolvePath(chip.File, workspaceRoot);
        var ok = path.Length > 0 && File.Exists(path);
        return chip with { Resolved = ok };
    }

    public static IReadOnlyList<GlassAttachChip> ResolveAgainstDisk(
        IReadOnlyList<GlassAttachChip> chips,
        string? workspaceRoot)
    {
        if (chips.Count == 0)
            return chips;
        var list = new List<GlassAttachChip>(chips.Count);
        foreach (var c in chips)
            list.Add(ResolveAgainstDisk(c, workspaceRoot));
        return list;
    }

    public static string ResolvePath(string file, string? workspaceRoot)
    {
        var path = (file ?? "").Trim();
        if (path.Length == 0)
            return "";
        if (Path.IsPathRooted(path))
            return path;
        if (!string.IsNullOrWhiteSpace(workspaceRoot))
            return Path.Combine(workspaceRoot, path);
        return path;
    }

    /// <summary>Drop peeled <c>[path:line]</c> markers from bubble prose when chips render below.</summary>
    public static string StripBracketsForDisplay(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body ?? "";

        var stripped = BodyBracket().Replace(body, m =>
        {
            if (!TryParseBracketInner(m.Groups[1].Value, out _))
                return m.Value;
            return "";
        });

        stripped = Regex.Replace(stripped, "[ \\t]{2,}", " ");
        stripped = Regex.Replace(stripped, " *\\n *", "\n");
        return stripped.Trim();
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
        // Telegram Desktop paste: [dd.MM.yyyy HH:mm] must stay prose (not red missing-file chips).
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

    /// <summary>
    /// Path-ish bracket inner (extension or separator).
    /// Rejects Telegram timestamps like <c>05.08.2026 14</c> (space, no slash)
    /// and digit-only "extensions" like <c>.2026</c>.
    /// </summary>
    internal static bool LooksLikePath(string path)
    {
        var hasSep = path.Contains('/') || path.Contains('\\');
        if (!hasSep && path.Contains(' '))
            return false;

        if (hasSep)
            return true;

        var dot = path.LastIndexOf('.');
        if (dot <= 0 || dot >= path.Length - 1)
            return false;

        var ext = path[(dot + 1)..];
        if (ext.Length == 0 || ext.All(char.IsDigit))
            return false;

        return true;
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
