using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Features.Search.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models;
using CascadeIDE.IdeDisplay;
using CascadeIDE.IdeDisplay.CommandPalette;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Палитра команд.</summary>
public partial class MainWindowViewModel
{
    private const int CommandPalettePageStep = 8;

    private HotkeyGestureMap? _hotkeyGestureMap;

    private HotkeyGestureMap HotkeyGestureMap => _hotkeyGestureMap ??= HotkeyGestureMap.Load();

    private readonly CommandPaletteGoToAsyncHandle _commandPaletteGoTo = new();

    private readonly IIdsSurfaceCompositor<CommandPaletteSurfaceIntent, CommandPaletteSurfaceSnapshot> _commandPaletteSurfaceCompositor =
        new CommandPaletteSurfaceCompositor();
    private CommandPaletteSurfaceSnapshot _commandPaletteSurfaceSnapshot = CommandPaletteSurfaceSnapshot.Empty;

    [ObservableProperty]
    public partial bool IsCommandPaletteOpen { get; set; }

    [ObservableProperty]
    public partial string CommandPaletteQuery { get; set; } = "";

    [ObservableProperty]
    public partial int CommandPaletteSelectedIndex { get; set; } = -1;

    /// <summary>IDS v0 снимок оверлея Ctrl+Q (канал -> композитор -> поверхность).</summary>
    public CommandPaletteSurfaceSnapshot CommandPaletteSurfaceSnapshot
    {
        get => _commandPaletteSurfaceSnapshot;
        private set => SetProperty(ref _commandPaletteSurfaceSnapshot, value);
    }

    /// <summary>Подсказка внизу палитры; жест «выделить запрос» из <c>hotkeys.toml</c> (<c>toggle_command_palette</c>).</summary>
    public string CommandPaletteFooterHint
    {
        get
        {
            var h = HotkeyGestureMap.GetDisplayHint("toggle_command_palette");
            var melody = IntentMelodyAliases.SampleAliasesForFooter(8);
            return CommandPaletteChromeProjection.FooterHint(h, melody);
        }
    }

    /// <summary>Плейсхолдер поля поиска; примеры melody из <c>IntentMelody/intent-melody-aliases.toml</c>.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Привязка Avalonia к экземпляру MainWindowViewModel (DataContext).")]
    public string CommandPalettePlaceholderText
    {
        get
        {
            var melody = IntentMelodyAliases.SampleAliasesForFooter(6);
            return CommandPaletteChromeProjection.QueryPlaceholder(melody);
        }
    }

    public ObservableCollection<IdeCommandPaletteRowViewModel> FilteredCommandPaletteEntries { get; } = new();

    partial void OnCommandPaletteQueryChanged(string value) => RefreshCommandPaletteFilter();

    partial void OnCommandPaletteSelectedIndexChanged(int value) => RefreshCommandPaletteSurfaceSnapshot();

    partial void OnIsCommandPaletteOpenChanged(bool value)
    {
        if (!value)
        {
            _commandPaletteGoTo.Cancel();
            CommandPaletteSurfaceSnapshot = CommandPaletteSurfaceSnapshot.Empty;
            return;
        }

        RefreshCommandPaletteSurfaceSnapshot();
    }

    [RelayCommand]
    private void ToggleCommandPalette()
    {
        IsCommandPaletteOpen = !IsCommandPaletteOpen;
        if (!IsCommandPaletteOpen)
            return;
        CommandPaletteQuery = "";
        RefreshCommandPaletteFilter();
        CommandPaletteSelectedIndex = CommandPaletteSelectionProjection.InitialSelectedIndex(
            FilteredCommandPaletteEntries.Count);
    }

    [RelayCommand]
    private void CloseCommandPalette() => IsCommandPaletteOpen = false;

    [RelayCommand]
    private async Task ExecuteCommandPaletteSelectionAsync()
    {
        if (CommandPaletteSelectedIndex < 0 || CommandPaletteSelectedIndex >= FilteredCommandPaletteEntries.Count)
            return;
        var row = FilteredCommandPaletteEntries[CommandPaletteSelectedIndex];
        if (row.RowKind == IdeCommandPaletteRowKind.Hint)
            return;
        if (row.RowKind == IdeCommandPaletteRowKind.GoTo)
        {
            IsCommandPaletteOpen = false;
            if (string.IsNullOrEmpty(row.NavigateFilePath))
                return;
            await IdeCommandPaletteExecutionOrchestrator
                .RunSelectionAsync(row, (Services.IIdeMcpActions)this, _ideMcpExecutor, CancellationToken.None)
                .ConfigureAwait(false);
            return;
        }

        if (!row.IsAvailable)
            return;
        IsCommandPaletteOpen = false;
        await IdeCommandPaletteExecutionOrchestrator
            .RunSelectionAsync(row, (Services.IIdeMcpActions)this, _ideMcpExecutor, CancellationToken.None)
            .ConfigureAwait(false);
    }

    [RelayCommand]
    private void CommandPaletteMoveSelection(int delta)
    {
        if (CommandPaletteSelectionProjection.TryMoveCircular(
                CommandPaletteSelectedIndex,
                delta,
                FilteredCommandPaletteEntries.Count,
                out var next))
            CommandPaletteSelectedIndex = next;
    }

    /// <summary>Прокрутка списка страницей (PgUp/PgDn).</summary>
    [RelayCommand]
    private void CommandPalettePageMove(int directionSign)
    {
        if (CommandPaletteSelectionProjection.TryPageMove(
                CommandPaletteSelectedIndex,
                directionSign,
                CommandPalettePageStep,
                FilteredCommandPaletteEntries.Count,
                out var next))
            CommandPaletteSelectedIndex = next;
    }

    private void RefreshCommandPaletteFilter() =>
        IdeCommandPaletteFilterOrchestrator.RefreshCommandPaletteFilter(
            () => CommandPaletteQuery,
            FilteredCommandPaletteEntries,
            HotkeyGestureMap,
            UiModeFamily,
            Workspace.SolutionPath,
            Workspace.SolutionRoots,
            CurrentFilePath,
            EditorText,
            _commandPaletteGoTo,
            i => CommandPaletteSelectedIndex = i,
            () => CommandPaletteSelectedIndex,
            RefreshCommandPaletteSurfaceSnapshot,
            () => CommandPaletteGoToSearchBackendFactory.Resolve(
                CommandPaletteGoToSearchBackendNormalizer.Parse(_settings.CommandPalette.GoToSearch.Backend),
                _hybridIndex,
                _settings.HybridIndex.ScopeMode,
                _settings.HybridIndex.Enabled));

    /// <summary>Обновить подписи доступности при смене UI-режима с открытой палитрой.</summary>
    private void RefreshCommandPaletteIfOpen()
    {
        if (IsCommandPaletteOpen)
            RefreshCommandPaletteFilter();
    }

    private void RefreshCommandPaletteSurfaceSnapshot()
    {
        if (!IsCommandPaletteOpen)
        {
            CommandPaletteSurfaceSnapshot = CommandPaletteSurfaceSnapshot.Empty;
            return;
        }

        var intent = new CommandPaletteSurfaceIntent(
            CommandPaletteQuery,
            CommandPaletteSelectedIndex,
            FilteredCommandPaletteEntries);
        CommandPaletteSurfaceSnapshot = _commandPaletteSurfaceCompositor.Compose(intent);
    }
}
