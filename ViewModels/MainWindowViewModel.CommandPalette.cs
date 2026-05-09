using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.IdeDisplay;
using CascadeIDE.IdeDisplay.CommandPalette;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public enum IdeCommandPaletteRowKind
{
    Command,
    GoTo,
    Hint,
}

public sealed class IdeCommandPaletteRowViewModel : ViewModelBase
{
    public IdeCommandPaletteRowViewModel(
        IdeCommandPaletteCatalog.Entry entry,
        string? hotkeyHint,
        UiModeFamily currentFamily,
        string? melodyAliasTail = null,
        string? argsJsonOverride = null)
    {
        RowKind = IdeCommandPaletteRowKind.Command;
        PaletteId = entry.PaletteId;
        CommandId = entry.CommandId;
        Category = entry.Category;
        if (!string.IsNullOrEmpty(melodyAliasTail))
        {
            // Режим c: — акцент на мелодии (первая строка), не на command_id.
            Title = $"c:{melodyAliasTail}";
            Subtitle = CommandPaletteSubtitleProjection.MelodyPaletteSecondaryLine(entry.Title, entry.CommandId, entry.Category);
        }
        else
        {
            Title = entry.Title;
            Subtitle = CommandPaletteSubtitleProjection.CommandPaletteSubtitle(entry.CommandId, entry.Category);
        }
        IsMelodyAccentRow = !string.IsNullOrEmpty(melodyAliasTail);
        ArgsJson = argsJsonOverride ?? entry.ArgsJson;
        HotkeyHint = hotkeyHint;
        IsAvailable = IdeCommandPaletteMatch.IsEntryAvailable(entry, currentFamily);
        UnavailableHint = IdeCommandPaletteMatch.UnavailableHint(entry, currentFamily);
    }

    /// <summary>Строка навигации (файл / совпадение rg).</summary>
    public IdeCommandPaletteRowViewModel(
        string title,
        string category,
        string fullPath,
        int line,
        int column,
        string prefixHint)
    {
        RowKind = IdeCommandPaletteRowKind.GoTo;
        PaletteId = "__goto__";
        CommandId = "__goto__";
        Title = title;
        Category = category;
        Subtitle = category;
        ArgsJson = null;
        HotkeyHint = prefixHint;
        NavigateFilePath = fullPath;
        NavigateLine = line;
        NavigateColumn = column;
        IsAvailable = true;
        UnavailableHint = null;
        IsMelodyAccentRow = false;
    }

    public IdeCommandPaletteRowViewModel(string title, string category)
    {
        RowKind = IdeCommandPaletteRowKind.Hint;
        PaletteId = "__hint__";
        CommandId = "__hint__";
        Title = title;
        Category = category;
        Subtitle = category;
        ArgsJson = null;
        HotkeyHint = null;
        IsAvailable = false;
        UnavailableHint = null;
        IsMelodyAccentRow = false;
    }

    /// <summary>
    /// Melody: команда исполняется через MCP, но нет строки в <see cref="IdeCommandPaletteCatalog"/> — заголовок из дока протокола.
    /// </summary>
    public IdeCommandPaletteRowViewModel(
        string commandId,
        string melodyAliasTail,
        string titleFromDoc,
        string? hotkeyHint,
        string? argsJson = null)
    {
        RowKind = IdeCommandPaletteRowKind.Command;
        PaletteId = commandId;
        CommandId = commandId;
        Title = $"c:{melodyAliasTail}";
        Category = "ide_execute_command";
        Subtitle = $"{titleFromDoc} · {commandId} · ide_execute_command";
        ArgsJson = argsJson;
        HotkeyHint = hotkeyHint;
        IsAvailable = true;
        UnavailableHint = null;
        IsMelodyAccentRow = true;
    }

    public IdeCommandPaletteRowKind RowKind { get; }

    public bool ShowUnavailableHint => RowKind == IdeCommandPaletteRowKind.Command && !IsAvailable && !string.IsNullOrEmpty(UnavailableHint);

    /// <summary>Строка из режима <c>c:</c> — увеличенный шрифт заголовка в палитре.</summary>
    public bool IsMelodyAccentRow { get; }

    /// <summary>Размер шрифта первой строки: чуть крупнее для мелодии <c>c:</c>.</summary>
    public double PaletteTitleFontSize => IsMelodyAccentRow ? 14.0 : 12.0;

    public string PaletteId { get; }
    public string CommandId { get; }
    public string Title { get; }
    /// <summary>Сырой тег группы из каталога (раздел палитры); для подписи в UI см. <see cref="Subtitle"/>.</summary>
    public string Category { get; }
    /// <summary>Вторая строка палитры: в обычном режиме <c>command_id · раздел</c>; в <c>c:</c> — подпись команды и id под мелодией.</summary>
    public string Subtitle { get; }
    public string? ArgsJson { get; }
    public string? HotkeyHint { get; }
    public bool IsAvailable { get; }
    public string? UnavailableHint { get; }
    public double RowOpacity => IsAvailable ? 1.0 : 0.45;

    public string? NavigateFilePath { get; }
    public int NavigateLine { get; }
    public int NavigateColumn { get; } = 1;

}

/// <summary>Палитра команд.</summary>
public partial class MainWindowViewModel
{
    private const int CommandPalettePageStep = 8;
    private const int GoToPaletteMaxFiles = 100;
    private const int GoToPaletteMaxRipgrep = 80;
    private const int GoToRipgrepDebounceMs = 220;

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

        CommandPaletteSelectedIndex = FilteredCommandPaletteEntries.Count > 0 ? 0 : -1;
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

        var root = TryGetWorkspaceRootForPalette();
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

        if (CommandPaletteSelectedIndex >= FilteredCommandPaletteEntries.Count)
            CommandPaletteSelectedIndex = Math.Max(0, FilteredCommandPaletteEntries.Count - 1);
        RefreshCommandPaletteSurfaceSnapshot();
    }

    private void FillGoToFileEntries(string term)
    {
        var files = McpSolutionTree.CollectFileEntries(Workspace.SolutionRoots).ToList();
        IEnumerable<(string Title, string FullPath)> query = files;
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = files.Where(e =>
                e.Title.Contains(t, StringComparison.OrdinalIgnoreCase)
                || e.FullPath.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var (title, path) in query.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).Take(GoToPaletteMaxFiles))
        {
            var rel = TryRelativePath(path);
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                title,
                rel ?? path,
                path,
                line: 0,
                column: 1,
                prefixHint: "f:"));
        }

        if (FilteredCommandPaletteEntries.Count == 0)
        {
            FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                "Нет файлов по фильтру",
                "Проверь дерево решения или строку поиска."));
        }
    }

    private string? TryRelativePath(string fullPath)
    {
        var root = TryGetWorkspaceRootForPalette();
        if (root is null)
            return null;
        try
        {
            return Path.GetRelativePath(root, fullPath);
        }
        catch
        {
            return null;
        }
    }

    private string? TryGetWorkspaceRootForPalette()
    {
        var sp = Workspace.SolutionPath ?? "";
        if (string.IsNullOrWhiteSpace(sp))
            return null;
        var root = BreakpointsFileService.GetWorkspaceRoot(sp);
        return string.IsNullOrEmpty(root) || !Directory.Exists(root) ? null : root;
    }

    private async Task RunGoToRipgrepAsync(GoToAllQuery query, string workspaceRoot, long seq, CancellationToken ct)
    {
        try
        {
            await Task.Delay(GoToRipgrepDebounceMs, ct).ConfigureAwait(false);
            var (pattern, fixedString, glob) = GoToPaletteRipgrepPatternBuilder.Build(query);
            var (matches, err) = await RipgrepWorkspaceSearchService.SearchMatchesAsync(
                    workspaceRoot,
                    pattern,
                    subPath: null,
                    fixedString,
                    glob,
                    GoToPaletteMaxRipgrep,
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

                var prefix = $"{query.Prefix}:";
                var cat = query.Prefix == 't' ? "t: тип" : query.Prefix == 'm' ? "m: член" : "x: текст";
                foreach (var m in matches)
                {
                    var rel = TryRelativePath(m.Path);
                    var preview = m.LineText.Trim();
                    if (preview.Length > 160)
                        preview = preview[..157] + "…";
                    FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                        preview,
                        rel is not null ? $"{rel} · {m.LineNumber}" : $"{m.Path} · {m.LineNumber}",
                        m.Path,
                        m.LineNumber,
                        1,
                        prefix));
                }

                if (FilteredCommandPaletteEntries.Count == 0)
                {
                    FilteredCommandPaletteEntries.Add(new IdeCommandPaletteRowViewModel(
                        "Ничего не найдено",
                        cat));
                }

                CommandPaletteSelectedIndex = FilteredCommandPaletteEntries.Count > 0 ? 0 : -1;
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
