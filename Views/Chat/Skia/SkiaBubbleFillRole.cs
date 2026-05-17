namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Заливка/рамка пузыря — вместо строкового accent в renderer.</summary>
internal enum SkiaBubbleFillRole
{
    MessageUser = 0,
    MessageAssistant = 1,
    MessageThinking = 2,
    MessageTool = 3,
    ClarificationPending = 4,
    ClarificationResolved = 5,
    OverviewNav = 6,
    ThreadRow = 7,
    ThreadRowActive = 8,
    ThreadHeader = 9,
    ThreadHeaderActive = 10,
    SpineCard = 11,
    SpineStrip = 12
}
