#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Browsable FM model directory for Intercom HUD (CIT-lit).
/// Sealed Cloud.ru internal Face shortlist with tariff prices (RUB/Mtok ex-VAT).
/// Live GET /v1/models can merge later via <see cref="BuildDirectory"/> extras.
/// </summary>
public static class GlassIntercomModels
{
    public const string DefaultId = "default";
    public const string KimiK26Id = "moonshotai/Kimi-K2.6";
    public const string Glm51Id = "zai-org/GLM-5.1";
    public const string QwenCoderNextId = "Qwen/Qwen3-Coder-Next";
    public const string DeepSeekV4ProId = "deepseek-ai/DeepSeek-V4-Pro";
    public const string Qwen36_35BId = "Qwen/Qwen3.6-35B-A3B";
    public const string Qwen35_397BId = "Qwen/Qwen3.5-397B-A17B";

    /// <summary>Cloud.ru Evolution FM tariff revision used for sealed prices.</summary>
    public const string TariffRev = "260720";

    public readonly record struct Entry(
        string Id,
        string Display,
        int? InRubPerMtok = null,
        int? OutRubPerMtok = null)
    {
        public string PriceLine =>
            InRubPerMtok is int i && OutRubPerMtok is int o
                ? $"{i}/{o} \u20BD/M · ex-VAT"
                : "price · —";

        public string Line =>
            InRubPerMtok is int i && OutRubPerMtok is int o
                ? $"{Display} · {i}/{o}"
                : Display;

        public string ToolTip =>
            InRubPerMtok is int i && OutRubPerMtok is int o
                ? $"{Id}\nin {i} / out {o} \u20BD per 1M tok (ex-VAT) · Cloud.ru tariff {TariffRev}"
                : Id;
    }

    /// <summary>Sealed Face catalog — internal LLM slots with lived tariff dig.</summary>
    public static IReadOnlyList<Entry> SealedDirectory { get; } =
    [
        new(KimiK26Id, "Kimi-K2.6", 144, 595),
        new(Glm51Id, "GLM-5.1", 163, 680),
        new(QwenCoderNextId, "Qwen3-Coder-Next", 100, 200),
        new(DeepSeekV4ProId, "DeepSeek-V4-Pro", 150, 600),
        new(Qwen36_35BId, "Qwen3.6-35B-A3B", 180, 270),
        new(Qwen35_397BId, "Qwen3.5-397B-A17B", 750, 890)
    ];

    public static IReadOnlyList<Entry> BuildDirectory(
        string? stickyModelId,
        string? cfgModelId = null,
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
            if (string.Equals(id, DefaultId, StringComparison.OrdinalIgnoreCase))
                return;
            if (byId.ContainsKey(id))
                return;
            byId[id] = new Entry(id, ShortLabel(id));
        }

        Add(cfgModelId);
        Add(stickyModelId);
        if (extraIds is not null)
        {
            foreach (var x in extraIds)
                Add(x);
        }

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

    public static string? ResolveSelectedId(
        IReadOnlyList<Entry> directory,
        string? preferred,
        string? cfgModelId = null)
    {
        if (directory.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(preferred)
            && Find(directory, preferred) is not null)
            return preferred.Trim();

        if (!string.IsNullOrWhiteSpace(cfgModelId)
            && Find(directory, cfgModelId) is not null)
            return cfgModelId.Trim();

        if (string.IsNullOrWhiteSpace(preferred)
            && Find(directory, DefaultId) is not null)
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

    public static string FormatStatusLine(Entry entry, bool wroteCfg)
    {
        var price = entry.InRubPerMtok is int i && entry.OutRubPerMtok is int o
            ? $" · {i}/{o} \u20BD/M"
            : "";
        var cfg = wroteCfg ? " · CFG" : "";
        return $"glass · model · {entry.Display}{cfg}{price}";
    }
}
