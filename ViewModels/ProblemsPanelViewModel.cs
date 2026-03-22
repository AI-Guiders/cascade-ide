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
}

/// <summary>Вкладка «Problems»: список диагностик по открытым .cs.</summary>
public sealed class ProblemsPanelViewModel : ObservableObject
{
    private readonly Action<ProblemListItem> _navigate;

    public ObservableCollection<ProblemListItem> Items { get; } = new();

    public IRelayCommand<ProblemListItem?> NavigateCommand { get; }

    public ProblemsPanelViewModel(Action<ProblemListItem> navigate)
    {
        _navigate = navigate;
        NavigateCommand = new RelayCommand<ProblemListItem?>(item =>
        {
            if (item is not null)
                _navigate(item);
        });
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

    public int ErrorCount
    {
        get
        {
            var n = 0;
            foreach (var i in Items)
            {
                if (string.Equals(i.Severity, "error", StringComparison.OrdinalIgnoreCase))
                    n++;
            }

            return n;
        }
    }

    public int WarningCount
    {
        get
        {
            var n = 0;
            foreach (var i in Items)
            {
                if (string.Equals(i.Severity, "warning", StringComparison.OrdinalIgnoreCase))
                    n++;
            }

            return n;
        }
    }

    public string SummaryText => $"{ErrorCount} ошиб., {WarningCount} предупр., всего {Items.Count}";
}
