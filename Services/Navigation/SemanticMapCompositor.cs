#nullable enable
using CascadeIDE.Services.SkiaInstruments;
namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Оркестратор компоновки Semantic Map: уровень карты -> движок раскладки + политика высоты.
/// Surface compositor решает только размещение инструмента в слотах, а не внутреннюю геометрию графа.
/// </summary>
public sealed class SemanticMapCompositor : ISemanticMapCompositor
{
    public const double DefaultWidth = 280;
    public const double DefaultHeightFile = 120;
    public const double DefaultHeightControlFlow = 220;

    private readonly ISemanticMapIntentStage _intentStage;
    private readonly ISemanticMapDeclutterStage _declutterStage;
    private readonly ISemanticMapLayoutStage _layoutStage;

    public SemanticMapCompositor(
        IWorkspaceNavigationGraphLayoutEngine? fileLayout = null,
        IWorkspaceNavigationGraphLayoutEngine? controlFlowLayout = null,
        ISemanticMapIntentStage? intentStage = null,
        ISemanticMapDeclutterStage? declutterStage = null,
        ISemanticMapLayoutStage? layoutStage = null)
    {
        _intentStage = intentStage ?? new SemanticMapIntentStage();
        _declutterStage = declutterStage ?? new SemanticMapDeclutterStage();
        _layoutStage = layoutStage ?? new SemanticMapLayoutStage(fileLayout, controlFlowLayout);
    }

    public SemanticMapCompositionResult Compose(SemanticMapCompositionIntent intent, in SkiaInstrumentViewport viewport) =>
        Compose(intent.Subgraph, intent.SemanticMapLevel, viewport.Width, viewport.Height);

    public SemanticMapCompositionResult Compose(
        WorkspaceNavigationSubgraphDocument doc,
        string semanticMapLevel,
        double availableWidth,
        double availableHeight)
    {
        var context = new SemanticMapPipelineContext(
            doc,
            semanticMapLevel,
            new SkiaInstrumentViewport(availableWidth, availableHeight),
            SemanticMapDetailLevel.Normal);
        var intent = _intentStage.Resolve(context);
        var decluttered = _declutterStage.Apply(intent);
        return _layoutStage.Layout(decluttered);
    }
}
