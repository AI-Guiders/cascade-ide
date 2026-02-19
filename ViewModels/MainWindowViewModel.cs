using System.Collections.ObjectModel;
using System.Windows.Input;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, Services.IIdeMcpActions
{
    private readonly Services.IOllamaService _ollama = new Services.OllamaService();

    [ObservableProperty]
    private bool _ollamaAvailable;

    [ObservableProperty]
    private string _ollamaStatus = "Проверка Ollama…";

    public ObservableCollection<string> OllamaModels { get; } = [];

    [ObservableProperty]
    private string? _selectedOllamaModel;

    [ObservableProperty]
    private ObservableCollection<SolutionItem> _solutionRoots = [];

    [ObservableProperty]
    private SolutionItem? _selectedSolutionItem;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private string _editorText = "";

    [ObservableProperty]
    private string _solutionPath = "";

    [ObservableProperty]
    private string _solutionLoadError = "";

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private string _chatInput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private bool _isChatLoading;

    partial void OnSelectedOllamaModelChanged(string? value) => (SendChatCommand as IRelayCommand)?.NotifyCanExecuteChanged();

    partial void OnSelectedSolutionItemChanged(SolutionItem? value)
    {
        if (value?.FullPath is { } path && File.Exists(path))
        {
            CurrentFilePath = path;
            try
            {
                EditorText = File.ReadAllText(path);
            }
            catch
            {
                EditorText = "";
            }
        }
    }

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
            SelectedOllamaModel = OllamaModels.FirstOrDefault();
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

    [RelayCommand]
    private async Task OpenSolutionAsync()
    {
        // Будет открыт через диалог из кода представления
        await Task.CompletedTask;
    }

    public void LoadSolution(string path)
    {
        SolutionLoadError = "";
        var root = Services.SolutionParser.Load(path, out var error);
        if (root is null)
        {
            SolutionLoadError = error ?? "Не удалось загрузить решение.";
            return;
        }
        SolutionPath = path;
        SolutionRoots.Clear();
        SolutionRoots.Add(root);
    }

    [RelayCommand(CanExecute = nameof(CanSendChat))]
    private async Task SendChatAsync()
    {
        var input = ChatInput.Trim();
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(SelectedOllamaModel))
            return;

        ChatInput = "";
        ChatMessages.Add(new ChatMessageViewModel("user", input));
        IsChatLoading = true;

        try
        {
            var messages = ChatMessages.Take(ChatMessages.Count - 1)
                .Select(m => new Services.ChatMessage(m.Role, m.Content))
                .Append(new Services.ChatMessage("user", input))
                .ToList();
            var assistantMsg = new ChatMessageViewModel("assistant", "");
            ChatMessages.Add(assistantMsg);

            await foreach (var token in _ollama.StreamChatAsync(SelectedOllamaModel!, messages, CancellationToken.None))
            {
                assistantMsg.Content += token;
            }
        }
        finally
        {
            IsChatLoading = false;
        }
    }

    private bool CanSendChat() => !string.IsNullOrWhiteSpace(ChatInput) && !string.IsNullOrEmpty(SelectedOllamaModel) && !IsChatLoading;

    void Services.IIdeMcpActions.OpenFile(string path)
    {
        if (!File.Exists(path))
            return;
        CurrentFilePath = path;
        try
        {
            EditorText = File.ReadAllText(path);
        }
        catch
        {
            EditorText = "";
        }
    }

    void Services.IIdeMcpActions.SetBreakpoint(string filePath, int line, string? condition)
    {
        // TODO: отобразить в UI (маргина или список брейкпоинтов), передать в debug-mcp
        System.Diagnostics.Debug.WriteLine($"Breakpoint: {filePath}:{line} {condition}");
    }

    void Services.IIdeMcpActions.ShowPreview(string title, string content)
    {
        ChatMessages.Add(new ChatMessageViewModel("preview", $"[{title}]\n{content}"));
    }

    Task<string> Services.IIdeMcpActions.RequestConfirmationAsync(string message, CancellationToken cancellationToken)
    {
        // TODO: модальное окно с Ok/Cancel; пока возвращаем ok
        return Task.FromResult("ok");
    }
}
