using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

/// <summary>
/// Сервис языка C#: автодополнение, подсказки по параметрам, Quick Info, подсветка вхождений.
/// Реализация разнесена по partial в каталоге <c>Services/CSharp/</c>. Работает в фоне, с кэшем.
/// </summary>
public sealed partial class CSharpLanguageService
{
    private static readonly MetadataReference[] DefaultReferences = BuildDefaultReferences();
    private static readonly ConcurrentDictionary<string, SyntaxTree> GlobalUsingsTreeByDirectory = new(StringComparer.OrdinalIgnoreCase);
    // SDK implicit usings emulation for ad-hoc single-file compilation
    // when project-generated GlobalUsings.g.cs is unavailable.
    private const string FallbackGlobalUsingsSource =
        """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;
    private static readonly SyntaxTree DefaultGlobalUsingsTree = BuildFallbackGlobalUsingsTree();

    private const int CacheMaxEntries = 128;
    private const int TextHashCacheMaxEntries = 16;

    private readonly ConcurrentDictionary<(string path, int textHash), (CSharpCompilation comp, SyntaxTree tree)> _modelCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash, int line, int col), IReadOnlyList<CompletionItem>> _completionCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash, int line, int col), string?> _signatureCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash, int line, int col), IReadOnlyList<TextSpan>> _highlightCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash, int line, int col), string?> _quickInfoCache = new();
    private readonly ConcurrentDictionary<(string path, int textHash), IReadOnlyList<EditorTrailingInlayPart>> _inlayHintCache = new();
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

    private static MetadataReference[] BuildDefaultReferences()
    {
        var refs = new List<MetadataReference>(64);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddRef(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (!File.Exists(path))
                return;
            if (!seen.Add(path))
                return;
            refs.Add(MetadataReference.CreateFromFile(path));
        }

        try
        {
            // Core references (works even if TPA list is unavailable).
            AddRef(typeof(object).Assembly.Location);
            AddRef(typeof(Enumerable).Assembly.Location);
            AddRef(typeof(RuntimeHelpers).Assembly.Location);
            AddRef(typeof(Exception).Assembly.Location);
            AddRef(typeof(FileNotFoundException).Assembly.Location);
            AddRef(typeof(Process).Assembly.Location);

            // Richer semantic model for BCL constructor/parameter inlays (e.g., message:).
            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa && !string.IsNullOrWhiteSpace(tpa))
            {
                foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (name is null)
                        continue;
                    if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "mscorlib", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "netstandard", StringComparison.OrdinalIgnoreCase))
                    {
                        AddRef(path);
                    }
                }
            }
        }
        catch
        {
            // Минимальный набор при ошибке
        }
        return refs.ToArray();
    }

    private static MetadataReference[] GetDefaultReferences() => DefaultReferences;

    private static SyntaxTree BuildFallbackGlobalUsingsTree() =>
        CSharpSyntaxTree.ParseText(FallbackGlobalUsingsSource, path: "__cascade_global_usings__.g.cs");

    private static string? FindNearestProjectDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrWhiteSpace(dir))
        {
            try
            {
                if (Directory.EnumerateFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Any())
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static string? TryFindProjectGlobalUsingsPath(string projectDir)
    {
        var objDir = Path.Combine(projectDir, "obj");
        if (!Directory.Exists(objDir))
            return null;
        try
        {
            // Prefer the common debug target first for fresh local edits.
            var preferred = Path.Combine(objDir, "Debug", "net10.0", "GlobalUsings.g.cs");
            if (File.Exists(preferred))
                return preferred;

            var candidates = Directory.EnumerateFiles(objDir, "GlobalUsings.g.cs", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .ToList();
            return candidates.Count == 0 ? null : candidates[0].FullName;
        }
        catch
        {
            return null;
        }
    }

    private static SyntaxTree GetGlobalUsingsTreeForFile(string filePath)
    {
        var sourceDir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(sourceDir))
            return DefaultGlobalUsingsTree;
        if (GlobalUsingsTreeByDirectory.TryGetValue(sourceDir, out var cached))
            return cached;

        var projectDir = FindNearestProjectDirectory(filePath);
        if (string.IsNullOrWhiteSpace(projectDir))
        {
            GlobalUsingsTreeByDirectory[sourceDir] = DefaultGlobalUsingsTree;
            return DefaultGlobalUsingsTree;
        }

        var globalUsingsPath = TryFindProjectGlobalUsingsPath(projectDir);
        if (string.IsNullOrWhiteSpace(globalUsingsPath) || !File.Exists(globalUsingsPath))
        {
            GlobalUsingsTreeByDirectory[sourceDir] = DefaultGlobalUsingsTree;
            return DefaultGlobalUsingsTree;
        }

        try
        {
            var text = File.ReadAllText(globalUsingsPath);
            var tree = CSharpSyntaxTree.ParseText(text, path: globalUsingsPath);
            GlobalUsingsTreeByDirectory[sourceDir] = tree;
            return tree;
        }
        catch
        {
            GlobalUsingsTreeByDirectory[sourceDir] = DefaultGlobalUsingsTree;
            return DefaultGlobalUsingsTree;
        }
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
            var globalUsingsTree = GetGlobalUsingsTreeForFile(filePath);
            var compilation = CSharpCompilation.Create(
                "Temp",
                [globalUsingsTree, tree],
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

    private void TrimInlayCache()
    {
        if (_inlayHintCache.Count <= 128) return;
        var keys = _inlayHintCache.Keys.ToList();
        foreach (var k in keys.Take(keys.Count - 64))
            _inlayHintCache.TryRemove(k, out _);
    }

    /// <summary>Элемент автодополнения.</summary>
    public sealed record CompletionItem(string DisplayText, string InsertText, string? Description = null);

    /// <summary>Сбросить кэш при смене решения/файла (опционально).</summary>
    public void InvalidateCache()
    {
        _modelCache.Clear();
        _completionCache.Clear();
        _signatureCache.Clear();
        _highlightCache.Clear();
        _quickInfoCache.Clear();
        _inlayHintCache.Clear();
        GlobalUsingsTreeByDirectory.Clear();
        lock (_modelCacheLock)
        {
            _modelCacheOrder.Clear();
        }
    }
}
