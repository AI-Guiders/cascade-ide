using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public sealed class IdeCommandPaletteRowViewModel : ViewModelBase
{
    public IdeCommandPaletteRowViewModel(
        IdeCommandPaletteCatalog.Entry entry,
        string? hotkeyHint,
        UiModeFamily currentFamily)
    {
        PaletteId = entry.PaletteId;
        CommandId = entry.CommandId;
        Title = entry.Title;
        Category = entry.Category;
        ArgsJson = entry.ArgsJson;
        HotkeyHint = hotkeyHint;
        IsAvailable = IdeCommandPaletteMatch.IsEntryAvailable(entry, currentFamily);
        UnavailableHint = IdeCommandPaletteMatch.UnavailableHint(entry, currentFamily);
    }

    public bool ShowUnavailableHint => !IsAvailable && !string.IsNullOrEmpty(UnavailableHint);

    public string PaletteId { get; }
    public string CommandId { get; }
    public string Title { get; }
    public string Category { get; }
    public string? ArgsJson { get; }
    public string? HotkeyHint { get; }
    public bool IsAvailable { get; }
    public string? UnavailableHint { get; }
    public double RowOpacity => IsAvailable ? 1.0 : 0.45;
}

public partial class MainWindowViewModel
{
    private const int CommandPalettePageStep = 8;

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
        if (!row.IsAvailable)
            return;
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

    /// <summary>Прокрутка списка страницей (PgUp/PgDn).</summary>
    [RelayCommand]
    private void CommandPalettePageMove(int directionSign)
    {
        if (FilteredCommandPaletteEntries.Count == 0)
            return;
        var step = CommandPalettePageStep * Math.Sign(directionSign);
        if (step == 0)
            return;
        var next = CommandPaletteSelectedIndex + step;
        CommandPaletteSelectedIndex = Math.Clamp(next, 0, FilteredCommandPaletteEntries.Count - 1);
    }

    private void RefreshCommandPaletteFilter()
    {
        var q = CommandPaletteQuery.Trim();
        var ranked = IdeCommandPaletteMatch.FilterAndRank(IdeCommandPaletteCatalog.All, q);

        FilteredCommandPaletteEntries.Clear();
        var hotkeys = HotkeyGestureMap;
        var family = UiModeFamily;
        foreach (var e in ranked)
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(e, hotkeys.GetDisplayHint(e.CommandId), family));

        if (CommandPaletteSelectedIndex >= FilteredCommandPaletteEntries.Count)
            CommandPaletteSelectedIndex = Math.Max(0, FilteredCommandPaletteEntries.Count - 1);
    }

    /// <summary>Обновить подписи доступности при смене UI-режима с открытой палитрой.</summary>
    private void RefreshCommandPaletteIfOpen()
    {
        if (IsCommandPaletteOpen)
            RefreshCommandPaletteFilter();
    }
}
