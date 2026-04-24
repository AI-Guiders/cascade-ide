#nullable enable
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Селектор launch profile, импорт <c>launchSettings.json</c> (ADR 0090).</summary>
public partial class MainWindowViewModel
{
    public ObservableCollection<string> LaunchProfileIds { get; } = new();

    [ObservableProperty]
    private string? _selectedLaunchProfileId;

    private bool _suspendLaunchProfileSelectionPersistence;

    /// <summary>Есть ≥1 именованного профиля в <c>launch-profiles.toml</c>.</summary>
    public bool ShowLaunchProfilePicker => LaunchProfileIds.Count > 0;

    partial void OnSelectedLaunchProfileIdChanged(string? value)
    {
        if (_suspendLaunchProfileSelectionPersistence)
            return;
        var sln = Workspace.SolutionPath;
        if (string.IsNullOrEmpty(sln) || string.IsNullOrEmpty(value))
            return;
        _ = Services.LaunchProfilesStore.TrySetActiveProfile(sln, value, out _);
        OnPropertyChanged(nameof(StartupProjectBanner));
    }

    public void RefreshLaunchProfilePickerFromStore()
    {
        _suspendLaunchProfileSelectionPersistence = true;
        try
        {
            LaunchProfileIds.Clear();
            var sln = Workspace.SolutionPath;
            if (string.IsNullOrEmpty(sln))
            {
                SelectedLaunchProfileId = null;
                return;
            }

            if (!Services.LaunchProfilesStore.TryGetOrderedProfileIds(sln, out var names, out _))
            {
                SelectedLaunchProfileId = null;
                return;
            }

            foreach (var n in names)
                LaunchProfileIds.Add(n);

            if (Services.LaunchProfilesStore.TryGetActiveProfileName(sln, out var active, out _) && !string.IsNullOrEmpty(active))
            {
                var match = LaunchProfileIds.FirstOrDefault(x => string.Equals(x, active, StringComparison.OrdinalIgnoreCase));
                SelectedLaunchProfileId = match ?? LaunchProfileIds[0];
            }
            else
                SelectedLaunchProfileId = LaunchProfileIds[0];
        }
        finally
        {
            _suspendLaunchProfileSelectionPersistence = false;
        }

        OnPropertyChanged(nameof(ShowLaunchProfilePicker));
        OnPropertyChanged(nameof(StartupProjectBanner));
    }

    [RelayCommand(CanExecute = nameof(CanImportLaunchSettingsFromSelection))]
    private async Task ImportLaunchSettingsFromSelectionAsync()
    {
        var item = Workspace.SelectedSolutionItem;
        var path = item?.FullPath;
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return;

        var sln = Workspace.SolutionPath;
        if (string.IsNullOrEmpty(sln))
            return;

        var solutionDir = Services.BreakpointsFileService.GetWorkspaceRoot(sln);
        if (string.IsNullOrEmpty(solutionDir))
            return;

        var full = Path.GetFullPath(path);
        string rel;
        try
        {
            rel = Path.GetRelativePath(solutionDir, full);
            if (rel.StartsWith("..", StringComparison.Ordinal))
            {
                await ShowDebugInfoAsync("launchSettings.json", "Проект должен быть внутри каталога решения.").ConfigureAwait(false);
                return;
            }
        }
        catch (Exception ex)
        {
            await ShowDebugInfoAsync("launchSettings.json", ex.Message).ConfigureAwait(false);
            return;
        }

        if (!Services.LaunchProfilesStore.TryImportFromLaunchSettings(sln, rel, out var n, out var err) || !string.IsNullOrEmpty(err))
        {
            await ShowDebugInfoAsync("Импорт launch profiles", err ?? "import_failed").ConfigureAwait(false);
            return;
        }

        RefreshLaunchProfilePickerFromStore();
        await ShowDebugInfoAsync("Импорт launch profiles", $"Скопировано профилей (Kestrel/Project) в {Services.LaunchProfilesStore.FileName}: {n}.").ConfigureAwait(false);
    }

    private bool CanImportLaunchSettingsFromSelection() =>
        !string.IsNullOrEmpty(Workspace.SolutionPath) &&
        !string.IsNullOrEmpty(Workspace.SelectedSolutionItem?.FullPath) &&
        Workspace.SelectedSolutionItem!.FullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
}
