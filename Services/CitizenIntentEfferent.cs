#nullable enable
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CascadeIDE.Services;

/// <summary>
/// Efferent citizen wire for MAF host (peel #13).
/// Transfers crew-callout to organ: parse @intent, map to IDE commands.
/// Same refuse-W discipline as CDP CitizenIntentRouter; no second LLM loop.
/// </summary>
internal static partial class CitizenIntentEfferent
{
    internal sealed record IdeAction(
        string Raw,
        bool Ok,
        string? CommandId = null,
        IReadOnlyDictionary<string, JsonElement>? Args = null,
        string? Reason = null);

    internal static IReadOnlyList<string> ExtractIntentTexts(string? assistantText)
    {
        if (string.IsNullOrWhiteSpace(assistantText))
            return [];

        var list = new List<string>();
        foreach (var rawLine in assistantText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("@intent", StringComparison.OrdinalIgnoreCase))
                continue;
            var body = line.Length > "@intent".Length
                ? line["@intent".Length..].Trim()
                : "";
            if (body.Length > 0)
                list.Add(body);
        }

        return list;
    }

    internal static IdeAction MapToIde(string? intentText)
    {
        var raw = (intentText ?? "").Trim();
        if (raw.Length == 0)
            return new IdeAction(raw, Ok: false, Reason: "empty_intent");

        if (LooksLikeWSpray(raw))
        {
            return new IdeAction(
                raw,
                Ok: false,
                Reason: "refuse_w_spray — seats_detail=full / catalog dump is thrash");
        }

        if (raw.StartsWith("open ", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("open path=", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractPath(raw);
            if (string.IsNullOrWhiteSpace(path))
                return new IdeAction(raw, Ok: false, Reason: "open_path_empty");

            return new IdeAction(
                raw,
                Ok: true,
                CommandId: "open_file",
                Args: new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["path"] = JsonSerializer.SerializeToElement(path),
                });
        }

        if (raw.Equals("drill editor", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("go=editor_scene", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("go=editor", StringComparison.OrdinalIgnoreCase))
        {
            return new IdeAction(raw, Ok: true, CommandId: "get_editor_state", Args: null);
        }

        if (raw.Equals("go=ide_state", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("drill ide", StringComparison.OrdinalIgnoreCase))
        {
            return new IdeAction(raw, Ok: true, CommandId: "get_ide_state", Args: null);
        }

        return new IdeAction(raw, Ok: false, Reason: "unrecognized_intent_for_cide");
    }

    static bool LooksLikeWSpray(string raw) =>
        raw.Contains("seats_detail=full", StringComparison.OrdinalIgnoreCase)
        || raw.Contains("ListTools", StringComparison.OrdinalIgnoreCase)
        || raw.Contains("W-spray", StringComparison.OrdinalIgnoreCase)
        || raw.Contains("wspray", StringComparison.OrdinalIgnoreCase);

    static string? ExtractPath(string raw)
    {
        var m = PathEqualsRegex().Match(raw);
        if (m.Success)
            return m.Groups[1].Value.Trim().Trim('"');

        if (raw.StartsWith("open ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = raw["open ".Length..].Trim();
            if (rest.StartsWith("path=", StringComparison.OrdinalIgnoreCase))
                rest = rest["path=".Length..].Trim();
            return string.IsNullOrWhiteSpace(rest) ? null : rest.Trim('"');
        }

        return null;
    }

    [GeneratedRegex(@"path\s*=\s*(\S.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PathEqualsRegex();
}
