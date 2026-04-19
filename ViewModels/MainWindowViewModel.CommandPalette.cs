using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CascadeIDE.Features.UiChrome;
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
        string? melodyAliasTail = null)
    {
        RowKind = IdeCommandPaletteRowKind.Command;
        PaletteId = entry.PaletteId;
        CommandId = entry.CommandId;
        Title = entry.Title;
        Category = string.IsNullOrEmpty(melodyAliasTail)
            ? entry.Category
            : $"c:{melodyAliasTail} · {entry.Category}";
        ArgsJson = entry.ArgsJson;
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
        ArgsJson = null;
        HotkeyHint = prefixHint;
        NavigateFilePath = fullPath;
        NavigateLine = line;
        NavigateColumn = column;
        IsAvailable = true;
        UnavailableHint = null;
    }

    public IdeCommandPaletteRowViewModel(string title, string category)
    {
        RowKind = IdeCommandPaletteRowKind.Hint;
        PaletteId = "__hint__";
        CommandId = "__hint__";
        Title = title;
        Category = category;
        ArgsJson = null;
        HotkeyHint = null;
        IsAvailable = false;
        UnavailableHint = null;
    }

    public IdeCommandPaletteRowKind RowKind { get; }

    public bool ShowUnavailableHint => RowKind == IdeCommandPaletteRowKind.Command && !IsAvailable && !string.IsNullOrEmpty(UnavailableHint);

    public string PaletteId { get; }
    public string CommandId { get; }
    public string Title { get; }
    public string Category { get; }
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

    [ObservableProperty]
    public partial bool IsCommandPaletteOpen { get; set; }

    [ObservableProperty]
    public partial string CommandPaletteQuery { get; set; } = "";

    [ObservableProperty]
    public partial int CommandPaletteSelectedIndex { get; set; } = -1;

    /// <summary>Подсказка внизу палитры; жест «выделить запрос» из <c>hotkeys.toml</c> (<c>toggle_command_palette</c>).</summary>
    public string CommandPaletteFooterHint
    {
        get
        {
            var h = HotkeyGestureMap.GetDisplayHint("toggle_command_palette");
            var nav = "f: файл · t: тип · m: член · x: текст · c: melody (gs, br, …)";
            return !string.IsNullOrEmpty(h)
                ? $"↑↓ выбор · Enter выполнить · Esc закрыть · PgUp/PgDn страница · {h} выделить запрос · {nav}"
                : $"↑↓ выбор · Enter выполнить · Esc закрыть · PgUp/PgDn страница · {nav}";
        }
    }

    public ObservableCollection<IdeCommandPaletteRowViewModel> FilteredCommandPaletteEntries { get; } = new();

    partial void OnCommandPaletteQueryChanged(string value) => RefreshCommandPaletteFilter();

    partial void OnIsCommandPaletteOpenChanged(bool value)
    {
        if (!value)
        {
            _commandPaletteGoToCts?.Cancel();
            _commandPaletteGoToCts = null;
        }
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
    }

    /// <summary>Режим <c>c:</c> (Command Melody) — см. <see cref="IntentMelodyAliases"/>, <see cref="MelodyInterpreter"/>.</summary>
    private void RefreshMelodyPaletteFilter(string tailNormalized)
    {
        FilteredCommandPaletteEntries.Clear();
        var hotkeys = HotkeyGestureMap;
        var family = UiModeFamily;
        var plan = MelodyInterpreter.BuildPalette(tailNormalized);
        foreach (var line in plan.Lines)
        {
            if (line.ToCommandPaletteRow(hotkeys, family) is { } row)
                FilteredCommandPaletteEntries.Add(row);
        }

        CommandPaletteSelectedIndex = plan.SelectedIndex;
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
            var (pattern, fixedString, glob) = BuildRipgrepPatternForGoTo(query);
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
            });
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    private static (string Pattern, bool FixedString, string? Glob) BuildRipgrepPatternForGoTo(GoToAllQuery q)
    {
        var term = q.Term.Trim();
        switch (q.Prefix)
        {
            case 'x':
                return (term, true, null);
            case 't':
            {
                var esc = Regex.Escape(term);
                var pattern = $@"(class|interface|enum|record|struct)\s+\S*{esc}\S*";
                return (pattern, false, "*.cs");
            }
            case 'm':
            {
                var esc = Regex.Escape(term);
                var pattern = $@"\b{esc}\b\s*\(";
                return (pattern, false, "*.cs");
            }
            default:
                return (term, true, null);
        }
    }

    /// <summary>Обновить подписи доступности при смене UI-режима с открытой палитрой.</summary>
    private void RefreshCommandPaletteIfOpen()
    {
        if (IsCommandPaletteOpen)
            RefreshCommandPaletteFilter();
    }
}
