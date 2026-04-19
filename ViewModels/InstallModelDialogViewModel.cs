using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public partial class InstallModelDialogViewModel : ViewModelBase
{
    private readonly Services.IOllamaService _ollama;
    private readonly Action _closeRequested;

    public InstallModelDialogViewModel(Services.IOllamaService ollama, Action closeRequested)
    {
        _ollama = ollama;
        _closeRequested = closeRequested;
        var recommended = Services.MachineInfo.GetRecommendedModels();
        RecommendedModels.Clear();
        foreach (var r in recommended)
            RecommendedModels.Add(r);
    }

    public ObservableCollection<Models.RecommendedModel> RecommendedModels { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private Models.RecommendedModel? _selectedRecommended;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string _customModelName = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool _isPulling;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private string _errorText = "";

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        var model = SelectedRecommended?.ModelName ?? CustomModelName?.Trim() ?? "";
        if (string.IsNullOrEmpty(model))
            return;

        ErrorText = "";
        IsPulling = true;
        ProgressText = $"Скачивание {model}…";

        try
        {
            await foreach (var status in _ollama.PullModelAsync(model, CancellationToken.None))
            {
                var s = status;
                UiScheduler.Default.Post(() => ProgressText = s);
            }
            await UiScheduler.Default.InvokeAsync(() =>
            {
                ProgressText = "Готово.";
                _closeRequested();
            });
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() => ErrorText = ex.Message);
        }
        finally
        {
            await UiScheduler.Default.InvokeAsync(() => IsPulling = false);
        }
    }

    private bool CanInstall() => !IsPulling && (!string.IsNullOrWhiteSpace(CustomModelName) || SelectedRecommended is not null);

    partial void OnCustomModelNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            SelectedRecommended = null;
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeRequested();
    }

    [RelayCommand]
    private void SelectRecommended(Models.RecommendedModel? model)
    {
        if (model is null) return;
        SelectedRecommended = model;
        CustomModelName = "";
    }
}
