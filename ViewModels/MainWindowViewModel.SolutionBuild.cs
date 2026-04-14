using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Сборка, <c>BuildOutputPanel</c>.</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private void ToggleChatPanel()
    {
        IsChatPanelExpanded = !IsChatPanelExpanded;
    }

    [RelayCommand(CanExecute = nameof(CanBuildSolution))]
    private async Task BuildSolutionAsync()
    {
        var solutionPath = Workspace.SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return;

        IsBuilding = true;
        if (!IsTerminalVisible)
            IsTerminalVisible = true;
        IsBuildOutputVisible = true;

        // Один канонический лог — только «Сборка · вывод» (и MCP get_build_output). Терминал не дублируем:
        // в Power переключаем MFD на вывод сборки, чтобы лог был на глазах без второй копии текста.
        CurrentSecondaryShellPage = SecondaryShellPage.Build;

        var header = $"Сборка: {solutionPath}\r\n";
        BuildOutputPanel.Set(header);

        void AppendBuildChunk(string chunk) => BuildOutputPanel.Append(chunk);

        try
        {
            var workDir = Path.GetDirectoryName(solutionPath) ?? "";
            // Без ConfigureAwait(false): иначе после await — пул потоков, finally с IsBuilding и вывод
            // в панель идут с фона → Avalonia: Call from invalid thread.
            var (success, exitCode, output) = await _dotnetRunner.RunAsync(["build", solutionPath], workDir);

            AppendBuildChunk(output + "\r\n");
            if (!success && exitCode != 0)
                AppendBuildChunk($"\r\nКод выхода: {exitCode}");
        }
        catch (Exception ex)
        {
            AppendBuildChunk("Ошибка: " + ex.Message + "\r\n");
        }
        finally
        {
            BuildOutputPanel.FlushPending();
            IsBuilding = false;
        }
    }

    private bool CanBuildSolution() =>
        !string.IsNullOrWhiteSpace(Workspace.SolutionPath)
        && File.Exists(Workspace.SolutionPath)
        && !IsBuilding;

    [RelayCommand]
    private void HideBuildOutput()
    {
        IsBuildOutputVisible = false;
    }

    public void LoadSolution(string path)
    {
        _ = LoadSolutionAsync(path);
    }

    /// <summary>Загрузка решения в фоне, чтобы не блокировать UI.</summary>
    public async Task LoadSolutionAsync(string path)
    {
        Workspace.SolutionLoadError = "";
        try
        {
            var (root, normalizedSolutionPath, error, workspaceLoadVersion) =
                await Workspace.LoadSolutionTreeAsync(path).ConfigureAwait(false);

            await UiScheduler.Default.InvokeAsync(() =>
            {
                if (workspaceLoadVersion != Workspace.CurrentLoadVersion)
                    return;

                if (root is null)
                {
                    Workspace.SolutionLoadError = error ?? "Не удалось загрузить решение.";
                    return;
                }

                // New solution becomes authoritative UI context: clear stale editor selection/state.
                _openFileDebounceCts?.Cancel();
                Workspace.SelectedSolutionItem = null;
                Documents.ClearForNewSolution();
                CurrentFilePath = null;
                EditorText = "";
                IsLoadingCurrentFile = false;

                Workspace.SolutionPath = normalizedSolutionPath ?? path;
                Workspace.SolutionRoots.Clear();
                Workspace.SolutionRoots.Add(root);
                RefreshStartupProjectAfterSolutionLoad();
            });
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
                Workspace.SolutionLoadError = "Ошибка загрузки решения: " + ex.Message);
            TryLogLoadSolutionCrash(path, ex);
        }
    }

    private void TryLogLoadSolutionCrash(string? solutionPath, Exception ex)
    {
        try
        {
            var baseDir = "";
            if (!string.IsNullOrWhiteSpace(solutionPath))
            {
                try
                {
                    var full = Path.GetFullPath(solutionPath);
                    baseDir = File.Exists(full) ? (Path.GetDirectoryName(full) ?? "") : full;
                }
                catch
                {
                    baseDir = "";
                }
            }

            if (string.IsNullOrWhiteSpace(baseDir))
                baseDir = Environment.CurrentDirectory;

            var logDir = Path.Combine(baseDir, ".cascade-ide");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "crash-log.txt");
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'");
            var payload =
                $"[{stamp}] LoadSolution crash{Environment.NewLine}" +
                $"solution: {solutionPath}{Environment.NewLine}" +
                $"{ex}{Environment.NewLine}" +
                $"---{Environment.NewLine}";
            File.AppendAllText(logPath, payload);
        }
        catch
        {
            // Do not throw from crash logger.
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstallModel))]
    private async Task InstallModelAsync()
    {
        var model = ModelToInstall?.Trim() ?? "";
        if (string.IsNullOrEmpty(model) || !OllamaAvailable)
            return;

        IsPullingModel = true;
        PullModelProgress = $"Скачивание {model}…";

        try
        {
            await foreach (var status in _ollama.PullModelAsync(model, CancellationToken.None))
            {
                var s = status;
                UiScheduler.Default.Post(() => PullModelProgress = s);
            }
            // IAsyncEnumerable после цикла может продолжиться не на UI — как у сборки без нужного контекста.
            await UiScheduler.Default.InvokeAsync(async () =>
            {
                PullModelProgress = "Готово.";
                await RefreshOllamaAsync();
            });
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
                PullModelProgress = "Ошибка: " + ex.Message);
        }
        finally
        {
            await UiScheduler.Default.InvokeAsync(() => IsPullingModel = false);
        }
    }

    private bool CanInstallModel() => OllamaAvailable && !string.IsNullOrWhiteSpace(ModelToInstall) && !IsPullingModel;
}