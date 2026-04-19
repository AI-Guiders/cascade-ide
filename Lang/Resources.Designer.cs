#nullable enable

namespace CascadeIDE.Lang;

/// <summary>Строки UI. Файлы: <c>Lang/Resources.resx</c>, <c>Resources.ru-RU.resx</c>, <c>Resources.en-US.resx</c> (как IncomeCascade).</summary>
public static class Resources
{
    static System.Resources.ResourceManager? _resourceManager;
    static System.Globalization.CultureInfo? _culture;

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static System.Globalization.CultureInfo? Culture
    {
        get => _culture;
        set => _culture = value;
    }

    static System.Resources.ResourceManager ResourceManager =>
        _resourceManager ??= new System.Resources.ResourceManager(
            "CascadeIDE.Lang.Resources",
            typeof(Resources).Assembly);

    static string GetString(string name) => ResourceManager.GetString(name, _culture) ?? name;

    public static string ChatPanel_PlanHeader => GetString(nameof(ChatPanel_PlanHeader));
    public static string ChatPanel_PlanEmptyHint => GetString(nameof(ChatPanel_PlanEmptyHint));
    public static string ChatPanel_NextActionHeader => GetString(nameof(ChatPanel_NextActionHeader));
    public static string ChatPanel_AgentOpsHeader => GetString(nameof(ChatPanel_AgentOpsHeader));
    public static string ChatPanel_AgentOpsEmptyHint => GetString(nameof(ChatPanel_AgentOpsEmptyHint));
    public static string ChatPanel_ConfirmHeader => GetString(nameof(ChatPanel_ConfirmHeader));
    public static string ChatPanel_ConfirmHint => GetString(nameof(ChatPanel_ConfirmHint));
    public static string ChatPanel_ConfirmButton => GetString(nameof(ChatPanel_ConfirmButton));
    public static string ChatPanel_CancelButton => GetString(nameof(ChatPanel_CancelButton));
    public static string ChatPanel_ExplainStep => GetString(nameof(ChatPanel_ExplainStep));
    public static string ChatPanel_EmergencyStop => GetString(nameof(ChatPanel_EmergencyStop));
    public static string ChatPanel_TraceHeader => GetString(nameof(ChatPanel_TraceHeader));
    public static string ChatPanel_TraceEmptyHint => GetString(nameof(ChatPanel_TraceEmptyHint));
    public static string ChatPanel_TraceRollback => GetString(nameof(ChatPanel_TraceRollback));
    public static string ChatPanel_SafetyDockTitlePower => GetString(nameof(ChatPanel_SafetyDockTitlePower));
    public static string ChatPanel_SafetyTierL1 => GetString(nameof(ChatPanel_SafetyTierL1));
    public static string ChatPanel_SafetyTierL2 => GetString(nameof(ChatPanel_SafetyTierL2));
    public static string ChatPanel_SafetyTierL3 => GetString(nameof(ChatPanel_SafetyTierL3));
    public static string ChatPanel_SafetyTierL1Subtitle => GetString(nameof(ChatPanel_SafetyTierL1Subtitle));
    public static string ChatPanel_SafetyTierL2Subtitle => GetString(nameof(ChatPanel_SafetyTierL2Subtitle));
    public static string ChatPanel_SafetyTierL3Subtitle => GetString(nameof(ChatPanel_SafetyTierL3Subtitle));
    public static string ChatPanel_SafetyCompactHeader => GetString(nameof(ChatPanel_SafetyCompactHeader));
    public static string ChatPanel_EmergencyStopCompact => GetString(nameof(ChatPanel_EmergencyStopCompact));
    public static string ChatPanel_ChatExpandButton => GetString(nameof(ChatPanel_ChatExpandButton));
    public static string ChatPanel_ChatExpandTooltip => GetString(nameof(ChatPanel_ChatExpandTooltip));
    public static string ChatPanel_ChatCollapseTooltip => GetString(nameof(ChatPanel_ChatCollapseTooltip));
    public static string ChatPanel_AgentTyping => GetString(nameof(ChatPanel_AgentTyping));
    public static string ChatPanel_NoMessagesYet => GetString(nameof(ChatPanel_NoMessagesYet));
    public static string ChatPanel_MessageWatermark => GetString(nameof(ChatPanel_MessageWatermark));
    public static string ChatPanel_SendButton => GetString(nameof(ChatPanel_SendButton));
    public static string PanelChrome_EmergencyStopCaps => GetString(nameof(PanelChrome_EmergencyStopCaps));
    public static string PanelChrome_OverflowActionsPlaceholder => GetString(nameof(PanelChrome_OverflowActionsPlaceholder));
    public static string PanelChrome_CopyTitle => GetString(nameof(PanelChrome_CopyTitle));
    public static string Lang_Menu_UiLanguage => GetString(nameof(Lang_Menu_UiLanguage));
    public static string Lang_Menu_Russian => GetString(nameof(Lang_Menu_Russian));
    public static string Lang_Menu_English => GetString(nameof(Lang_Menu_English));
    public static string Lang_Menu_FollowSystem => GetString(nameof(Lang_Menu_FollowSystem));
    public static string Safety_Description_L1 => GetString(nameof(Safety_Description_L1));
    public static string Safety_Description_L2 => GetString(nameof(Safety_Description_L2));
    public static string Safety_Description_L3 => GetString(nameof(Safety_Description_L3));
}
