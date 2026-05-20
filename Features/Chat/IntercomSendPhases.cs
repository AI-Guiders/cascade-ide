#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Имена фаз <see cref="IntercomSendTrace"/> для <see cref="Application.IntercomOutboundSendOrchestrator"/>.</summary>
internal static class IntercomSendPhases
{
    internal static class SendChat
    {
        internal static string Root => nameof(SendChat);
        internal static string Slash => Phase(nameof(Slash));
        internal static string BuildOutbound => Phase(nameof(BuildOutbound));
        internal static string PrepareMessage => Phase(nameof(PrepareMessage));
        internal static string CommitFeed => Phase(nameof(CommitFeed));
        internal static string DispatchProvider => Phase(nameof(DispatchProvider));

        private static string Phase(string name) => $"{nameof(SendChat)}.{name}";
    }
}
