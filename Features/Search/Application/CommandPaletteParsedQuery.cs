#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Результат разбора строки палитры (ADR 0112 §1).</summary>
internal abstract record CommandPaletteParsedQuery
{
    private CommandPaletteParsedQuery() { }

    internal sealed record Melody(string TailNormalized) : CommandPaletteParsedQuery;

    internal sealed record GoTo(GoToAllQuery Query) : CommandPaletteParsedQuery;

    /// <summary>Свободный текст для fuzzy-поиска по каталогу <see cref="IdeCommandPaletteCatalog.All"/>.</summary>
    internal sealed record Catalog(string TrimmedRaw) : CommandPaletteParsedQuery;
}

/// <summary>
/// Порядок: сначала <c>c:</c> (IntentMelody), затем <c>f|t|m|x:</c>, иначе каталог — совместимо с историческим UX.
/// </summary>
internal static class CommandPaletteParsedQueryParser
{
    internal static CommandPaletteParsedQuery Parse(string? raw)
    {
        if (IntentMelodyAliases.TryGetTail(raw, out var melodyTail))
            return new CommandPaletteParsedQuery.Melody(melodyTail);

        if (GoToAllQueryParser.TryParse(raw) is { } goTo)
            return new CommandPaletteParsedQuery.GoTo(goTo);

        var catalog = raw?.Trim() ?? "";
        return new CommandPaletteParsedQuery.Catalog(catalog);
    }
}
