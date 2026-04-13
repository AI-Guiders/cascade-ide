using System.Collections.ObjectModel;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Снимок «готовность окружения» (ADR 0023), отдельно от Workspace Health.</summary>
public partial class MainWindowViewModel
{
    public ObservableCollection<EnvironmentReadinessItem> EnvironmentReadinessItems { get; } = [];

    [ObservableProperty]
    private string _environmentReadinessUpdatedText = "";

    /// <summary>Полноэкранная страница поверх вторичного контура (v1 — колонка зоны Mfd); открытие — палитра команд.</summary>
    [ObservableProperty]
    private bool _showEnvironmentReadinessPage;

    partial void OnShowEnvironmentReadinessPageChanged(bool value)
    {
        if (value)
            _ = RefreshEnvironmentReadinessAsync();
    }

    [RelayCommand]
    private void CloseEnvironmentReadinessPage() => ShowEnvironmentReadinessPage = false;

    [RelayCommand]
    private async Task RefreshEnvironmentReadinessAsync()
    {
        var rows = await EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync(
            _settings,
            Workspace.SolutionPath,
            _csharpLspHost,
            _markdownLspHost).ConfigureAwait(false);

        await UiScheduler.Default.InvokeAsync(() =>
        {
            EnvironmentReadinessItems.Clear();
            foreach (var row in rows)
                EnvironmentReadinessItems.Add(row);
            EnvironmentReadinessUpdatedText = $"Обновлено: {DateTime.Now:HH:mm:ss}";
        });
    }
}
