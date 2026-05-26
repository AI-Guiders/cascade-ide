using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

using CascadeIDE.Features.Chat;

namespace CascadeIDE.ViewModels;

/// <summary>Часть <see cref="MainWindowViewModel.ShellState"/>: ввод чата и конфиг MCP/ACP для автономной сессии.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    private string _sendMessageKey = "Enter";

    /// <summary>Перенос строки в многострочном composer Skia — отдельно от отправки (модель как в мессенджерах).</summary>
    [ObservableProperty]
    private string _composerNewLineKey = "Ctrl+Enter";

    /// <summary>Показывать thinking/reasoning в истории после завершения ответа.</summary>
    [ObservableProperty]
    private bool _showThinkingInHistory = true;

    /// <summary>Отправлять только диагностики и сигнатуры текущего файла (минимальный контекст).</summary>
    [ObservableProperty]
    private bool _useMinimizedContext = true;

    /// <summary>
    /// JSON-конфиг внешних MCP-серверов (stdio) для автономного режима.
    /// Формат — как в <see cref="McpSettings.ExternalServersJson"/>.
    /// </summary>
    [ObservableProperty]
    private string _externalMcpServersJson = "[]";

    /// <summary>Подмешивать stdio MCP текущей IDE (<c>cascade-ide</c>) в <c>session/new</c> для Cursor ACP (ADR 0048 §7).</summary>
    [ObservableProperty]
    private bool _acpAutoInjectIdeMcp = true;

    public static readonly IReadOnlyList<string> SendMessageKeyOptions =
        ChatComposerChordOptions.Ordered;

    /// <summary>Те же варианты, что у отправки: Enter / Ctrl+Enter / Shift+Enter.</summary>
    public IReadOnlyList<string> SendMessageKeyOptionsList => SendMessageKeyOptions;

    /// <summary>Те же варианты, что у отправки: Enter / Ctrl+Enter / Shift+Enter.</summary>
    public IReadOnlyList<string> ComposerNewLineKeyOptionsList => SendMessageKeyOptions;
}
