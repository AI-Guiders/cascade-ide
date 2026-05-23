using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Workspace;

/// <summary>
/// Состояние сессии решения: путь, дерево, выбор; фоновая загрузка дерева.
/// <see cref="MainWindowViewModel"/> — композитор: документы, редактор, сервисы после загрузки.
/// </summary>
public sealed partial class SolutionWorkspaceViewModel : ViewModelBase
{
    private long _loadVersion;

    [ObservableProperty]
    private string _solutionPath = "";

    [ObservableProperty]
    private string _solutionLoadError = "";

    [ObservableProperty]
    private ObservableCollection<SolutionItem> _solutionRoots = [];

    [ObservableProperty]
    private SolutionItem? _selectedSolutionItem;

    public long CurrentLoadVersion => Interlocked.Read(ref _loadVersion);

    public void Clear()
    {
        SelectedSolutionItem = null;
        SolutionPath = "";
        SolutionLoadError = "";
        SolutionRoots.Clear();
    }

    /// <summary>
    /// Загрузка дерева в пуле потоков; применение к VM — на UI (<see cref="MainWindowViewModel.LoadSolutionAsync"/>).
    /// </summary>
    public async Task<(SolutionItem? Root, string? NormalizedSolutionPath, string? Error, long LoadVersion)> LoadSolutionTreeAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);

        try
        {
            var (root, error) = await Task.Run(() =>
            {
                var trimmed = path.Trim();
                string normalizedProbe;
                try
                {
                    normalizedProbe = CanonicalFilePath.Normalize(trimmed);
                }
                catch
                {
                    normalizedProbe = trimmed;
                }

                if (Directory.Exists(normalizedProbe))
                {
                    var r = FolderWorkspaceTreeBuilder.TryBuild(normalizedProbe, out var err);
                    return (r, err);
                }

                var sln = SolutionParser.Load(trimmed, out var err2);
                return (sln, err2);
            }, cancellationToken).ConfigureAwait(false);

            if (root is null)
                return (null, null, error ?? "Не удалось загрузить решение.", loadVersion);

            var normalizedSolutionPath = root.FullPath;
            if (string.IsNullOrEmpty(normalizedSolutionPath))
            {
                try { normalizedSolutionPath = CanonicalFilePath.Normalize(path); }
                catch { normalizedSolutionPath = path; }
            }

            return (root, normalizedSolutionPath, null, loadVersion);
        }
        catch (OperationCanceledException)
        {
            return (null, null, "Загрузка решения отменена.", loadVersion);
        }
        catch (Exception ex)
        {
            return (null, null, "Ошибка загрузки решения: " + ex.Message, loadVersion);
        }
    }
}
