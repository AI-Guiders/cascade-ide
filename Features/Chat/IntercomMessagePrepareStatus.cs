#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Результат prepare-pipeline (ADR 0134).</summary>
public enum IntercomMessagePrepareStatus
{
    Failed,
    PartialSuccess,
    Success,
}
