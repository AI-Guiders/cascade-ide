namespace CascadeIDE.ViewModels;

/// <summary>Путь Cursor ACP.</summary>
public partial class MainWindowViewModel
{
    private string _cursorAcpAgentPath = "";

    /// <summary>Путь к <c>cursor-agent.cmd</c> или каталогу <c>dist-package</c> (настройки, провайдер Cursor ACP).</summary>
    public string CursorAcpAgentPath
    {
        get => _cursorAcpAgentPath;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _cursorAcpAgentPath, v))
                return;
            _settings.Ai.CursorAcpPath = v;
            SaveSettingsIfChanged();
            if (ActiveAiProvider == "CursorACP")
                ChatPanel.DisposeCursorAcpSession();
            ChatPanel.RefreshSendChatCommandState();
        }
    }
}
