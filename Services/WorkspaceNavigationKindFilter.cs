#nullable enable

namespace CascadeIDE.Services;

/// <summary>Имена видов связей в ответе <c>get_workspace_navigation_context</c> (режим <c>related</c> / основа <c>subgraph</c>).</summary>
public static class WorkspaceNavigationRelatedKinds
{
    public const string PartialPeer = "partial_peer";
    public const string ProjectPeer = "project_peer";
    public const string XamlCodeBehindPair = "xaml_codebehind_pair";
    public const string TestCounterpart = "test_counterpart";
    public const string SameNamespace = "same_namespace";
    public const string SameDirectory = "same_directory";

    internal static readonly string[] All =
    [
        PartialPeer,
        ProjectPeer,
        XamlCodeBehindPair,
        TestCounterpart,
        SameNamespace,
        SameDirectory
    ];

    /// <summary>Каноническое имя вида или <c>null</c>, если токен не известен.</summary>
    public static string? TryCanonicalKind(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        var s = token.Trim();
        foreach (var k in All)
        {
            if (string.Equals(k, s, StringComparison.OrdinalIgnoreCase))
                return k;
        }

        return null;
    }
}

/// <summary>
/// Фильтр по видам связей: <c>include_kinds</c> — белый список (если задан и непустой);
/// <c>exclude_kinds</c> — вычитание. Неизвестные токены в списках игнорируются; если в <c>include</c> не осталось ни одного известного вида — считается «без ограничения по include».
/// </summary>
public readonly struct WorkspaceNavigationKindFilter
{
    private readonly HashSet<string>? _include;
    private readonly HashSet<string> _exclude;

    private WorkspaceNavigationKindFilter(HashSet<string>? include, HashSet<string> exclude)
    {
        _include = include;
        _exclude = exclude;
    }

    /// <summary><c>null</c> — без белого списка (все виды, кроме исключённых).</summary>
    public IReadOnlyList<string>? EffectiveIncludeKinds =>
        _include is null ? null : _include.OrderBy(x => x, StringComparer.Ordinal).ToList();

    /// <summary>Канонические исключённые виды (может быть пустым).</summary>
    public IReadOnlyList<string> EffectiveExcludeKinds =>
        _exclude.Count == 0 ? Array.Empty<string>() : _exclude.OrderBy(x => x, StringComparer.Ordinal).ToList();

    public static WorkspaceNavigationKindFilter Create(IReadOnlyList<string>? includeKinds, IReadOnlyList<string>? excludeKinds)
    {
        var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (excludeKinds is not null)
        {
            foreach (var t in excludeKinds)
            {
                var c = WorkspaceNavigationRelatedKinds.TryCanonicalKind(t);
                if (c is not null)
                    exclude.Add(c);
            }
        }

        HashSet<string>? include = null;
        if (includeKinds is not null && includeKinds.Count > 0)
        {
            include = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in includeKinds)
            {
                var c = WorkspaceNavigationRelatedKinds.TryCanonicalKind(t);
                if (c is not null)
                    include.Add(c);
            }

            if (include.Count == 0)
                include = null;
        }

        return new WorkspaceNavigationKindFilter(include, exclude);
    }

    public bool Allows(string kind)
    {
        if (string.IsNullOrEmpty(kind))
            return false;
        if (_include is not null && !_include.Contains(kind))
            return false;
        if (_exclude.Contains(kind))
            return false;
        return true;
    }
}
