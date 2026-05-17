#nullable enable
using CascadeIDE.Cockpit.Graph;

namespace CascadeIDE.Services;

/// <summary>
/// Короткий сценарный API для <b>карты намерений</b> (CodeNavigation) поверх <see cref="GraphDocumentBlueprint"/> (те же виды шагов, что и у
/// <see cref="CascadeIDE.Services.CodeNavigation.CodeNavigationControlFlowSubgraphBuilder"/>):
/// <list type="bullet">
/// <item><description><see cref="DrawFunction"/> — <c>call_step</c>, ребро <c>Call</c>;</description></item>
/// <item><description><see cref="DrawLoopCall"/> — <c>call_step</c>, ребро <c>LoopCall</c> (вызов в теле цикла);</description></item>
/// <item><description><see cref="DrawCondition"/> — <c>condition_step</c>, рёбра <c>ConditionalCall</c>;</description></item>
/// <item><description><see cref="DrawReturn"/> — <c>exit_step</c>, рёбра <c>Exit</c> (выход из метода).</description></item>
/// </list>
/// В wire-формате ещё бывают <c>Merge</c>, <c>MultiBranch</c> и т.д.; для них по-прежнему можно собрать граф руками через blueprint или JSON.
/// </summary>
public sealed class ControlFlowSceneSketch
{
    private const string DefaultAnchorLabel = "file";

    private readonly GraphDocumentBlueprint _blueprint;
    private readonly Dictionary<string, string> _stepKeyToNodeId = new(StringComparer.Ordinal);

    public ControlFlowSceneSketch(
        string anchorPath,
        string anchorRationale = "sketch",
        int maxNodes = 64,
        int maxEdges = 128)
    {
        AnchorPath = anchorPath;
        _blueprint = new GraphDocumentBlueprint(
            anchorPath,
            maxNodes,
            maxEdges,
            DefaultAnchorLabel,
            anchorRationale,
            GraphKind.CodeIntent);
    }

    public string AnchorPath { get; }

    /// <summary>Якорь subgraph (id узла <c>n0</c>).</summary>
    public string AnchorNodeId => _blueprint.AnchorNodeId;

    /// <summary>
    /// Последовательный шаг вызова: <paramref name="fromStepKey"/> — ключ уже добавленного шага или <c>null</c> (от якоря).
    /// </summary>
    public string DrawFunction(string? fromStepKey, string toStepKey) =>
        DrawCallStep(fromStepKey, toStepKey, "Call", "Call");

    /// <summary>
    /// Вызов в контексте цикла (<c>LoopCall</c>), как вызов внутри <c>for</c>/<c>while</c> в билдере по AST.
    /// </summary>
    public string DrawLoopCall(string? fromStepKey, string toStepKey) =>
        DrawCallStep(fromStepKey, toStepKey, "LoopCall", "LoopCall");

    private string DrawCallStep(string? fromStepKey, string toStepKey, string edgeKind, string relatedKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toStepKey);
        if (_stepKeyToNodeId.ContainsKey(toStepKey))
            throw new InvalidOperationException($"Step key '{toStepKey}' is already used.");

        var fromId = ResolveFromKey(fromStepKey);
        var id = _blueprint.TryAddNode(
            "call_step",
            AnchorPath,
            toStepKey,
            "",
            $"call {toStepKey}",
            toStepKey,
            assignControlFlowLegendIndex: true);
        if (id is null)
            throw new InvalidOperationException("Node cap reached.");

        _blueprint.AddEdges([fromId], id, edgeKind, relatedKind);
        _stepKeyToNodeId[toStepKey] = id;
        return id;
    }

    /// <summary>
    /// Ветвление: от шага <paramref name="fromStepKey"/> — узел условия, затем два шага <paramref name="thenStepKey"/> и <paramref name="elseStepKey"/>.
    /// Текст условия для легенды — <paramref name="conditionLegend"/> (например предикат исходного <c>if</c>).
    /// </summary>
    public string DrawCondition(
        string fromStepKey,
        string thenStepKey,
        string elseStepKey,
        string? conditionLegend = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromStepKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(thenStepKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(elseStepKey);
        if (string.Equals(thenStepKey, elseStepKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Then and else step keys must differ.");

        foreach (var k in new[] { thenStepKey, elseStepKey })
        {
            if (_stepKeyToNodeId.ContainsKey(k))
                throw new InvalidOperationException($"Step key '{k}' is already used.");
        }

        var fromId = ResolveFromKey(fromStepKey);
        var leg = string.IsNullOrWhiteSpace(conditionLegend) ? "if condition" : conditionLegend.Trim();

        var condId = _blueprint.TryAddNode(
            "condition_step",
            AnchorPath,
            "IF",
            "",
            "if condition",
            leg,
            assignControlFlowLegendIndex: true);
        if (condId is null)
            throw new InvalidOperationException("Node cap reached.");

        _blueprint.AddEdges([fromId], condId, "ConditionalCall", "ConditionalCall");

        var thenId = AddCallBranch(thenStepKey);
        var elseId = AddCallBranch(elseStepKey);
        _blueprint.AddEdges([condId], thenId, "ConditionalCall", "ConditionalCall");
        _blueprint.AddEdges([condId], elseId, "ConditionalCall", "ConditionalCall");

        _stepKeyToNodeId[thenStepKey] = thenId;
        _stepKeyToNodeId[elseStepKey] = elseId;
        return condId;
    }

    /// <summary>
    /// Выход из метода (<c>exit_step</c>): узел «return», рёбра <c>Exit</c>. Опционально зарегистрировать <paramref name="exitStepKey"/>, если на этот шаг ссылаются дальше (редко).
    /// </summary>
    public string DrawReturn(string fromStepKey, string? exitStepKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromStepKey);
        if (exitStepKey is not null && _stepKeyToNodeId.ContainsKey(exitStepKey))
            throw new InvalidOperationException($"Step key '{exitStepKey}' is already used.");

        var fromId = ResolveFromKey(fromStepKey);
        var id = _blueprint.TryAddNode(
            "exit_step",
            AnchorPath,
            "RET",
            "",
            "return",
            "return",
            assignControlFlowLegendIndex: true);
        if (id is null)
            throw new InvalidOperationException("Node cap reached.");

        _blueprint.AddEdges([fromId], id, "Exit", "Exit");
        if (!string.IsNullOrWhiteSpace(exitStepKey))
            _stepKeyToNodeId[exitStepKey!] = id;
        return id;
    }

    /// <summary>
    /// Документ для <see cref="CascadeIDE.Features.WorkspaceNavigation.Application.CodeNavigationMapCompositor"/> (<see cref="GraphDocument"/> + <c>CodeNavigationMapLevelKind.ControlFlow</c>):
    /// раскладка идёт через Intent → Declutter → Layout, без обхода пайплайна (ADR 0055).
    /// </summary>
    public GraphDocument ToDocument() => _blueprint.ToDocument();

    private string AddCallBranch(string stepKey)
    {
        var id = _blueprint.TryAddNode(
            "call_step",
            AnchorPath,
            stepKey,
            "",
            $"call {stepKey}",
            stepKey,
            assignControlFlowLegendIndex: true);
        if (id is null)
            throw new InvalidOperationException("Node cap reached.");
        return id;
    }

    private string ResolveFromKey(string? fromStepKey)
    {
        if (fromStepKey is null)
            return _blueprint.AnchorNodeId;
        if (_stepKeyToNodeId.TryGetValue(fromStepKey, out var id))
            return id;
        throw new InvalidOperationException($"Unknown step key '{fromStepKey}'.");
    }
}
