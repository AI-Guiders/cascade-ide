namespace CascadeIDE.ViewModels;

/// <summary>Путь Cursor ACP и предпочитаемая модель.</summary>
public partial class MainWindowViewModel
{
    private string _cursorAcpAgentPath = "";
    private string _cursorAcpModelId = "";

    /// <summary>Путь к <c>cursor-agent.cmd</c> или каталогу <c>dist-package</c> (настройки, провайдер Cursor ACP).</summary>
    public string CursorAcpAgentPath
    {
        get => _cursorAcpAgentPath;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _cursorAcpAgentPath, v))
                return;
            _settings.Ai.Acp.CursorAcpPath = v;
            SaveSettingsIfChanged();
            if (string.Equals(AiMode, "acp", StringComparison.OrdinalIgnoreCase))
                ChatPanel.DisposeCursorAcpSession();
            ChatPanel.RefreshSendChatCommandState();
        }
    }

    /// <summary><c>modelId</c> для Cursor ACP (<c>session/setModel</c>); TOML: <c>cursor_acp_model_id</c>.</summary>
    public string CursorAcpModelId
    {
        get => _cursorAcpModelId;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _cursorAcpModelId, v))
                return;
            _settings.Ai.Acp.CursorAcpModelId = v;
            SaveSettingsIfChanged();
        }
    }
}
