using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanToggleChatPanel))]
    private void ToggleChatPanel()
    {
        IsChatPanelExpanded = !IsChatPanelExpanded;
    }

    private static bool CanToggleChatPanel() => true;

    [RelayCommand(CanExecute = nameof(CanBuildSolution))]
    private async Task BuildSolutionAsync()
    {
        if (string.IsNullOrWhiteSpace(SolutionPath) || !File.Exists(SolutionPath))
            return;

        IsBuilding = true;
        if (!IsTerminalVisible)
            IsTerminalVisible = true;
        IsBuildOutputVisible = true;

        // Power: вкладка «Терминал» — основная консоль кокпита; вывод сборки туда же, иначе пользователь
        // остаётся на терминале и не видит лог (он шёл только в «Сборка · вывод»). В Focus/Balanced — вкладка журнала.
        var mirrorBuildToTerminal = IsPowerMode;
        if (mirrorBuildToTerminal)
            BottomPanelTabIndex = 0;
        else
            BottomPanelTabIndex = 1;

        var header = $"Сборка: {SolutionPath}\r\n";
        BuildOutputPanel.BuildOutput = header;
        if (mirrorBuildToTerminal)
            TerminalPanel.TerminalOutput += $"\r\n=== dotnet build (IDE) ===\r\n{header}";

        void AppendBuildChunk(string chunk)
        {
            BuildOutputPanel.BuildOutput += chunk;
            if (mirrorBuildToTerminal)
                TerminalPanel.TerminalOutput += chunk;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                ArgumentList = { "build", SolutionPath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(SolutionPath) ?? ""
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
            {
                AppendBuildChunk("Не удалось запустить dotnet build.\r\n");
                return;
            }

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr);
            await process.WaitForExitAsync();

            AppendBuildChunk(await stdout + "\r\n" + await stderr);
            if (process.ExitCode != 0)
                AppendBuildChunk($"\r\nКод выхода: {process.ExitCode}");
        }
        catch (Exception ex)
        {
            AppendBuildChunk("Ошибка: " + ex.Message + "\r\n");
        }
        finally
        {
            IsBuilding = false;
        }
    }

    private bool CanBuildSolution() => !string.IsNullOrWhiteSpace(SolutionPath) && File.Exists(SolutionPath) && !IsBuilding;

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
        var loadVersion = Interlocked.Increment(ref _solutionLoadVersion);
        SolutionLoadError = "";
        try
        {
            var (root, error) = await Task.Run(() =>
            {
                var r = Services.SolutionParser.Load(path, out var err);
                return (r, err);
            }).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Ignore stale completion when a newer ide_load_solution call already started.
                if (loadVersion != Interlocked.Read(ref _solutionLoadVersion))
                    return;

                if (root is null)
                {
                    SolutionLoadError = error ?? "Не удалось загрузить решение.";
                    return;
                }

                var normalizedSolutionPath = root.FullPath;
                if (string.IsNullOrEmpty(normalizedSolutionPath))
                {
                    try { normalizedSolutionPath = Path.GetFullPath(path); }
                    catch { normalizedSolutionPath = path; }
                }

                // New solution becomes authoritative UI context: clear stale editor selection/state.
                _openFileDebounceCts?.Cancel();
                SelectedSolutionItem = null;
                OpenDocuments.Clear();
                Group1Documents.Clear();
                Group2Documents.Clear();
                Group3Documents.Clear();
                DockDocuments.Clear();
                DockActiveDocument = null;
                _recentlyClosedDocumentPaths.Clear();
                _recentlyClosedDocumentCount = 0;
                ReopenClosedDocumentCommand.NotifyCanExecuteChanged();
                SelectedDocument = null;
                SelectedDocumentGroup2 = null;
                SelectedDocumentGroup3 = null;
                CurrentFilePath = null;
                EditorText = "";
                IsLoadingCurrentFile = false;

                SolutionPath = normalizedSolutionPath ?? path;
                SolutionRoots.Clear();
                SolutionRoots.Add(root);

                RebuildAndReinitDockLayout();
            });
        }
        catch (Exception ex)
        {
            SolutionLoadError = "Ошибка загрузки решения: " + ex.Message;
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
                Dispatcher.UIThread.Post(() => PullModelProgress = s);
            }
            PullModelProgress = "Готово.";
            await RefreshOllamaAsync();
        }
        catch (Exception ex)
        {
            PullModelProgress = "Ошибка: " + ex.Message;
        }
        finally
        {
            IsPullingModel = false;
        }
    }

    private bool CanInstallModel() => OllamaAvailable && !string.IsNullOrWhiteSpace(ModelToInstall) && !IsPullingModel;
}