using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Holds solution/workspace state (solution path, tree roots, selection) and performs background loading.
/// MainWindowViewModel owns UI-side reactions (opening documents, restarting services, etc.).
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
    /// Loads solution tree in background thread and returns parsed root + normalized path.
    /// Caller should ignore stale results by comparing returned <c>LoadVersion</c>.
    /// </summary>
    /// <remarks>
    /// Вызов с UI-потока: до первого <c>await</c> выполняется сброс <see cref="SolutionLoadError"/> и инкремент версии —
    /// это свойства, связанные с интерфейсом (Avalonia). Вызывать с фона нельзя без маршалинга на UI.
    /// </remarks>
    public async Task<(SolutionItem? Root, string? NormalizedSolutionPath, string? Error, long LoadVersion)> LoadSolutionTreeAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        SolutionLoadError = "";

        try
        {
            var (root, error) = await Task.Run(() =>
            {
                var trimmed = path.Trim();
                string normalizedProbe;
                try
                {
                    normalizedProbe = Path.GetFullPath(trimmed);
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
                try { normalizedSolutionPath = Path.GetFullPath(path); }
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

