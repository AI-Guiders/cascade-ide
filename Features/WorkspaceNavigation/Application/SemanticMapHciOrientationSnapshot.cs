#nullable enable

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Узкий снимок слоя B (HCI) для подсказки рядом с Semantic Map / карта намерений.
/// Не подменяет символьную истину Roslyn; только ориентация по индексу (ADR 0106 § Semantic Map и слой B).
/// </summary>
public sealed record SemanticMapHciOrientationHit(string LeafPath, string HitKind, int Line, string Snippet);

/// <param name="Hits">Топ попаданий из <c>SearchHybridAsync</c> (FTS, без semantic по умолчанию).</param>
/// <param name="Query">Запрос, отправленный в индекс (диагностика).</param>
/// <param name="Error">Ошибка поиска/индекса; при непустом <see cref="Hits"/> может быть null.</param>
public sealed record SemanticMapHciOrientationSnapshot(
    IReadOnlyList<SemanticMapHciOrientationHit> Hits,
    string Query,
    string? Error);
