#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Services;

/// <summary>Слияние именованного пресета из JSON настроек с аргументами MCP.</summary>
public static class CodeNavigationPresetMerge
{
    private sealed class PresetDto
    {
        [JsonPropertyName("include_kinds")]
        public List<string>? IncludeKinds { get; set; }

        [JsonPropertyName("exclude_kinds")]
        public List<string>? ExcludeKinds { get; set; }
    }

    /// <summary>
    /// <paramref name="requestInclude"/> / <paramref name="requestExclude"/> с не-<c>null</c> перезаписывают соответствующую сторону пресета;
    /// для exclude пресет и запрос объединяются, если оба заданы (дедуп по канону).
    /// </summary>
    public static (IReadOnlyList<string>? Include, IReadOnlyList<string>? Exclude, string? Error) Merge(
        string? presetName,
        string presetsJson,
        IReadOnlyList<string>? requestInclude,
        IReadOnlyList<string>? requestExclude)
    {
        IReadOnlyList<string>? pInc = null;
        IReadOnlyList<string>? pExc = null;
        if (!string.IsNullOrWhiteSpace(presetName))
        {
            if (!TryGetPreset(presetsJson, presetName.Trim(), out pInc, out pExc, out var err))
                return (null, null, err);
        }

        var inc = requestInclude ?? pInc;

        if (requestExclude is not null && requestExclude.Count > 0)
        {
            if (pExc is { Count: > 0 })
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var x in pExc)
                {
                    var c = CodeNavigationRelatedKinds.TryCanonicalKind(x);
                    if (c is not null)
                        set.Add(c);
                }

                foreach (var x in requestExclude)
                {
                    var c = CodeNavigationRelatedKinds.TryCanonicalKind(x);
                    if (c is not null)
                        set.Add(c);
                }

                return (inc, set.OrderBy(x => x, StringComparer.Ordinal).ToList(), null);
            }

            return (inc, requestExclude, null);
        }

        return (inc, pExc ?? Array.Empty<string>(), null);
    }

    private static bool TryGetPreset(
        string presetsJson,
        string key,
        out IReadOnlyList<string>? includeKinds,
        out IReadOnlyList<string>? excludeKinds,
        out string? error)
    {
        includeKinds = null;
        excludeKinds = null;
        error = null;
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, PresetDto>>(presetsJson.Trim());
            if (dict is null || !dict.TryGetValue(key, out var dto) || dto is null)
            {
                error = $"Неизвестный пресет «{key}»";
                return false;
            }

            if (dto.IncludeKinds is { Count: > 0 })
            {
                var list = new List<string>();
                foreach (var t in dto.IncludeKinds)
                {
                    var c = CodeNavigationRelatedKinds.TryCanonicalKind(t);
                    if (c is not null)
                        list.Add(c);
                }

                includeKinds = list.Count > 0 ? list : null;
            }

            if (dto.ExcludeKinds is { Count: > 0 })
            {
                var list = new List<string>();
                foreach (var t in dto.ExcludeKinds)
                {
                    var c = CodeNavigationRelatedKinds.TryCanonicalKind(t);
                    if (c is not null)
                        list.Add(c);
                }

                excludeKinds = list.Count > 0 ? list : null;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"presets_json: {ex.Message}";
            return false;
        }
    }
}
