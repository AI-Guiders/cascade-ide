#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.ViewModels;
using CascadeIDE.Services.SkiaInstruments;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Композитор карты кода: выбирает layout по уровню карты и возвращает сцену
/// вместе с рекомендуемой высотой viewport для читаемой отрисовки.
/// </summary>
public interface ICodeNavigationMapCompositor
    : ISkiaInstrumentCompositor<CodeNavigationMapCompositionIntent, CodeNavigationMapCompositionResult>
{
    CodeNavigationMapCompositionResult Compose(
        CodeNavigationMapSubgraphDocument doc,
        string mapLevel,
        double availableWidth,
        double availableHeight,
        CodeNavigationMapDetailLevel detailLevel = CodeNavigationMapDetailLevel.Normal);
}

public sealed record CodeNavigationMapCompositionResult(
    CodeNavigationMapGraphSceneVm Scene,
    double PreferredHeight,
    IReadOnlyList<CodeNavigationMapInstrumentBlockDescriptor> CodeNavigationMapInstrumentBlocks);

/// <param name="DetailLevel">Политика Declutter + метрики Layout (ADR 0055); источник — <c>[code_navigation_map].detail_level</c>.</param>
public sealed record CodeNavigationMapCompositionIntent(
    CodeNavigationMapSubgraphDocument Subgraph,
    string MapLevel,
    CodeNavigationMapDetailLevel DetailLevel = CodeNavigationMapDetailLevel.Normal);
