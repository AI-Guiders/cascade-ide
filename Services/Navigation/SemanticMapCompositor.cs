#nullable enable
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Models;
using CascadeIDE.Services.SkiaInstruments;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Оркестратор компоновки <b>семантической карты</b> (граф → сцена для отрисовки): уровень карты → движок раскладки + политика высоты.
/// «Карта кода» (control flow) — <b>CodeNavigation</b>; режим файлов/связей — <b>WorkspaceNavigation</b> (см. <see cref="Models.SemanticMapLevelKind"/>).
/// Поверхность Skia compositor занимается размещением в слотах кокпита, а не смыслом узлов.
/// </summary>
public sealed class SemanticMapCompositor : ISemanticMapCompositor
{
    public const double DefaultWidth = SemanticMapGraphPrimitives.DefaultViewportWidth;
    public const double DefaultHeightFile = SemanticMapGraphPrimitives.DefaultViewportHeightFile;
    public const double DefaultHeightControlFlow = SemanticMapGraphPrimitives.DefaultViewportHeightControlFlow;
    /// <summary>Верхний предел «интринсик»-высоты и слияния с viewport; сцена карты заполняет высоту области, если она передана в SkiaInstrumentViewport.</summary>
    public const double MaxHeightControlFlow = SemanticMapGraphPrimitives.MaxViewportHeightControlFlow;

    private readonly ISemanticMapIntentStage _intentStage;
    private readonly ISemanticMapDeclutterStage _declutterStage;
    private readonly ISemanticMapLayoutStage _layoutStage;

    public SemanticMapCompositor(
        ISemanticMapSubgraphLayoutEngine? fileLayout = null,
        ISemanticMapSubgraphLayoutEngine? controlFlowLayout = null,
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
        SemanticMapSubgraphDocument doc,
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
