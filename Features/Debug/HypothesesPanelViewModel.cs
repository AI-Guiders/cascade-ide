using System.Collections.ObjectModel;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Debug;

/// <summary>Вкладка «Гипотезы»: список из <see cref="DebugHypothesesStorage"/>.</summary>
public sealed partial class HypothesesPanelViewModel : ViewModelBase
{
    private readonly Func<string> _getWorkspaceRoot;
    private DispatcherTimer? _persistDebounce;

    public HypothesesPanelViewModel(Func<string> getWorkspaceRoot)
    {
        _getWorkspaceRoot = getWorkspaceRoot;
    }

    /// <summary>Отложенная запись после правки текста (не на каждый символ).</summary>
    public void RequestPersistSoon()
    {
        if (_persistDebounce is null)
        {
            _persistDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            _persistDebounce.Tick += OnPersistDebounceTick;
        }

        _persistDebounce.Stop();
        _persistDebounce.Start();
    }

    private void OnPersistDebounceTick(object? sender, EventArgs e)
    {
        _persistDebounce?.Stop();
        Persist();
    }

    public ObservableCollection<HypothesisRowViewModel> Items { get; } = [];

    /// <summary>Перезагрузить с диска при смене workspace.</summary>
    public void LoadFromWorkspace()
    {
        Items.Clear();
        var root = _getWorkspaceRoot();
        if (string.IsNullOrEmpty(root))
            return;

        var data = DebugHypothesesStorage.Load(root);
        foreach (var r in data.Hypotheses)
        {
            if (string.IsNullOrWhiteSpace(r.Id))
                continue;
            Items.Add(new HypothesisRowViewModel(this, r));
        }
    }

    internal void Persist()
    {
        var ws = _getWorkspaceRoot();
        if (string.IsNullOrEmpty(ws))
            return;

        var root = new DebugHypothesesFileRoot
        {
            Version = 1,
            Hypotheses = Items.Select(h => h.ToRecord()).Where(r => !string.IsNullOrWhiteSpace(r.Id)).ToList(),
        };
        DebugHypothesesStorage.Save(ws, root);
    }

    [RelayCommand]
    private void AddHypothesis()
    {
        Items.Add(new HypothesisRowViewModel(this, new DebugHypothesisRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Text = "",
            Status = DebugHypothesisStatus.Open,
        }));
        Persist();
    }

    internal void RemoveRow(HypothesisRowViewModel row)
    {
        Items.Remove(row);
        Persist();
    }
}

/// <summary>Строка списка гипотез.</summary>
public sealed partial class HypothesisRowViewModel : ViewModelBase
{
    private readonly HypothesesPanelViewModel _owner;

    public HypothesisRowViewModel(HypothesesPanelViewModel owner, DebugHypothesisRecord record)
    {
        _owner = owner;
        _id = record.Id;
        _text = record.Text ?? "";
        _status = record.Status;
    }

    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private DebugHypothesisStatus _status;

    partial void OnTextChanged(string value) => _owner.RequestPersistSoon();

    partial void OnStatusChanged(DebugHypothesisStatus value) => _owner.Persist();

    public DebugHypothesisRecord ToRecord() => new()
    {
        Id = Id,
        Text = Text ?? "",
        Status = Status,
    };

    [RelayCommand]
    private void Remove() => _owner.RemoveRow(this);

    [RelayCommand]
    private void SetOpen() => Status = DebugHypothesisStatus.Open;

    [RelayCommand]
    private void SetRejected() => Status = DebugHypothesisStatus.Rejected;

    [RelayCommand]
    private void SetConfirmed() => Status = DebugHypothesisStatus.Confirmed;
}
