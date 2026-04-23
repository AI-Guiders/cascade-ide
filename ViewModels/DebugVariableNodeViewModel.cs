using System.Collections.ObjectModel;
using Avalonia.Threading;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

public delegate Task<IReadOnlyList<DebugVariableRow>> ExpandDebugVariableChildrenAsync(
    int variablesReference,
    int? indexedHint,
    int? namedHint,
    CancellationToken cancellationToken);

/// <summary>Узел дерева Locals: scope, переменная или placeholder для lazy expand (chevron при пустом <see cref="Children"/> до загрузки).</summary>
public sealed partial class DebugVariableNodeViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isLoadingChildren;
    private bool _childrenLoadCompleted;
    private readonly DebugVariableRow? _row;
    private readonly ExpandDebugVariableChildrenAsync? _expandChildren;

    private DebugVariableNodeViewModel(
        bool isScope,
        bool isPlaceholder,
        string? scopeTitle,
        DebugVariableRow? row,
        int depth,
        ExpandDebugVariableChildrenAsync? expandChildren)
    {
        IsScope = isScope;
        IsPlaceholder = isPlaceholder;
        SectionTitle = scopeTitle;
        _row = row;
        Depth = depth;
        _expandChildren = expandChildren;
        if (isPlaceholder || isScope)
        {
            if (isScope)
                TypePart = "—";
            return;
        }

        if (row is { } r)
        {
            NamePart = r.Name;
            Value = r.Value;
            TypePart = string.IsNullOrEmpty(r.Type) ? "—" : r.Type;
            if (r.VariablesReference == 0)
                _childrenLoadCompleted = true;
        }
    }

    public bool IsScope { get; }
    public bool IsPlaceholder { get; }
    public int Depth { get; }
    public string? SectionTitle { get; }
    public string NamePart { get; } = "";
    public string Value { get; } = "";
    public string TypePart { get; } = "—";
    public bool IsVariableRow => !IsScope && !IsPlaceholder;
    public ObservableCollection<DebugVariableNodeViewModel> Children { get; } = [];

    public static DebugVariableNodeViewModel CreateScope(
        string title,
        IReadOnlyList<DebugVariableRow> roots,
        ExpandDebugVariableChildrenAsync expandChildren)
    {
        var n = new DebugVariableNodeViewModel(true, false, title, null, 0, expandChildren);
        foreach (var r in roots)
            n.Children.Add(CreateFromRow(r, 0, expandChildren));
        n.IsExpanded = true;
        return n;
    }

    public static DebugVariableNodeViewModel CreateFromRow(
        DebugVariableRow row,
        int depth,
        ExpandDebugVariableChildrenAsync? expandChildren)
    {
        var n = new DebugVariableNodeViewModel(false, false, null, row, depth, expandChildren);
        if (row.VariablesReference != 0)
            n.Children.Add(CreateExpandPlaceholder());
        return n;
    }

    static DebugVariableNodeViewModel CreateExpandPlaceholder() => new(false, true, null, null, 0, null);

    partial void OnIsExpandedChanged(bool value)
    {
        if (!value || IsScope || IsPlaceholder)
            return;
        if (_row is not { } row || row.VariablesReference == 0 || _childrenLoadCompleted)
            return;
        if (Children.Count != 1 || !Children[0].IsPlaceholder)
            return;
        if (_expandChildren == null)
            return;
        _ = LoadVariableChildrenOnExpandAsync();
    }

    async Task LoadVariableChildrenOnExpandAsync()
    {
        if (_row is not { } row || _expandChildren == null)
            return;
        if (IsLoadingChildren)
            return;
        IsLoadingChildren = true;
        IReadOnlyList<DebugVariableRow> rows;
        try
        {
            rows = await _expandChildren(row.VariablesReference, row.IndexedVariables, row.NamedVariables, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            rows = Array.Empty<DebugVariableRow>();
        }

        var expand = _expandChildren;
        var d = Depth;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            Children.Clear();
            foreach (var r in rows)
                Children.Add(CreateFromRow(r, d + 1, expand));
            _childrenLoadCompleted = true;
            IsLoadingChildren = false;
        });
    }
}
