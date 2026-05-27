using CascadeIDE.Services;

namespace CascadeIDE.Models;

/// <summary>Подписи положительной/отрицательной ветви IF на графе control-flow (ADR 0053).</summary>
public static class CodeNavigationMapConditionBranchLabels
{
    public const string PresetPlusMinus = "plus_minus";
    public const string PresetTrueFalse = "true_false";
    public const string PresetOneZero = "one_zero";
    public const string PresetCustom = "custom";

    public sealed record Pair(string Positive, string Negative);

    public static Pair Resolve(CodeNavigationMapSettings? settings, string? solutionPath = null)
    {
        var map = settings ?? new CodeNavigationMapSettings();
        var presetId = NormalizePresetId(map.ConditionBranchLabelPreset);
        if (presetId == PresetCustom)
        {
            return new Pair(
                Sanitize(map.ConditionBranchPositive, "+"),
                Sanitize(map.ConditionBranchNegative, "-"));
        }

        var merged = CodeNavigationMapConditionBranchPresetsLoader.GetEffectivePresets(map, solutionPath);
        foreach (var entry in merged)
        {
            if (!string.Equals(entry.Id, presetId, StringComparison.OrdinalIgnoreCase))
                continue;
            return PairFromEntry(entry, fallbackPositive: "+", fallbackNegative: "-");
        }

        foreach (var entry in merged)
        {
            if (!string.Equals(entry.Id, PresetPlusMinus, StringComparison.OrdinalIgnoreCase))
                continue;
            return PairFromEntry(entry, "+", "-");
        }

        return new Pair("+", "-");
    }

    public static string? ResolveDisplayLabel(string? edgeProvenance, Pair labels)
    {
        if (!CodeNavigationMapConditionBranchProvenance.TryParseDisplayPolarity(edgeProvenance, out var isTrue))
            return null;
        return isTrue ? labels.Positive : labels.Negative;
    }

    public static string NormalizePresetId(string? value)
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0)
            return PresetPlusMinus;
        return v.ToLowerInvariant() switch
        {
            PresetTrueFalse or "true-false" => PresetTrueFalse,
            PresetOneZero or "one-zero" or "01" => PresetOneZero,
            PresetCustom => PresetCustom,
            _ => v
        };
    }

    private static Pair PairFromEntry(
        CodeNavigationMapConditionBranchPresetEntry entry,
        string fallbackPositive,
        string fallbackNegative) =>
        new(
            Sanitize(entry.Positive, fallbackPositive),
            Sanitize(entry.Negative, fallbackNegative));

    private static string Sanitize(string? text, string fallback)
    {
        var s = (text ?? "").Trim();
        return s.Length == 0 ? fallback : s.Length <= 8 ? s : s[..8];
    }
}
