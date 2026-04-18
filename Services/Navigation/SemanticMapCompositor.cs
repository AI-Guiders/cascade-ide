#nullable enable
using CascadeIDE.Models;
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
    /// <summary>Верхний предел «интринсик»-высоты и слияния с viewport; сама карта заполняет высоту инструмента, если она передана в SkiaInstrumentViewport.</summary>
    public const double MaxHeightControlFlow = 640;

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
        _declutterStage = declutterStage ?? new SemanticMapDeclutterStage(_intentStage);
        _layoutStage = layoutStage ?? new SemanticMapLayoutStage(fileLayout, controlFlowLayout);
    }

    public SemanticMapCompositionResult Compose(SemanticMapCompositionIntent intent, in SkiaInstrumentViewport viewport)
    {
        var context = new SemanticMapPipelineContext(
            intent.Subgraph,
            intent.SemanticMapLevel,
            viewport,
            SemanticMapDetailLevel.Normal);
        var resolved = _intentStage.Resolve(context);
        var decluttered = _declutterStage.Apply(resolved);
        return _layoutStage.Layout(decluttered);
    }

    public SemanticMapCompositionResult Compose(
        WorkspaceNavigationSubgraphDocument doc,
        string semanticMapLevel,
        double availableWidth,
        double availableHeight,
        SemanticMapDetailLevel detailLevel = SemanticMapDetailLevel.Normal)
    {
        var context = new SemanticMapPipelineContext(
            doc,
            semanticMapLevel,
            new SkiaInstrumentViewport(availableWidth, availableHeight),
            detailLevel);
        var resolved = _intentStage.Resolve(context);
        var decluttered = _declutterStage.Apply(resolved);
        return _layoutStage.Layout(decluttered);
    }
}
