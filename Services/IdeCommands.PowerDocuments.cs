namespace CascadeIDE.Services;

/// <summary>Focus/Power, автономка, чат, Ollama и документы (partial IdeCommands).</summary>
public static partial class IdeCommands
{
    // ——— Focus / Power / автономка / чат
    /// <summary>Создать контрольную точку (Focus). returns: text.</summary>
    public const string FocusCheckpoint = "focus_checkpoint";
    /// <summary>Откатить к последней контрольной точке (Focus). returns: text.</summary>
    public const string FocusRollback = "focus_rollback";
    /// <summary>Подтвердить текущий шаг плана (Focus). returns: text.</summary>
    public const string ConfirmFocusStep = "confirm_focus_step";
    /// <summary>Отменить текущий шаг плана (Focus). returns: text.</summary>
    public const string CancelFocusStep = "cancel_focus_step";
    /// <summary>Пояснить текущий шаг (Focus/Power). returns: text.</summary>
    public const string ExplainCurrentStep = "explain_current_step";
    /// <summary>Экстренно остановить автономные действия/выполнение (Emergency stop). returns: text.</summary>
    public const string EmergencyStop = "emergency_stop";
    /// <summary>Обновить снимок рабочего состояния (Power cockpit). returns: text.</summary>
    public const string RefreshWorkspaceSnapshot = "refresh_workspace_snapshot";
    /// <summary>Шаг трассы по индексу в AgentTraceSteps (0 — самый старый). args: step_index:integer; returns: text; example: {"step_index":0}.</summary>
    public const string ExplainTraceStep = "explain_trace_step";
    /// <summary>Откатить состояние по шагу трассы. args: step_index:integer; returns: text; example: {"step_index":0}.</summary>
    public const string RollbackTraceStep = "rollback_trace_step";
    /// <summary>Установить Safety L1. returns: text.</summary>
    public const string SetSafetyL1 = "set_safety_l1";
    /// <summary>Установить Safety L2. returns: text.</summary>
    public const string SetSafetyL2 = "set_safety_l2";
    /// <summary>Установить Safety L3. returns: text.</summary>
    public const string SetSafetyL3 = "set_safety_l3";
    /// <summary>Запустить автономный режим (agent run). returns: text.</summary>
    public const string StartAutonomous = "start_autonomous";
    /// <summary>Поставить автономный режим на паузу. returns: text.</summary>
    public const string PauseAutonomous = "pause_autonomous";
    /// <summary>Продолжить автономный режим после паузы. returns: text.</summary>
    public const string ResumeAutonomous = "resume_autonomous";
    /// <summary>Quick action: починить упавшие тесты. returns: text.</summary>
    public const string FixFailingTests = "fix_failing_tests";
    /// <summary>Quick action: расследовать NullReferenceException. returns: text.</summary>
    public const string InvestigateNullref = "investigate_nullref";
    /// <summary>Quick action: подготовить коммит (сводка/план/проверки). returns: text.</summary>
    public const string PrepareCommit = "prepare_commit";
    /// <summary>Чат: args: message?:string, role?:string. role assistant — только строка ассистента из MCP; иначе user и отправка; при chat_mcp_only локальный LLM не вызывается. returns: text; example: {"message":"hello"}.</summary>
    public const string SendChat = "send_chat";
    /// <summary>Скачать модель Ollama (как в настройках). args: model:string; returns: text; example: {"model":"qwen2.5-coder:7b"}.</summary>
    public const string InstallOllamaModel = "install_ollama_model";

    // ——— Документы
    /// <summary>Переоткрыть недавно закрытый документ. returns: text.</summary>
    public const string ReopenClosedDocument = "reopen_closed_document";
    /// <summary>Активировать документ (переключить вкладку). args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}.</summary>
    public const string ActivateDocument = "activate_document";
    /// <summary>Закрыть документ. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}.</summary>
    public const string CloseDocument = "close_document";
    /// <summary>Закрепить/открепить документ (pin). args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}.</summary>
    public const string TogglePinDocument = "toggle_pin_document";
    /// <summary>Переместить документ в группу 1. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}.</summary>
    public const string MoveDocumentToGroup1 = "move_document_to_group_1";
    /// <summary>Переместить документ в группу 2. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}.</summary>
    public const string MoveDocumentToGroup2 = "move_document_to_group_2";
    /// <summary>Переместить документ в группу 3. args: file_path:string; returns: text; example: {"file_path":"C:\\\\tmp\\\\a.cs"}.</summary>
    public const string MoveDocumentToGroup3 = "move_document_to_group_3";
}
