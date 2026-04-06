using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public sealed class IdeCommandPaletteRowViewModel : ViewModelBase
{
    public IdeCommandPaletteRowViewModel(IdeCommandPaletteCatalog.Entry entry, string? hotkeyHint)
    {
        PaletteId = entry.PaletteId;
        CommandId = entry.CommandId;
        Title = entry.Title;
        Category = entry.Category;
        ArgsJson = entry.ArgsJson;
        HotkeyHint = hotkeyHint;
    }

    public string PaletteId { get; }
    public string CommandId { get; }
    public string Title { get; }
    public string Category { get; }
    public string? ArgsJson { get; }
    public string? HotkeyHint { get; }
}

public partial class MainWindowViewModel
{
    private HotkeyGestureMap? _hotkeyGestureMap;

    private HotkeyGestureMap HotkeyGestureMap => _hotkeyGestureMap ??= HotkeyGestureMap.Load();

    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    [ObservableProperty]
    private string _commandPaletteQuery = "";

    [ObservableProperty]
    private int _commandPaletteSelectedIndex = -1;

    public ObservableCollection<IdeCommandPaletteRowViewModel> FilteredCommandPaletteEntries { get; } = new();

    partial void OnCommandPaletteQueryChanged(string value)
    {
        RefreshCommandPaletteFilter();
        CommandPaletteSelectedIndex = FilteredCommandPaletteEntries.Count > 0 ? 0 : -1;
    }

    [RelayCommand]
    private void ToggleCommandPalette()
    {
        IsCommandPaletteOpen = !IsCommandPaletteOpen;
        if (!IsCommandPaletteOpen)
            return;
        CommandPaletteQuery = "";
        RefreshCommandPaletteFilter();
        CommandPaletteSelectedIndex = FilteredCommandPaletteEntries.Count > 0 ? 0 : -1;
    }

    [RelayCommand]
    private void CloseCommandPalette() => IsCommandPaletteOpen = false;

    [RelayCommand]
    private async Task ExecuteCommandPaletteSelectionAsync()
    {
        if (CommandPaletteSelectedIndex < 0 || CommandPaletteSelectedIndex >= FilteredCommandPaletteEntries.Count)
            return;
        var row = FilteredCommandPaletteEntries[CommandPaletteSelectedIndex];
        IsCommandPaletteOpen = false;
        var args = IdeCommandPaletteCatalog.ParseArgs(row.ArgsJson);
        await _ideMcpExecutor.ExecuteAsync(row.CommandId, args, CancellationToken.None).ConfigureAwait(false);
    }

    [RelayCommand]
    private void CommandPaletteMoveSelection(int delta)
    {
        if (FilteredCommandPaletteEntries.Count == 0)
            return;
        var next = CommandPaletteSelectedIndex + delta;
        if (next < 0)
            next = FilteredCommandPaletteEntries.Count - 1;
        else if (next >= FilteredCommandPaletteEntries.Count)
            next = 0;
        CommandPaletteSelectedIndex = next;
    }

    private void RefreshCommandPaletteFilter()
    {
        var q = CommandPaletteQuery.Trim();
        IEnumerable<IdeCommandPaletteCatalog.Entry> filtered = string.IsNullOrEmpty(q)
            ? IdeCommandPaletteCatalog.All
            : IdeCommandPaletteCatalog.All.Where(Matches);

        bool Matches(IdeCommandPaletteCatalog.Entry e) =>
            e.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || e.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
            || e.CommandId.Contains(q, StringComparison.OrdinalIgnoreCase)
            || e.PaletteId.Contains(q, StringComparison.OrdinalIgnoreCase);

        FilteredCommandPaletteEntries.Clear();
        var hotkeys = HotkeyGestureMap;
        foreach (var e in filtered.OrderBy(x => x.Category).ThenBy(x => x.Title))
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(e, hotkeys.GetDisplayHint(e.CommandId)));

        if (CommandPaletteSelectedIndex >= FilteredCommandPaletteEntries.Count)
            CommandPaletteSelectedIndex = Math.Max(0, FilteredCommandPaletteEntries.Count - 1);
    }
}
