using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Features.Search.Application;
using CascadeIDE.Features.Shell.Application;
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

    private CancellationTokenSource? _commandPaletteGoToCts;
    private long _commandPaletteGoToSeq;
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
            _commandPaletteGoToCts?.Cancel();
            _commandPaletteGoToCts = null;
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
            if (row.NavigateLine > 0)
                ((Services.IIdeMcpActions)this).GoToPosition(row.NavigateFilePath, row.NavigateLine, row.NavigateColumn, null, null);
            else
                ((Services.IIdeMcpActions)this).OpenFile(row.NavigateFilePath);
            return;
        }

        if (!row.IsAvailable)
            return;
        IsCommandPaletteOpen = false;
        var args = IdeCommandPaletteCatalog.ParseArgs(row.ArgsJson);
        await _ideMcpExecutor.ExecuteAsync(row.CommandId, args, CancellationToken.None).ConfigureAwait(false);
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

    private void RefreshCommandPaletteFilter()
    {
        var raw = CommandPaletteQuery;
        if (IntentMelodyAliases.TryGetTail(raw, out var melodyTail))
        {
            _commandPaletteGoToCts?.Cancel();
            _commandPaletteGoToCts = null;
            RefreshMelodyPaletteFilter(melodyTail);
            return;
        }

        if (GoToAllQueryParser.TryParse(raw) is { } goTo)
        {
            RefreshGoToPaletteFilter(goTo);
            return;
        }

        _commandPaletteGoToCts?.Cancel();
        _commandPaletteGoToCts = null;

        var q = raw.Trim();
        var ranked = IdeCommandPaletteMatch.FilterAndRank(IdeCommandPaletteCatalog.All, q);

        FilteredCommandPaletteEntries.Clear();
        var hotkeys = HotkeyGestureMap;
        var family = UiModeFamily;
        foreach (var e in ranked)
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(e, hotkeys.GetDisplayHint(e.CommandId), family));

        CommandPaletteSelectedIndex = CommandPaletteSelectionProjection.InitialSelectedIndex(
            FilteredCommandPaletteEntries.Count);
        RefreshCommandPaletteSurfaceSnapshot();
    }

    /// <summary>Режим <c>c:</c> (Command Melody) — см. <see cref="IntentMelodyAliases"/>, <see cref="MelodyInterpreter"/>.</summary>
    private void RefreshMelodyPaletteFilter(string tailNormalized)
    {
        FilteredCommandPaletteEntries.Clear();
        var hotkeys = HotkeyGestureMap;
        var family = UiModeFamily;
        if (TryBuildParametricMelodyPlan(tailNormalized) is { } paramPlan)
        {
            foreach (var line in paramPlan.Lines)
            {
                if (line.ToCommandPaletteRow(hotkeys, family) is { } row)
                    FilteredCommandPaletteEntries.Add(row);
            }

            CommandPaletteSelectedIndex = paramPlan.SelectedIndex;
            RefreshCommandPaletteSurfaceSnapshot();
            return;
        }

        var plan = MelodyInterpreter.BuildPalette(tailNormalized);
        foreach (var line in plan.Lines)
        {
            if (line.ToCommandPaletteRow(hotkeys, family) is { } row)
                FilteredCommandPaletteEntries.Add(row);
        }

        CommandPaletteSelectedIndex = plan.SelectedIndex;
        RefreshCommandPaletteSurfaceSnapshot();
    }

    private MelodyPalettePlan? TryBuildParametricMelodyPlan(string tailNormalized)
    {
        if (ParametricIntentMelody.TryParseLineRangeTail(tailNormalized, out var parsed) && parsed is not null)
        {
            if (ParametricIntentMelody.TryBuildExecutionArgs(
                    parsed,
                    CurrentFilePath,
                    EditorText,
                    out var commandId,
                    out var argsJson,
                    out var error))
            {
                return new MelodyPalettePlan(
                    [new MelodyPaletteCommand(parsed.DisplayTail, commandId, argsJson)],
                    0);
            }

            return new MelodyPalettePlan(
                [new MelodyPaletteHint(error, ParametricIntentMelody.BuildAliasUsageHint(parsed.Alias))],
                0);
        }

        var aliasBeforeColon = ParametricIntentMelody.TryGetAliasPrefixBeforeColon(tailNormalized);
        if (!string.IsNullOrEmpty(aliasBeforeColon) && ParametricIntentMelody.IsPaletteOnlyAlias(aliasBeforeColon))
        {
            return new MelodyPalettePlan(
                [new MelodyPaletteHint(
                    ParametricIntentMelody.BuildAliasUsageHint(aliasBeforeColon),
                    ParametricIntentMelody.BuildAliasUsageCategory(aliasBeforeColon))],
                0);
        }

        return null;
    }

    private void RefreshGoToPaletteFilter(GoToAllQuery q)
    {
        _commandPaletteGoToCts?.Cancel();
        _commandPaletteGoToCts = null;

        FilteredCommandPaletteEntries.Clear();

        var root = CommandPaletteGoToWorkspacePresentation.TryResolveRoot(Workspace.SolutionPath);
        if (root is null)
        {
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                "Нет открытого workspace",
                "Открой решение или папку, затем повтори поиск."));
            CommandPaletteSelectedIndex = 0;
            return;
        }

        switch (q.Prefix)
        {
            case 'f':
                FillGoToFileEntries(q.Term);
                break;
            case 't':
            case 'm':
            case 'x':
                if (string.IsNullOrWhiteSpace(q.Term))
                {
                    FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                        "Введи запрос после префикса",
                        q.Prefix == 't' ? "t: тип (по .cs)" : q.Prefix == 'm' ? "m: член (эвристика по .cs)" : "x: текст (ripgrep)"));
                }
                else
                {
                    FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel("Поиск…", "Подожди результат"));
                    _commandPaletteGoToCts = new CancellationTokenSource();
                    var ct = _commandPaletteGoToCts.Token;
                    var seq = ++_commandPaletteGoToSeq;
                    _ = RunGoToRipgrepAsync(q, root, seq, ct);
                }
                break;
        }

        CommandPaletteSelectedIndex = CommandPaletteSelectionProjection.ClampUpperOrKeep(
            CommandPaletteSelectedIndex,
            FilteredCommandPaletteEntries.Count);
        RefreshCommandPaletteSurfaceSnapshot();
    }

    private void FillGoToFileEntries(string term)
    {
        var workspaceRoot =
            CommandPaletteGoToWorkspacePresentation.TryResolveRoot(Workspace.SolutionPath)
            ?? "";
        var files = McpSolutionTree.CollectFileEntries(Workspace.SolutionRoots);
        foreach (var row in CommandPaletteGoToFileNavRowsProjection.EnumerateFiltered(
                     files,
                     term,
                     workspaceRoot,
                     CommandPaletteGoToLimits.MaxFiles))
        {
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                row.Title,
                row.SubtitleCategory,
                row.FullPath,
                row.Line,
                row.Column,
                row.PrefixHint));
        }

        if (FilteredCommandPaletteEntries.Count == 0)
        {
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                "Нет файлов по фильтру",
                "Проверь дерево решения или строку поиска."));
        }
    }

    private async Task RunGoToRipgrepAsync(GoToAllQuery query, string workspaceRoot, long seq, CancellationToken ct)
    {
        try
        {
            await Task.Delay(CommandPaletteGoToLimits.RipgrepDebounceMs, ct).ConfigureAwait(false);
            var (pattern, fixedString, glob) = GoToPaletteRipgrepPatternBuilder.Build(query);
            var (matches, err) = await RipgrepWorkspaceSearchService.SearchMatchesAsync(
                    workspaceRoot,
                    pattern,
                    subPath: null,
                    fixedString,
                    glob,
                    CommandPaletteGoToLimits.MaxRipgrepMatches,
                    rgExecutable: null,
                    ct)
                .ConfigureAwait(false);

            await UiScheduler.Default.InvokeAsync(() =>
            {
                if (seq != _commandPaletteGoToSeq)
                    return;
                if (GoToAllQueryParser.TryParse(CommandPaletteQuery) is not { } cur
                    || cur.Prefix != query.Prefix
                    || !string.Equals(cur.Term, query.Term, StringComparison.Ordinal))
                    return;

                FilteredCommandPaletteEntries.Clear();
                if (err is not null)
                {
                    FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(err, "ripgrep"));
                    CommandPaletteSelectedIndex = 0;
                    return;
                }

                var cat = query.Prefix == 't' ? "t: тип" : query.Prefix == 'm' ? "m: член" : "x: текст";
                foreach (var row in CommandPaletteGoToRipgrepNavRowsProjection.FromMatches(matches, workspaceRoot, query))
                {
                    FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                        row.Title,
                        row.SubtitleCategory,
                        row.FullPath,
                        row.Line,
                        row.Column,
                        row.PrefixHint));
                }

                if (FilteredCommandPaletteEntries.Count == 0)
                {
                    FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                        "Ничего не найдено",
                        cat));
                }

                CommandPaletteSelectedIndex = CommandPaletteSelectionProjection.InitialSelectedIndex(
                    FilteredCommandPaletteEntries.Count);
                RefreshCommandPaletteSurfaceSnapshot();
            });
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

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
