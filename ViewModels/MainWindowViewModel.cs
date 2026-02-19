using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AgentIde.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Services.IOllamaService _ollama = new Services.OllamaService();

    [ObservableProperty]
    private bool _ollamaAvailable;

    [ObservableProperty]
    private string _ollamaStatus = "Проверка Ollama…";

    public ObservableCollection<string> OllamaModels { get; } = [];

    public async Task RefreshOllamaAsync()
    {
        OllamaStatus = "Проверка Ollama…";
        OllamaAvailable = await _ollama.IsAvailableAsync();
        if (OllamaAvailable)
        {
            var names = await _ollama.GetModelNamesAsync();
            OllamaModels.Clear();
            foreach (var n in names)
                OllamaModels.Add(n);
            OllamaStatus = names.Count > 0
                ? $"Ollama: {names.Count} моделей"
                : "Ollama запущен, моделей нет (ollama pull <model>)";
        }
        else
        {
            OllamaModels.Clear();
            OllamaStatus = "Ollama недоступен (localhost:11434). Установи и запусти Ollama.";
        }
    }
}
