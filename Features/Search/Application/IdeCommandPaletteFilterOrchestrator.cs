#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Фильтрация и наполнение списка палитры команд (каталог, melody, go-to).</summary>
internal static class IdeCommandPaletteFilterOrchestrator
{
    public static void RefreshCommandPaletteFilter(
        Func<string> getCommandPaletteQuery,
        ObservableCollection<IdeCommandPaletteRowViewModel> filteredEntries,
        HotkeyGestureMap hotkeyGestureMap,
        UiModeFamily uiModeFamily,
        string? workspaceSolutionPath,
        ObservableCollection<SolutionItem> solutionRoots,
        string? currentFilePath,
        string editorText,
        CommandPaletteGoToAsyncHandle goToHandle,
        Action<int> setSelectedIndex,
        Func<int> getSelectedIndex,
        Action refreshCommandPaletteSurfaceSnapshot)
    {
        var raw = getCommandPaletteQuery();
        if (IntentMelodyAliases.TryGetTail(raw, out var melodyTail))
        {
            goToHandle.Cancel();
            RefreshMelodyPaletteFilter(
                melodyTail,
                filteredEntries,
                hotkeyGestureMap,
                uiModeFamily,
                currentFilePath,
                editorText,
                setSelectedIndex,
                refreshCommandPaletteSurfaceSnapshot);
            return;
        }

        if (GoToAllQueryParser.TryParse(raw) is { } goTo)
        {
            RefreshGoToPaletteFilter(
                goTo,
                filteredEntries,
                workspaceSolutionPath,
                solutionRoots,
                goToHandle,
                setSelectedIndex,
                getSelectedIndex,
                refreshCommandPaletteSurfaceSnapshot,
                getCommandPaletteQuery);
            return;
        }

        goToHandle.Cancel();

        var q = raw.Trim();
        var ranked = IdeCommandPaletteMatch.FilterAndRank(IdeCommandPaletteCatalog.All, q);

        filteredEntries.Clear();
        var hotkeys = hotkeyGestureMap;
        var family = uiModeFamily;
        foreach (var e in ranked)
            filteredEntries.Add(new IdeCommandPaletteRowViewModel(e, hotkeys.GetDisplayHint(e.CommandId), family));

        setSelectedIndex(CommandPaletteSelectionProjection.InitialSelectedIndex(filteredEntries.Count));
        refreshCommandPaletteSurfaceSnapshot();
    }

    private static void RefreshMelodyPaletteFilter(
        string tailNormalized,
        ObservableCollection<IdeCommandPaletteRowViewModel> filteredEntries,
        HotkeyGestureMap hotkeyGestureMap,
        UiModeFamily uiModeFamily,
        string? currentFilePath,
        string editorText,
        Action<int> setSelectedIndex,
        Action refreshCommandPaletteSurfaceSnapshot)
    {
        filteredEntries.Clear();
        var hotkeys = hotkeyGestureMap;
        var family = uiModeFamily;
        if (TryBuildParametricMelodyPlan(tailNormalized, currentFilePath, editorText) is { } paramPlan)
        {
            foreach (var line in paramPlan.Lines)
            {
                if (line.ToCommandPaletteRow(hotkeys, family) is { } row)
                    filteredEntries.Add(row);
            }

            setSelectedIndex(paramPlan.SelectedIndex);
            refreshCommandPaletteSurfaceSnapshot();
            return;
        }

        var plan = MelodyInterpreter.BuildPalette(tailNormalized);
        foreach (var line in plan.Lines)
        {
            if (line.ToCommandPaletteRow(hotkeys, family) is { } row)
                filteredEntries.Add(row);
        }

        setSelectedIndex(plan.SelectedIndex);
        refreshCommandPaletteSurfaceSnapshot();
    }

    private static MelodyPalettePlan? TryBuildParametricMelodyPlan(
        string tailNormalized,
        string? currentFilePath,
        string editorText)
    {
        if (ParametricIntentMelody.TryParseLineRangeTail(tailNormalized, out var parsed) && parsed is not null)
        {
            if (ParametricIntentMelody.TryBuildExecutionArgs(
                    parsed,
                    currentFilePath,
                    editorText,
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

    private static void RefreshGoToPaletteFilter(
        GoToAllQuery q,
        ObservableCollection<IdeCommandPaletteRowViewModel> filteredEntries,
        string? workspaceSolutionPath,
        ObservableCollection<SolutionItem> solutionRoots,
        CommandPaletteGoToAsyncHandle goToHandle,
        Action<int> setSelectedIndex,
        Func<int> getSelectedIndex,
        Action refreshCommandPaletteSurfaceSnapshot,
        Func<string> getCommandPaletteQuery)
    {
        goToHandle.Cancel();

        filteredEntries.Clear();

        var root = CommandPaletteGoToWorkspacePresentation.TryResolveRoot(workspaceSolutionPath);
        if (root is null)
        {
            filteredEntries.Add(new IdeCommandPaletteRowViewModel(
                "Нет открытого workspace",
                "Открой решение или папку, затем повтори поиск."));
            setSelectedIndex(0);
            return;
        }

        switch (q.Prefix)
        {
            case 'f':
                FillGoToFileEntries(q.Term, filteredEntries, workspaceSolutionPath, solutionRoots);
                break;
            case 't':
            case 'm':
            case 'x':
                if (string.IsNullOrWhiteSpace(q.Term))
                {
                    filteredEntries.Add(new IdeCommandPaletteRowViewModel(
                        "Введи запрос после префикса",
                        q.Prefix == 't' ? "t: тип (по .cs)" : q.Prefix == 'm' ? "m: член (эвристика по .cs)" : "x: текст (ripgrep)"));
                }
                else
                {
                    filteredEntries.Add(new IdeCommandPaletteRowViewModel("Поиск…", "Подожди результат"));
                    goToHandle.Cts = new CancellationTokenSource();
                    var ct = goToHandle.Cts.Token;
                    var seq = ++goToHandle.Seq;
                    _ = RunGoToRipgrepAsync(
                        q,
                        root,
                        seq,
                        () => goToHandle.Seq,
                        getCommandPaletteQuery,
                        filteredEntries,
                        setSelectedIndex,
                        refreshCommandPaletteSurfaceSnapshot,
                        ct);
                }

                break;
        }

        setSelectedIndex(CommandPaletteSelectionProjection.ClampUpperOrKeep(
            getSelectedIndex(),
            filteredEntries.Count));
        refreshCommandPaletteSurfaceSnapshot();
    }

    private static void FillGoToFileEntries(
        string term,
        ObservableCollection<IdeCommandPaletteRowViewModel> filteredEntries,
        string? workspaceSolutionPath,
        ObservableCollection<SolutionItem> solutionRoots)
    {
        var workspaceRoot =
            CommandPaletteGoToWorkspacePresentation.TryResolveRoot(workspaceSolutionPath)
            ?? "";
        var files = McpSolutionTree.CollectFileEntries(solutionRoots);
        foreach (var row in CommandPaletteGoToFileNavRowsProjection.EnumerateFiltered(
                     files,
                     term,
                     workspaceRoot,
                     CommandPaletteGoToLimits.MaxFiles))
        {
            filteredEntries.Add(new IdeCommandPaletteRowViewModel(
                row.Title,
                row.SubtitleCategory,
                row.FullPath,
                row.Line,
                row.Column,
                row.PrefixHint));
        }

        if (filteredEntries.Count == 0)
        {
            filteredEntries.Add(new IdeCommandPaletteRowViewModel(
                "Нет файлов по фильтру",
                "Проверь дерево решения или строку поиска."));
        }
    }

    public static async Task RunGoToRipgrepAsync(
        GoToAllQuery query,
        string workspaceRoot,
        long seq,
        Func<long> getLiveGoToSeq,
        Func<string> getCommandPaletteQuery,
        ObservableCollection<IdeCommandPaletteRowViewModel> filteredEntries,
        Action<int> setSelectedIndex,
        Action refreshCommandPaletteSurfaceSnapshot,
        CancellationToken ct)
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
                if (seq != getLiveGoToSeq())
                    return;
                if (GoToAllQueryParser.TryParse(getCommandPaletteQuery()) is not { } cur
                    || cur.Prefix != query.Prefix
                    || !string.Equals(cur.Term, query.Term, StringComparison.Ordinal))
                    return;

                filteredEntries.Clear();
                if (err is not null)
                {
                    filteredEntries.Add(new IdeCommandPaletteRowViewModel(err, "ripgrep"));
                    setSelectedIndex(0);
                    return;
                }

                var cat = query.Prefix == 't' ? "t: тип" : query.Prefix == 'm' ? "m: член" : "x: текст";
                foreach (var row in CommandPaletteGoToRipgrepNavRowsProjection.FromMatches(matches, workspaceRoot, query))
                {
                    filteredEntries.Add(new IdeCommandPaletteRowViewModel(
                        row.Title,
                        row.SubtitleCategory,
                        row.FullPath,
                        row.Line,
                        row.Column,
                        row.PrefixHint));
                }

                if (filteredEntries.Count == 0)
                {
                    filteredEntries.Add(new IdeCommandPaletteRowViewModel(
                        "Ничего не найдено",
                        cat));
                }

                setSelectedIndex(CommandPaletteSelectionProjection.InitialSelectedIndex(filteredEntries.Count));
                refreshCommandPaletteSurfaceSnapshot();
            });
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }
}
