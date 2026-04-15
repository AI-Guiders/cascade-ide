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

    partial void OnCurrentSecondaryShellPageChanged(SecondaryShellPage value)
    {
        if (value == SecondaryShellPage.EnvironmentReadiness)
            _ = RefreshEnvironmentReadinessAsync();
    }

    /// <summary>Уйти со страницы готовности окружения на первую другую разрешённую страницу вторичного контура.</summary>
    [RelayCommand]
    private void CloseEnvironmentReadinessPage()
    {
        if (CurrentSecondaryShellPage != SecondaryShellPage.EnvironmentReadiness)
            return;
        foreach (var p in SecondaryShellPageOrder)
        {
            if (p == SecondaryShellPage.EnvironmentReadiness)
                continue;
            if (IsSecondaryShellPageAllowed(p))
            {
                CurrentSecondaryShellPage = p;
                return;
            }
        }

        CurrentSecondaryShellPage = SecondaryShellPage.Chat;
    }

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
