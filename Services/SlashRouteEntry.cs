#nullable enable
using CascadeIDE.Features.Chat;

namespace CascadeIDE.Services;

/// <summary>Форма slash (<c>/build run</c>) у <c>command_id</c> в <see cref="IntentMelodyAliases.BundledRelativePath"/> (ADR 0119).</summary>
public readonly record struct SlashRouteEntry(
    string SlashPath,
    string CommandId,
    string Help,
    ChatSlashCommandExecutionKind ExecutionKind = ChatSlashCommandExecutionKind.IdeCommand,
    string? MfdPage = null,
    string? PrimarySurface = null,
    string? Group = null,
    SlashCompletionKind Completion = SlashCompletionKind.None);
