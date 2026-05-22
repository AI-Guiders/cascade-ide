namespace CascadeIDE.Views;

/// <summary>Запрос переименования темы из Skia Intercom.</summary>
public sealed class TopicRenameRequestEventArgs : EventArgs
{
    public TopicRenameRequestEventArgs(Guid threadId, bool showContextMenu)
    {
        ThreadId = threadId;
        ShowContextMenu = showContextMenu;
    }

    public Guid ThreadId { get; }

    /// <summary>true — ПКМ (контекстное меню); false — сразу диалог (двойной клик, F2).</summary>
    public bool ShowContextMenu { get; }
}
