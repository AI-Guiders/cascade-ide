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
    string? SlashGroup = null,
    SlashCompletionKind Completion = SlashCompletionKind.None,
    IntercomMessageAudience MessageAudience = IntercomMessageAudience.SelfOnly);
