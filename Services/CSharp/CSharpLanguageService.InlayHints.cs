using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    private static readonly SymbolDisplayFormat InlayTypeDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Inlay-подсказки в буфер: <c>var → тип</c> (0085/0103), при необходимости имена аргументов
    /// (<c>  paramName: </c> у позиционных) и у индексаторов, как type/parameter hints в IDE.
    /// Позиционные аргументы: не дублировать имя, если аргумент — простой идентификатор с тем же имени, что и параметр;
    /// для <c>params</c> — одна подсказка на сгруппированные значения (не на каждый элемент).
    /// Кэш по (path, text); вызывать с актуальным <paramref name="sourceText"/>.
    /// </summary>
    public IReadOnlyList<EditorTrailingInlayPart> GetVarInlayHintsForFile(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return [];
        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var fileKey = (filePath, textHash);
        if (_inlayHintCache.TryGetValue(fileKey, out var cached))
            return cached;
        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var root = model.SyntaxTree.GetRoot(ct);
            var list = new List<EditorTrailingInlayPart>(32);
            foreach (var node in root.DescendantNodes())
            {
                ct.ThrowIfCancellationRequested();
                switch (node)
                {
                    case LocalDeclarationStatementSyntax lds
                        when lds.Declaration is { Type: IdentifierNameSyntax { Identifier.Text: "var" } } && lds.Declaration.Variables.Count > 0:
                    {
                        var v0 = lds.Declaration.Variables[0];
                        if (model.GetDeclaredSymbol(v0, ct) is not ILocalSymbol local)
                            break;
                        var label = "  " + local.Type.ToDisplayString(InlayTypeDisplayFormat);
                        var anchor = lds.Declaration.Type.Span.End;
                        list.Add(new EditorTrailingInlayPart(anchor, label));
                        break;
                    }
                    case ForEachStatementSyntax fes
                        when fes.Type is IdentifierNameSyntax { Identifier.Text: "var" }:
                    {
                        if (model.GetDeclaredSymbol(fes, ct) is not ILocalSymbol le)
                            break;
                        var label2 = "  " + le.Type.ToDisplayString(InlayTypeDisplayFormat);
                        var anchor2 = fes.Type.Span.End;
                        list.Add(new EditorTrailingInlayPart(anchor2, label2));
                        break;
                    }
                    case InvocationExpressionSyntax inv:
                        AddParameterNameInlaysForParenthesizedList(model, inv.ArgumentList, list, ct);
                        break;
                    case ObjectCreationExpressionSyntax oce:
                        AddParameterNameInlaysForParenthesizedList(model, oce.ArgumentList, list, ct);
                        break;
                    case ImplicitObjectCreationExpressionSyntax ioc:
                        AddParameterNameInlaysForParenthesizedList(model, ioc.ArgumentList, list, ct);
                        break;
                    case ElementAccessExpressionSyntax eae:
                        AddParameterNameInlaysForBracketedList(model, eae.ArgumentList, list, ct);
                        break;
                }
            }
            // Normalize hints per anchor: drop blank labels and keep the most informative one.
            // This prevents first-seen empty placeholders from hiding useful labels like "args:"/"message:".
            var bestByAnchor = new Dictionary<int, string>();
            foreach (var p in list)
            {
                if (string.IsNullOrWhiteSpace(p.Label))
                    continue;
                if (!bestByAnchor.TryGetValue(p.AnchorOffset, out var existing) || p.Label.Length > existing.Length)
                    bestByAnchor[p.AnchorOffset] = p.Label;
            }
            IReadOnlyList<EditorTrailingInlayPart> result = bestByAnchor
                .OrderBy(static kv => kv.Key)
                .Select(static kv => new EditorTrailingInlayPart(kv.Key, kv.Value))
                .ToList();
            if (InlayHintTrace.IsEnabled)
            {
                const int cap = 64;
                static string OneLineLabel(string? s) =>
                    (s ?? "").ReplaceLineEndings(" ").Replace('\r', ' ').Replace('\n', ' ');
                var preview = result.Count == 0
                    ? ""
                    : string.Join(" | ", result.Take(cap).Select(p => $"{p.AnchorOffset}→{OneLineLabel(p.Label)}"));
                if (result.Count > cap)
                    preview += $" | ...+{result.Count - cap}";
                InlayHintTrace.Log($"GetVarInlayHints count={result.Count} file={filePath} {preview}");
            }
            TrimInlayCache();
            _inlayHintCache[fileKey] = result;
            return result;
        }
        catch
        {
            return [];
        }
    }

    private static void AddParameterNameInlaysForParenthesizedList(
        SemanticModel model,
        ArgumentListSyntax? list,
        List<EditorTrailingInlayPart> outList,
        CancellationToken ct)
    {
        if (list is null)
            return;
        AddParameterNameInlaysForArguments(model, list.Arguments, outList, ct);
    }

    private static void AddParameterNameInlaysForBracketedList(
        SemanticModel model,
        BracketedArgumentListSyntax? list,
        List<EditorTrailingInlayPart> outList,
        CancellationToken ct)
    {
        if (list is null)
            return;
        AddParameterNameInlaysForArguments(model, list.Arguments, outList, ct);
    }

    private static IArgumentOperation? TryGetArgumentOperation(SemanticModel model, ArgumentSyntax arg, CancellationToken ct)
    {
        IOperation? op;
        try
        {
            op = model.GetOperation(arg, ct);
        }
        catch
        {
            return null;
        }
        while (op is IConversionOperation conv)
            op = conv.Operand;
        if (op is IArgumentOperation direct)
            return direct;
        // Иногда с аргументом связана операция над выражением (ILiteralOperation и т.д.) — поднимаемся по Parent.
        var walk = op;
        while (walk is not null && walk is not IArgumentOperation)
            walk = walk.Parent;
        if (walk is IArgumentOperation fromParent)
            return fromParent;

        // Roslyn иногда не возвращает IArgumentOperation с узла (внутренняя конверсия/значение);
        // привязываемся к IInvocationOperation / IObjectCreationOperation по Syntax аргумента.
        if (arg.Parent is not BaseArgumentListSyntax alist || alist.Parent is null)
            return null;
        IOperation? parentOp;
        try
        {
            parentOp = model.GetOperation(alist.Parent, ct);
        }
        catch
        {
            return null;
        }
        if (parentOp is IInvocationOperation invOp)
        {
            foreach (var a in invOp.Arguments)
            {
                if (a.Syntax == arg)
                    return a;
            }
            // params: IInvocationOperation.Arguments часто == 1; IArgumentOperation.Syntax не равен каждому ArgumentSyntax. Любой синтаксический arg из того же списка относится к тому же params.
            if (alist is ArgumentListSyntax al3)
            {
                foreach (var a in invOp.Arguments)
                {
                    if (a.Parameter is not { IsParams: true })
                        continue;
                    foreach (var syn in al3.Arguments)
                    {
                        if (syn == arg)
                            return a;
                    }
                }
            }
        }
        if (parentOp is IObjectCreationOperation ocr)
        {
            foreach (var a in ocr.Arguments)
            {
                if (a.Syntax == arg)
                    return a;
            }
        }
        return null;
    }

    private static ISymbol? GetBestSymbol(SymbolInfo info) =>
        info.Symbol ?? info.CandidateSymbols.FirstOrDefault();

    private static IParameterSymbol? TryResolveParameterBySymbolInfo(
        SemanticModel model,
        ArgumentSyntax arg,
        CancellationToken ct)
    {
        if (arg.Parent is not BaseArgumentListSyntax list)
            return null;

        var index = list.Arguments.IndexOf(arg);
        if (index < 0)
            return null;

        static IParameterSymbol? SelectParameter(ImmutableArray<IParameterSymbol> parameters, int argIndex)
        {
            if (parameters.IsDefaultOrEmpty)
                return null;
            var last = parameters.Length - 1;
            if (parameters[last].IsParams && argIndex >= last)
                return parameters[last];
            return argIndex < parameters.Length ? parameters[argIndex] : null;
        }

        var parent = list.Parent;
        ISymbol? symbol = parent switch
        {
            InvocationExpressionSyntax inv => GetBestSymbol(model.GetSymbolInfo(inv, ct)),
            ObjectCreationExpressionSyntax oce => GetBestSymbol(model.GetSymbolInfo(oce, ct)),
            ImplicitObjectCreationExpressionSyntax ioc => GetBestSymbol(model.GetSymbolInfo(ioc, ct)),
            ElementAccessExpressionSyntax eae => GetBestSymbol(model.GetSymbolInfo(eae, ct)),
            _ => null
        };

        var parameter = symbol switch
        {
            IMethodSymbol method => SelectParameter(method.Parameters, index),
            IPropertySymbol { IsIndexer: true } property => SelectParameter(property.Parameters, index),
            _ => null
        };
        if (parameter is not null)
            return parameter;

        // If constructor symbol isn't bound directly (e.g., reduced semantic model),
        // resolve by created type and pick a matching ctor by argument count/optionality.
        if (parent is ObjectCreationExpressionSyntax oce2 || parent is ImplicitObjectCreationExpressionSyntax)
        {
            INamedTypeSymbol? createdType = parent switch
            {
                ObjectCreationExpressionSyntax ocex => model.GetTypeInfo(ocex.Type, ct).Type as INamedTypeSymbol,
                ImplicitObjectCreationExpressionSyntax iocx => (model.GetTypeInfo(iocx, ct).Type
                    ?? model.GetTypeInfo(iocx, ct).ConvertedType) as INamedTypeSymbol,
                _ => null
            };
            if (createdType is null)
                return null;
            var argCount = list.Arguments.Count;
            IMethodSymbol? bestCtor = null;
            var bestScore = int.MinValue;
            foreach (var ctor in createdType.InstanceConstructors)
            {
                if (ctor.Parameters.IsDefaultOrEmpty)
                    continue;

                var parameters = ctor.Parameters;
                var hasParams = parameters[^1].IsParams;
                var required = parameters.Count(p => !p.IsOptional && !p.IsParams);
                if (argCount < required)
                    continue;
                if (!hasParams && argCount > parameters.Length)
                    continue;

                // Prefer exact arity, then the closest non-params shape.
                var score = parameters.Length == argCount ? 1000 : 0;
                if (hasParams)
                    score -= 100;
                score -= Math.Abs(parameters.Length - argCount);
                if (score <= bestScore)
                    continue;
                bestScore = score;
                bestCtor = ctor;
            }

            return bestCtor is null ? null : SelectParameter(bestCtor.Parameters, index);
        }

        return null;
    }

    private static IParameterSymbol? TryGetArgumentParameter(
        SemanticModel model,
        ArgumentSyntax arg,
        CancellationToken ct)
    {
        return TryGetArgumentOperation(model, arg, ct)?.Parameter
            ?? TryResolveParameterBySymbolInfo(model, arg, ct);
    }

    private static void AddParameterNameInlaysForArguments(
        SemanticModel model,
        SeparatedSyntaxList<ArgumentSyntax> args,
        List<EditorTrailingInlayPart> outList,
        CancellationToken ct)
    {
        // For params, Roslyn gives one IParameterSymbol per element — show a single inlay (first arg), not "items: " on every value.
        IParameterSymbol? paramsHintEmittedFor = null;
        foreach (var arg in args)
        {
            ct.ThrowIfCancellationRequested();
            if (arg.NameColon is not null)
                continue;
            var resolvedParameter = TryGetArgumentParameter(model, arg, ct);
            if (resolvedParameter is not IParameterSymbol p)
                continue;
            if (string.IsNullOrEmpty(p.Name))
                continue;
            var parameterName = p.Name;
            var isParams = p.IsParams;

            if (isParams)
            {
                if (resolvedParameter is IParameterSymbol p2 &&
                    paramsHintEmittedFor is not null &&
                    SymbolEqualityComparer.Default.Equals(paramsHintEmittedFor, p2))
                {
                    continue;
                }
                if (resolvedParameter is IParameterSymbol p3)
                    paramsHintEmittedFor = p3;
            }
            else
            {
                paramsHintEmittedFor = null;
            }

            // Drop redundant "args: " when the argument is just `args` (same as VS inlay policy).
            if (arg.Expression is IdentifierNameSyntax idArg &&
                string.Equals(idArg.Identifier.ValueText, parameterName, StringComparison.Ordinal))
                continue;

            var label = "  " + parameterName + ": ";
            outList.Add(new EditorTrailingInlayPart(arg.Expression.SpanStart, label));
        }
    }
}
