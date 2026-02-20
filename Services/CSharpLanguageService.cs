using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

/// <summary>Сервис языка C#: автодополнение, подсказки по параметрам, подсветка вхождений. Работает в фоне, с кэшем.</summary>
public sealed class CSharpLanguageService
{
    private const int CacheMaxEntries = 128;
    private const int TextHashCacheMaxEntries = 16;

    private readonly ConcurrentDictionary<(string path, int textHash), (CSharpCompilation comp, SyntaxTree tree)> _modelCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash, int line, int col), IReadOnlyList<CompletionItem>> _completionCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash, int line, int col), string?> _signatureCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash, int line, int col), IReadOnlyList<TextSpan>> _highlightCache = new();
    private readonly LinkedList<(string path, int textHash)> _modelCacheOrder = new();
    private readonly object _modelCacheLock = new();

    private static int GetStableHash(SourceText text)
    {
        var s = text.ToString();
        unchecked
        {
            int h = 0;
            foreach (var c in s)
                h = (h * 31) + c;
            return h;
        }
    }

    private static MetadataReference[] GetDefaultReferences()
    {
        var refs = new List<MetadataReference>();
        try
        {
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location));
        }
        catch
        {
            // Минимальный набор при ошибке
        }
        return refs.ToArray();
    }

    private (CSharpCompilation comp, SyntaxTree tree) GetOrCreateCompilationAndTree(string filePath, SourceText sourceText, CancellationToken ct)
    {
        var textHash = GetStableHash(sourceText);
        if (_modelCache.TryGetValue((filePath, textHash), out var cached))
            return cached;

        lock (_modelCacheLock)
        {
            if (_modelCache.TryGetValue((filePath, textHash), out cached))
                return cached;

            while (_modelCache.Count >= TextHashCacheMaxEntries && _modelCacheOrder.Count > 0)
            {
                var oldest = _modelCacheOrder.First!.Value;
                _modelCacheOrder.RemoveFirst();
                _modelCache.TryRemove(oldest, out _);
            }

            var tree = CSharpSyntaxTree.ParseText(sourceText, path: filePath, cancellationToken: ct);
            var compilation = CSharpCompilation.Create(
                "Temp",
                [tree],
                GetDefaultReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var entry = (compilation, tree);
            _modelCache[(filePath, textHash)] = entry;
            _modelCacheOrder.AddLast((filePath, textHash));
            return entry;
        }
    }

    private SemanticModel GetOrCreateModel(string filePath, SourceText sourceText, CancellationToken ct)
    {
        var (comp, tree) = GetOrCreateCompilationAndTree(filePath, sourceText, ct);
        return comp.GetSemanticModel(tree, ignoreAccessibility: true);
    }

    private static void TrimCaches<T>(ConcurrentDictionary<(string, int, int, int), T> cache)
    {
        if (cache.Count <= CacheMaxEntries) return;
        var keys = cache.Keys.ToList();
        foreach (var k in keys.Take(keys.Count - CacheMaxEntries))
            cache.TryRemove(k, out _);
    }

    /// <summary>Элемент автодополнения.</summary>
    public sealed record CompletionItem(string DisplayText, string InsertText, string? Description = null);

    /// <summary>Возвращает предложения автодополнения в позиции (1-based line, column). Выполнять в фоне.</summary>
    public IReadOnlyList<CompletionItem> GetCompletionItems(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1) return [];
        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_completionCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count) return [];
            var lineInfo = lines[line - 1];
            var colIndex = column - 1;
            var position = lineInfo.Start + Math.Min(Math.Max(0, colIndex), lineInfo.Span.Length);

            var root = model.SyntaxTree.GetRoot(ct);
            var token = root.FindToken(position, findInsideTrivia: true);
            var list = new List<CompletionItem>();

            // После точки — члены типа
            if (position > 0 && position <= text.Length && sourceText[position - 1] == '.')
            {
                var expr = token.Parent?.Parent as ExpressionSyntax;
                if (expr is not null)
                {
                    var symbolInfo = model.GetSymbolInfo(expr, ct);
                    if (symbolInfo.Symbol is ITypeSymbol typeSymbol)
                    {
                        foreach (var member in typeSymbol.GetMembers().Where(m => m.CanBeReferencedByName && !m.IsImplicitlyDeclared))
                        {
                            var insert = member.Name;
                            if (member is IMethodSymbol method && method.Parameters.Length > 0)
                                insert += "(";
                            list.Add(new CompletionItem(member.Name, insert, member.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                        }
                    }
                }
            }
            else
            {
                // Ключевые слова C#
                var keywords = new[] { "abstract", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "get", "goto", "if", "implicit", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var", "virtual", "void", "volatile", "while" };
                list.AddRange(keywords.Select(k => new CompletionItem(k, k)));

                // Символы в области видимости
                var semanticModel = model;
                var lookupPosition = token.SpanStart;
                foreach (var symbol in semanticModel.LookupSymbols(lookupPosition))
                {
                    if (symbol.CanBeReferencedByName && !symbol.IsImplicitlyDeclared && !list.Any(c => c.InsertText == symbol.Name))
                    {
                        var insert = symbol.Name;
                        if (symbol is IMethodSymbol ms && ms.Parameters.Length > 0)
                            insert += "(";
                        list.Add(new CompletionItem(symbol.Name, insert, symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    }
                }
            }

            TrimCaches(_completionCache);
            _completionCache[cacheKey] = list;
            return list;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Подсказка по параметрам (сигнатура метода) в позиции. Выполнять в фоне.</summary>
    public string? GetSignatureHelp(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1) return null;
        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_signatureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count) return null;
            var lineInfo = lines[line - 1];
            var colIndex = column - 1;
            var position = lineInfo.Start + Math.Min(Math.Max(0, colIndex), lineInfo.Span.Length);

            var root = model.SyntaxTree.GetRoot(ct);
            var node = root.FindNode(new TextSpan(position, 0), findInsideTrivia: true);
            InvocationExpressionSyntax? invocation = null;
            for (var n = node; n is not null; n = n.Parent)
            {
                if (n is InvocationExpressionSyntax inv)
                {
                    invocation = inv;
                    break;
                }
            }
            if (invocation is null) return null;

            var symbolInfo = model.GetSymbolInfo(invocation, ct);
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                var sig = method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                TrimCaches(_signatureCache);
                _signatureCache[cacheKey] = sig;
                return sig;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Диапазоны подсветки вхождений символа в том же файле (offset, length). Выполнять в фоне.</summary>
    public IReadOnlyList<TextSpan> GetHighlightSpans(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1) return [];
        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_highlightCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count) return [];
            var lineInfo = lines[line - 1];
            var colIndex = column - 1;
            var position = lineInfo.Start + Math.Min(Math.Max(0, colIndex), lineInfo.Span.Length);

            var root = model.SyntaxTree.GetRoot(ct);
            var token = root.FindToken(position, findInsideTrivia: true);
            ISymbol? symbol = null;
            for (var node = token.Parent; node is not null; node = node.Parent)
            {
                symbol = model.GetDeclaredSymbol(node, ct) ?? model.GetSymbolInfo(node, ct).Symbol;
                if (symbol is not null) break;
            }
            if (symbol is null)
            {
                _highlightCache[cacheKey] = [];
                return [];
            }

            var spans = new List<TextSpan>();
            foreach (var node in root.DescendantNodes())
            {
                ct.ThrowIfCancellationRequested();
                if (model.GetSymbolInfo(node, ct).Symbol?.Equals(symbol, SymbolEqualityComparer.Default) == true)
                    spans.Add(node.Span);
            }
            TrimCaches(_highlightCache);
            _highlightCache[cacheKey] = spans;
            return spans;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Диагностики по файлу (ошибки и выбранные предупреждения) для минимизации контекста.</summary>
    public IReadOnlyList<Diagnostic> GetDiagnosticsForFile(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return [];
        try
        {
            var text = SourceText.From(sourceText);
            var (comp, tree) = GetOrCreateCompilationAndTree(filePath, text, ct);
            var all = comp.GetDiagnostics(ct);
            var list = new List<Diagnostic>();
            foreach (var d in all)
            {
                if (d.Location.SourceTree?.FilePath != filePath) continue;
                if (d.Severity == DiagnosticSeverity.Error)
                    list.Add(d);
                else if (d.Severity == DiagnosticSeverity.Warning && list.Count < 50)
                    list.Add(d);
            }
            return list;
        }
        catch
        {
            return [];
        }
    }

    private static readonly SymbolDisplayFormat SignatureFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>Сигнатуры публичных типов и членов в файле (одна строка на объявление) для минимизации контекста.</summary>
    public IReadOnlyList<string> GetSignatureStringsForFile(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return [];
        try
        {
            var text = SourceText.From(sourceText);
            var model = GetOrCreateModel(filePath, text, ct);
            var root = model.SyntaxTree.GetRoot(ct);
            var list = new List<string>();
            foreach (var node in root.DescendantNodes())
            {
                ct.ThrowIfCancellationRequested();
                var symbol = model.GetDeclaredSymbol(node, ct);
                if (symbol is null) continue;
                if (symbol.DeclaredAccessibility != Accessibility.Public && symbol.ContainingType?.DeclaredAccessibility != Accessibility.Public)
                    continue;
                var line = symbol.ToDisplayString(SignatureFormat);
                if (string.IsNullOrEmpty(line)) continue;
                list.Add(line);
                if (list.Count >= 200) break;
            }
            return list;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Сбросить кэш при смене решения/файла (опционально).</summary>
    public void InvalidateCache()
    {
        _modelCache.Clear();
        _completionCache.Clear();
        _signatureCache.Clear();
        _highlightCache.Clear();
        lock (_modelCacheLock)
        {
            _modelCacheOrder.Clear();
        }
    }
}
