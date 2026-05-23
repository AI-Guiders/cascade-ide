using System.Threading;
using System.Threading.Tasks;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Build.Application;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Сборка, <c>BuildOutputPanel</c>.</summary>
public partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanBuildSolution))]
    private async Task BuildSolutionAsync()
    {
        var prep = MainWindowBuildSolutionPrepProjection.TryCreatePrep(Workspace.SolutionPath);
        if (prep is null)
            return;

        PublishToIdeDataBusAndRebuild(new BuildStateChanged(true));
        IsBuilding = true;
        if (!IsTerminalVisible)
            IsTerminalVisible = true;
        IsBuildOutputVisible = true;

        // Один канонический лог — только «Сборка · вывод» (и MCP get_build_output). Терминал не дублируем:
        // в Power переключаем MFD на вывод сборки, чтобы лог был на глазах без второй копии текста.
        CurrentMfdShellPage = prep.TargetMfdPage;
        BuildOutputPanel.Set(prep.BuildOutputHeader);

        void AppendBuildChunk(string chunk) => BuildOutputPanel.Append(chunk);

        var (lastExitCode, lastBuildSucceeded) =
            await DotnetSolutionChunkedBuildOrchestrator.RunSolutionBuildStreamingAsync(
                    prep.SolutionPath,
                    _dotnetRunner,
                    AppendBuildChunk,
                    CancellationToken.None)
                .ConfigureAwait(true);

        BuildOutputPanel.FlushPending();
        PublishToIdeDataBusAndRebuild(new BuildStateChanged(false, lastExitCode, lastBuildSucceeded));
        IsBuilding = false;
    }

    private bool CanBuildSolution() =>
        MainWindowBuildSolutionPrepProjection.CanBuild(Workspace.SolutionPath, IsBuilding);

    [RelayCommand]
    private void HideBuildOutput()
    {
        IsBuildOutputVisible = false;
    }

    public void LoadSolution(string path)
    {
        _ = LoadSolutionAsync(path);
    }

    /// <summary>Загрузка решения: парсинг .sln/папки в пуле потоков, применение к UI — на UI-потоке.</summary>
    public async Task LoadSolutionAsync(string path)
    {
        await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionLoadError = "").ConfigureAwait(false);

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

                SolutionLoadSessionApplyProjection.ApplySuccessfulLoad(
                    Workspace,
                    root,
                    path,
                    normalizedSolutionPath,
                    IsDockedMfdSolutionExplorerTree,
                    this);
            });
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
                Workspace.SolutionLoadError = "Ошибка загрузки решения: " + ex.Message);
            SolutionLoadCrashLog.TryAppend(path, ex);
        }
    }

    /// <summary>Пустое решение через <c>dotnet new sln</c> по полному пути к будущему <c>.sln</c> (файл не должен существовать).</summary>
    public Task<BlankSolutionCreateResult> TryCreateBlankSolutionAtPathAsync(
        string solutionFilePath,
        CancellationToken cancellationToken = default) =>
        BlankSolutionCreator.TryCreateAsync(solutionFilePath, _dotnetRunner, cancellationToken);

    /// <summary><c>dotnet new</c> + <c>dotnet sln add</c> в текущее решение (ADR 0125).</summary>
    public async Task<string> TryCreateProjectInSolutionAsync(
        string template,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        var result = await ProjectInSolutionCreator.TryCreateAsync(
                Workspace.SolutionPath,
                template,
                projectName,
                _dotnetRunner,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Ok)
            return result.ErrorMessage ?? "Не удалось создать проект.";

        if (!string.IsNullOrWhiteSpace(Workspace.SolutionPath))
            await LoadSolutionAsync(Workspace.SolutionPath).ConfigureAwait(false);

        return System.Text.Json.JsonSerializer.Serialize(
            new { ok = true, project_path = result.ProjectPath },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
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

    void SolutionLoadSessionApplyProjection.IHost.ResetEditorSessionForNewSolution()
    {
        _openFileDebounceCts?.Cancel();
        Documents.ClearForNewSolution();
        CurrentFilePath = null;
        EditorText = "";
        IsLoadingCurrentFile = false;
    }

    void SolutionLoadSessionApplyProjection.IHost.AfterSolutionApplied(MfdShellPage initialMfdPage)
    {
        RefreshStartupProjectAfterSolutionLoad();
        TryNavigateToMfdShellPage(initialMfdPage);
    }
}