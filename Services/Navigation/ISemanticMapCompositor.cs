#nullable enable
using CascadeIDE.ViewModels;
using CascadeIDE.Services.SkiaInstruments;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Композитор Semantic Map: выбирает layout по уровню карты и возвращает сцену
/// вместе с рекомендуемой высотой viewport для читаемой отрисовки.
/// </summary>
public interface ISemanticMapCompositor
    : ISkiaInstrumentCompositor<SemanticMapCompositionIntent, SemanticMapCompositionResult>
{
    SemanticMapCompositionResult Compose(
        WorkspaceNavigationSubgraphDocument doc,
        string semanticMapLevel,
        double availableWidth,
        double availableHeight);
}

public sealed record SemanticMapCompositionResult(SemanticMapGraphSceneVm Scene, double PreferredHeight);
public sealed record SemanticMapCompositionIntent(WorkspaceNavigationSubgraphDocument Subgraph, string SemanticMapLevel);
