namespace CascadeIDE.Services;

/// <summary>Формы melody (<c>c:</c>) из command-first каталога <see cref="IntentMelodyAliases.BundledRelativePath"/> (ADR 0109).</summary>
public static class IntentMelodyCatalog
{
    /// <inheritdoc cref="IntentMelodyAliases.GetCatalogSnapshot"/>
    public static bool TryGetRoot(string slug, out MelodyRootEntry entry)
    {
        var roots = IntentMelodyAliases.GetCatalogSnapshot().Roots;
        var key = slug.Trim().ToLowerInvariant();
        return roots.TryGetValue(key, out entry);
    }

    /// <summary>Параметрический melody-корень по <c>command_id</c> (0..1 на команду в каталоге).</summary>
    public static bool TryGetParametricRootByCommandId(string commandId, out MelodyRootEntry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        foreach (var root in IntentMelodyAliases.GetCatalogSnapshot().Roots.Values)
        {
            if (root.Shape != IntentMelodyShape.Parametric)
                continue;
            if (!string.Equals(root.CommandId, commandId.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            entry = root;
            return true;
        }

        return false;
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
    IReadOnlyDictionary<string, TailWireClassEntry> TailWireClasses,
    IReadOnlyDictionary<string, SlashRouteEntry> SlashRoutes);

public static class IntentSlashCatalog
{
    /// <inheritdoc cref="IntentMelodyAliases.GetCatalogSnapshot"/>
    public static IReadOnlyDictionary<string, SlashRouteEntry> SlashRoutes =>
        IntentMelodyAliases.GetCatalogSnapshot().SlashRoutes;

    public static bool TryGetRoute(string slashPath, out SlashRouteEntry entry)
    {
        var key = NormalizeSlashPath(slashPath);
        return SlashRoutes.TryGetValue(key, out entry);
    }

    internal static string NormalizeSlashPath(string? slashPath)
    {
        if (string.IsNullOrWhiteSpace(slashPath))
            return "";

        var t = slashPath.Trim();
        if (t.Length == 0)
            return "";

        if (t[0] != '/')
            t = "/" + t;

        return t;
    }
}
