#nullable enable

using System.Text.Json;

namespace CascadeIDE.Features.Chat;

public sealed partial class ChatSlashCommandRunner
{
    private async Task<ChatSlashCommandRunResult> RunIdeCommandAsync(
        ChatSlashCommandDescriptor descriptor,
        string displayPath,
        string? argsTail,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequiredArgs(descriptor, argsTail);
        if (validationError is not null)
            return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, validationError);

        if (_executeIdeCommand is null)
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "IDE command bridge недоступен для слэш-команд.");
        }

        IReadOnlyDictionary<string, JsonElement>? args;
        if (ChatSlashParametricArgsBuilder.IsParametricCatalogCommand(descriptor.CommandId))
        {
            if (_getEditorContext is null)
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "Контекст редактора недоступен для параметрической слэш-команды.");
            }

            if (!ChatSlashParametricArgsBuilder.TryBuild(
                    descriptor.CommandId,
                    argsTail ?? "",
                    _getEditorContext(),
                    out args,
                    out var parametricError))
            {
                return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, parametricError);
            }
        }
        else if (!TryBuildPathArgs(descriptor, argsTail, out args, out var pathError))
        {
            return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, pathError);
        }
        else if (args is null)
        {
            args = BuildArgs(descriptor, argsTail);
        }

        try
        {
            var json = await _executeIdeCommand(descriptor.CommandId, args, cancellationToken).ConfigureAwait(false);
            return new ChatSlashCommandRunResult(
                true,
                true,
                displayPath,
                argsTail,
                FormatSuccessDetail(json));
        }
        catch (Exception ex)
        {
            return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, ex.Message);
        }
    }
}
