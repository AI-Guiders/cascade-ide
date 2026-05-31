#nullable enable

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CascadeIDE.Lang;

/// <summary>
/// Обёртка над <see cref="Resources"/> для смены языка в рантайме: привязки к свойствам
/// получают <see cref="INotifyPropertyChanged"/> после смены <see cref="Resources.Culture"/>.
/// </summary>
public sealed class LocViewModel : INotifyPropertyChanged
{
    public static LocViewModel? Current { get; private set; }

    static readonly Lazy<PropertyInfo[]> StringPropertyCache = new(() =>
        typeof(LocViewModel)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray());

    public LocViewModel() => Current = this;

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        if (name is not null)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Установить культуру ресурсов и потока и уведомить все строковые свойства.</summary>
    public void SetCulture(CultureInfo culture)
    {
        Resources.Culture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        foreach (var p in StringPropertyCache.Value)
            OnPropertyChanged(p.Name);
    }

    public string ChatPanel_PlanHeader => Resources.ChatPanel_PlanHeader;
    public string ChatPanel_PlanEmptyHint => Resources.ChatPanel_PlanEmptyHint;
    public string ChatPanel_NextActionHeader => Resources.ChatPanel_NextActionHeader;
    public string ChatPanel_AgentOpsHeader => Resources.ChatPanel_AgentOpsHeader;
    public string ChatPanel_AgentOpsEmptyHint => Resources.ChatPanel_AgentOpsEmptyHint;
    public string ChatPanel_ConfirmHeader => Resources.ChatPanel_ConfirmHeader;
    public string ChatPanel_ConfirmHint => Resources.ChatPanel_ConfirmHint;
    public string ChatPanel_ConfirmButton => Resources.ChatPanel_ConfirmButton;
    public string ChatPanel_CancelButton => Resources.ChatPanel_CancelButton;
    public string ChatPanel_ExplainStep => Resources.ChatPanel_ExplainStep;
    public string ChatPanel_EmergencyStop => Resources.ChatPanel_EmergencyStop;
    public string ChatPanel_TraceHeader => Resources.ChatPanel_TraceHeader;
    public string ChatPanel_TraceEmptyHint => Resources.ChatPanel_TraceEmptyHint;
    public string ChatPanel_TraceRollback => Resources.ChatPanel_TraceRollback;
    public string ChatPanel_SafetyDockTitlePower => Resources.ChatPanel_SafetyDockTitlePower;
    public string ChatPanel_SafetyTierObserve => Resources.ChatPanel_SafetyTierObserve;
    public string ChatPanel_SafetyTierConfirm => Resources.ChatPanel_SafetyTierConfirm;
    public string ChatPanel_SafetyTierAutonomous => Resources.ChatPanel_SafetyTierAutonomous;
    public string ChatPanel_SafetyTierObserveSubtitle => Resources.ChatPanel_SafetyTierObserveSubtitle;
    public string ChatPanel_SafetyTierConfirmSubtitle => Resources.ChatPanel_SafetyTierConfirmSubtitle;
    public string ChatPanel_SafetyTierAutonomousSubtitle => Resources.ChatPanel_SafetyTierAutonomousSubtitle;
    public string ChatPanel_SafetyCompactHeader => Resources.ChatPanel_SafetyCompactHeader;
    public string ChatPanel_EmergencyStopCompact => Resources.ChatPanel_EmergencyStopCompact;
    public string ChatPanel_ChatExpandButton => Resources.ChatPanel_ChatExpandButton;
    public string ChatPanel_ChatExpandTooltip => Resources.ChatPanel_ChatExpandTooltip;
    public string ChatPanel_ChatCollapseTooltip => Resources.ChatPanel_ChatCollapseTooltip;
    public string ChatPanel_AgentTyping => Resources.ChatPanel_AgentTyping;
    public string ChatPanel_NoMessagesYet => Resources.ChatPanel_NoMessagesYet;
    public string ChatPanel_MessageWatermark => Resources.ChatPanel_MessageWatermark;
    public string ChatPanel_SendButton => Resources.ChatPanel_SendButton;
    public string PanelChrome_EmergencyStopCaps => Resources.PanelChrome_EmergencyStopCaps;
    public string PanelChrome_OverflowActionsPlaceholder => Resources.PanelChrome_OverflowActionsPlaceholder;
    public string PanelChrome_CopyTitle => Resources.PanelChrome_CopyTitle;
    public string Lang_Menu_UiLanguage => Resources.Lang_Menu_UiLanguage;
    public string Lang_Menu_Russian => Resources.Lang_Menu_Russian;
    public string Lang_Menu_English => Resources.Lang_Menu_English;
    public string Lang_Menu_FollowSystem => Resources.Lang_Menu_FollowSystem;
}
