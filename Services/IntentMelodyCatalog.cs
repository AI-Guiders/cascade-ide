namespace CascadeIDE.Services;

/// <summary>Параметрика и метаданные корней melody (<c>c:</c>) по <see cref="IntentMelodyAliases.BundledRelativePath"/>, см. ADR 0109.</summary>
public static class IntentMelodyCatalog
{
    /// <inheritdoc cref="IntentMelodyAliases.GetCatalogSnapshot"/>
    public static bool TryGetRoot(string slug, out MelodyRootEntry entry)
    {
        var roots = IntentMelodyAliases.GetCatalogSnapshot().Roots;
        var key = slug.Trim().ToLowerInvariant();
        return roots.TryGetValue(key, out entry);
    }

    /// <summary>Реестр <c>[[tail_wire_class]]</c> по id (lower).</summary>
    public static bool TryGetTailWireClass(string wireClassId, out TailWireClassEntry wire)
    {
        var id = wireClassId.Trim().ToLowerInvariant();
        return IntentMelodyAliases.GetCatalogSnapshot().TailWireClasses.TryGetValue(id, out wire);
    }
}

public readonly record struct MelodyRootEntry(
    string Slug,
    string CommandId,
    IntentMelodyShape Shape,
    bool ShowUsageHintIfBareSlug,
    string? TailSignature,
    string? WireClass,
    string? ChordCommit,
    string? PaletteHintSlug,
    /// <summary>Строка подсказки палитры (c:…); если пусто — шаблон <c>c:slug:&lt;start&gt;:&lt;end&gt;</c> в <c>ParametricIntentMelody</c>.</summary>
    string? PaletteUsageHint = null,
    /// <summary>Категория строки палитры; если пусто — общий fallback.</summary>
    string? PaletteUsageCategory = null);

public enum IntentMelodyShape
{
    Simple,
    Parametric,
}

/// <remarks>ADR 0109 <c>[[tail_wire_class]].kind</c>.</remarks>
public enum TailWireKind
{
    SingleRemainder,
    DelimitedSlots,
}

/// <param name="BetweenSlotsSeparators">Для <see cref="TailWireKind.DelimitedSlots"/> — литералы между целочисленными слотами (обычно <c>:</c> и/или пробел).</param>
public readonly record struct TailWireClassEntry(string Id, TailWireKind Kind, string[] BetweenSlotsSeparators);

public sealed record IntentMelodyCatalogSnapshot(
    IReadOnlyDictionary<string, MelodyRootEntry> Roots,
    IReadOnlyDictionary<string, TailWireClassEntry> TailWireClasses);
