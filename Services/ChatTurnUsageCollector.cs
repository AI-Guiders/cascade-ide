using CascadeIDE.Services.Fm;

namespace CascadeIDE.Services;

/// <summary>Сбор usage одного хода провайдера (опциональный out-parameter для <see cref="IAiChatProvider"/>).</summary>
public sealed class ChatTurnUsageCollector
{
    public FmTurnUsage? LastTurn { get; private set; }

    internal void Report(FmTurnUsage usage) => LastTurn = usage;
}
