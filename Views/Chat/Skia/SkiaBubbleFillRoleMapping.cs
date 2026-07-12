using CascadeIDE.Features.Chat;

namespace CascadeIDE.Views.Chat.Skia;

internal static class SkiaBubbleFillRoleMapping
{
    public static SkiaBubbleFillRole FromMessageRole(ChatMessageVisualRole role) =>
        role switch
        {
            ChatMessageVisualRole.User => SkiaBubbleFillRole.MessageUser,
            ChatMessageVisualRole.Assistant => SkiaBubbleFillRole.MessageAssistant,
            ChatMessageVisualRole.Thinking => SkiaBubbleFillRole.MessageThinking,
            ChatMessageVisualRole.Tool => SkiaBubbleFillRole.MessageTool,
            ChatMessageVisualRole.ClarificationPending => SkiaBubbleFillRole.ClarificationPending,
            ChatMessageVisualRole.ClarificationResolved => SkiaBubbleFillRole.ClarificationResolved,
            ChatMessageVisualRole.SlashCommand => SkiaBubbleFillRole.MessageTool,
            ChatMessageVisualRole.SedmContext => SkiaBubbleFillRole.SedmCard,
            ChatMessageVisualRole.SedmIntent => SkiaBubbleFillRole.SedmCard,
            ChatMessageVisualRole.SedmDecision => SkiaBubbleFillRole.SedmCard,
            ChatMessageVisualRole.SedmLifecycle => SkiaBubbleFillRole.SedmCard,
            _ => SkiaBubbleFillRole.MessageAssistant
        };
}
