namespace CascadeIDE.Features.UiChrome;

/// <summary>Числовые константы разметки, общие для всех режимов (без магических литералов в VM).</summary>
public static class UiModeLayoutDimensions
{
    /// <summary>Колонка чата в свёрнутом виде (полоска-тоггл).</summary>
    public const int ChatPanelCollapsedWidthPixels = 88;

    /// <summary>Ширина развёрнутого чата по умолчанию (Focus, Balanced).</summary>
    public const int ChatPanelExpandedDefaultWidthPixels = 340;

    /// <summary>Power cockpit: чуть шире из‑за боковой колонки.</summary>
    public const int ChatPanelExpandedPowerWidthPixels = 420;

    /// <summary>Agent Chat: приоритет чата (ориентир «как в Cursor»).</summary>
    public const int ChatPanelExpandedAgentChatWidthPixels = 520;
}
