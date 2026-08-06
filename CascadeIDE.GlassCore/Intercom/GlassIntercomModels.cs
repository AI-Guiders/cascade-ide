#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Browsable FM model directory for Intercom HUD (CIT-lit).
/// Sealed Cloud.ru shortlist — Combo alone ≠ Face; List uses <see cref="Entry.Line"/>.
/// Live GET /v1/models can merge later via <see cref="BuildDirectory"/> extras.
/// </summary>
public static class GlassIntercomModels
{
    public const string DefaultId = "default";
    public const string Glm51Id = "zai-org/GLM-5.1";
    public const string QwenCoderNextId = "Qwen/Qwen3-Coder-Next";

    public readonly record struct Entry(string Id, string Display)
    {
        public string Line => string.Equals(Id, DefaultId, StringComparison.OrdinalIgnoreCase)
            ? $"{Display} · CFG"
            : Display;
    }

    /// <summary>Sealed day-1 catalog (Citizen wire + second slot).</summary>
    public static IReadOnlyList<Entry> SealedDirectory { get; } =
    [
        new(DefaultId, "default"),
        new(Glm51Id, "GLM-5.1"),
        new(QwenCoderNextId, "Qwen3-Coder-Next")
    ];

    public static IReadOnlyList<Entry> BuildDirectory(
        string? stickyModelId,
        IEnumerable<string>? extraIds = null)
    {
        var byId = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in SealedDirectory)
            byId[e.Id] = e;

        void Add(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;
            var id = raw.Trim();
            if (string.Equals(id, "—", StringComparison.Ordinal))
                return;
            if (byId.ContainsKey(id))
                return;
            byId[id] = new Entry(id, ShortLabel(id));
        }

        Add(stickyModelId);
        if (extraIds is not null)
        {
            foreach (var x in extraIds)
                Add(x);
        }

        // Stable: sealed order first, then extras alpha.
        var sealedIds = new HashSet<string>(SealedDirectory.Select(e => e.Id), StringComparer.OrdinalIgnoreCase);
        var head = SealedDirectory.ToList();
        var tail = byId.Values
            .Where(e => !sealedIds.Contains(e.Id))
            .OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        head.AddRange(tail);
        return head;
    }

    public static string ShortLabel(string modelId)
    {
        var t = (modelId ?? "").Trim();
        if (t.Length == 0)
            return DefaultId;
        var slash = t.LastIndexOf('/');
        return slash >= 0 && slash < t.Length - 1 ? t[(slash + 1)..] : t;
    }

    public static string? ResolveSelectedId(IReadOnlyList<Entry> directory, string? preferred)
    {
        if (directory.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(preferred)
            && Find(directory, preferred) is not null)
            return preferred.Trim();

        // null sticky → CFG default row
        if (string.IsNullOrWhiteSpace(preferred))
            return DefaultId;

        return directory[0].Id;
    }

    public static Entry? Find(IReadOnlyList<Entry> directory, string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || directory.Count == 0)
            return null;

        var needle = id.Trim();
        foreach (var e in directory)
        {
            if (string.Equals(e.Id, needle, StringComparison.OrdinalIgnoreCase))
                return e;
        }

        return null;
    }

    /// <summary>Wire value for latch: default → null (CFG).</summary>
    public static string? ToLatchModelId(string? selectedId)
    {
        if (string.IsNullOrWhiteSpace(selectedId))
            return null;
        if (string.Equals(selectedId.Trim(), DefaultId, StringComparison.OrdinalIgnoreCase))
            return null;
        if (string.Equals(selectedId.Trim(), "—", StringComparison.Ordinal))
            return null;
        return selectedId.Trim();
    }

    public static IReadOnlyList<string> PickerIds(IReadOnlyList<Entry> directory) =>
        directory.Select(e => e.Id).ToList();
}
