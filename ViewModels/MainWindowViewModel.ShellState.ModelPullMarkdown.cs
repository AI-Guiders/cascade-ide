using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Часть <see cref="MainWindowViewModel"/>: pull модели и превью Markdown / Kroki.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallModelCommand))]
    private string _modelToInstall = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallModelCommand))]
    private bool _isPullingModel;

    [ObservableProperty]
    private string _pullModelProgress = "";

    /// <summary>Mermaid/PlantUML в превью Markdown через Kroki (текст диаграммы отправляется на сервер).</summary>
    [ObservableProperty]
    private bool _markdownKrokiEnabled = true;

    /// <summary>Базовый URL Kroki для превью диаграмм.</summary>
    [ObservableProperty]
    private string _markdownKrokiBaseUrl = "https://kroki.io";
}
