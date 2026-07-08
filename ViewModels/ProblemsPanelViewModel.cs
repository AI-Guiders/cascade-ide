using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public sealed record ProblemListItem(
    string FilePath,
    int Line,
    int Column,
    string Severity,
    string Id,
    string Message)
{
    public string FileName => Path.GetFileName(FilePath);

    public string HeaderLine => $"{Severity} {FileName}({Line},{Column}) {Id}";

    /// <summary>Для шаблона MFD/Problems: боковой акцент (ошибка / предупр.).</summary>
    public bool IsError => string.Equals(Severity, "error", StringComparison.OrdinalIgnoreCase);

    public bool IsWarning => !IsError;
}

/// <summary>Вкладка «Problems»: список диагностик по открытым .cs.</summary>
public sealed partial class ProblemsPanelViewModel : ObservableObject
{
    private readonly Action<ProblemListItem> _navigate;
    private readonly Action<ProblemListItem>? _attachToIntercom;

    public ObservableCollection<ProblemListItem> Items { get; } = new();

    public IRelayCommand<ProblemListItem?> NavigateCommand { get; }

    public IRelayCommand<ProblemListItem?> AttachToIntercomCommand { get; }

    public ProblemsPanelViewModel(Action<ProblemListItem> navigate, Action<ProblemListItem>? attachToIntercom = null)
    {
        _navigate = navigate;
        _attachToIntercom = attachToIntercom;
        NavigateCommand = new RelayCommand<ProblemListItem?>(item =>
        {
            if (item is not null)
                _navigate(item);
        });
        AttachToIntercomCommand = new RelayCommand<ProblemListItem?>(item =>
        {
            if (item is not null)
                _attachToIntercom?.Invoke(item);
        }, item => item is not null && _attachToIntercom is not null);
    }

    internal void ReplaceItems(IReadOnlyList<ProblemListItem> rows)
    {
        Items.Clear();
        foreach (var r in rows)
            Items.Add(r);
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(SummaryText));
    }


    public string SummaryText => $"{ErrorCount} ошиб., {WarningCount} предупр., всего {Items.Count}";
}
