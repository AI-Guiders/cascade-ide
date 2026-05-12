using CascadeIDE.Services;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Текст для FTS палитры: для <c>t:</c>/<c>m:</c> семантика шире regex <see cref="GoToPaletteRipgrepPatternBuilder"/> (ADR 0112 §6).</summary>
internal static class CommandPaletteHciQueryExtensions
{
    internal static string? TryBuildFtsSurfaceQuery(GoToAllQuery query)
    {
        var t = query.Term.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    /// <returns><c>null</c> — поиск по всем проиндексированным расширениям; иначе только <c>.cs</c> для типов и членов.</returns>
    internal static IReadOnlyList<string>? FtsIncludeExtensions(GoToAllQuery query) =>
        query.Prefix is 't' or 'm' ? [".cs"] : null;
}
