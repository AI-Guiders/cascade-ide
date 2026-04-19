using System.Collections.Immutable;
using System.Text.Json;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services;

/// <summary>
/// Палитра команд: проекция <see cref="IdeCommandRegistry"/> на строки списка (ADR 0013).
/// </summary>
public static class IdeCommandPaletteCatalog
{
    /// <param name="AllowedFamilies">Если задано и не пусто — команда доступна только в перечисленных семействах UI-режима.</param>
    public sealed record Entry(
        string PaletteId,
        string CommandId,
        string Title,
        string Category,
        string? ArgsJson = null,
        ImmutableArray<UiModeFamily>? AllowedFamilies = null);

    public static ImmutableArray<Entry> All { get; } =
        IdeCommandRegistry.AllEntries
            .Where(e => e.IncludeInPalette)
            .Select(e => new Entry(
                e.PaletteId,
                e.CommandId!,
                e.Title,
                e.Category,
                e.ArgsJson,
                e.AllowedFamilies))
            .ToImmutableArray();

    /// <summary>Парсит JSON объекта args для <see cref="IdeMcpCommandExecutor.ExecuteAsync"/>.</summary>
    public static IReadOnlyDictionary<string, JsonElement>? ParseArgs(string? argsJson) =>
        IdeCommandRegistry.ParseArgs(argsJson);
}
