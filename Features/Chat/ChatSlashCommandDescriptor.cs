#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Слэш-путь → <see cref="Services.IdeCommands"/> (ADR 0119).</summary>
public sealed record ChatSlashCommandDescriptor(
    string SlashPath,
    string CommandId,
    string Help,
    ChatSlashCommandExecutionKind ExecutionKind = ChatSlashCommandExecutionKind.IdeCommand,
    string? MfdPage = null,
    string? PrimarySurface = null,
    string? MapLevel = null,
    string? SlashGroup = null,
    SlashCompletionKind Completion = SlashCompletionKind.None,
    IntercomMessageAudience MessageAudience = IntercomMessageAudience.SelfOnly,
    bool AutoRunOnCommit = false,
    bool AutoRunRequiresArgs = true)
{
    /// <summary>После выбора подсказки autocomplete — выполнить без второго Enter.</summary>
    public bool ShouldAutoExecuteAfterAutocompleteCommit(string argsTail) =>
        AutoRunOnCommit && (!AutoRunRequiresArgs || !string.IsNullOrWhiteSpace(argsTail));
}
