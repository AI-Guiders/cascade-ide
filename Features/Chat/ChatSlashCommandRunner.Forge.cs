#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Forge.Infrastructure;
using CascadeIDE.Features.Forge.Lens;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

public sealed partial class ChatSlashCommandRunner
{
    private async Task<ChatSlashCommandRunResult> RunForgeAsync(
        ChatSlashCommandDescriptor descriptor,
        string displayPath,
        string? argsTail,
        CancellationToken cancellationToken)
    {
        if (string.Equals(descriptor.CommandId, "forge.artifact.goto", StringComparison.Ordinal))
        {
            if (_executeIdeCommand is null)
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "IDE command bridge недоступен для forge.artifact.goto.");
            }

            if (string.IsNullOrWhiteSpace(argsTail))
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "Bracket [FRG:…] required.");
            }

            var gotoArgs = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["bracket"] = JsonSerializer.SerializeToElement(argsTail.Trim()),
                ["select_code"] = JsonSerializer.SerializeToElement(true),
            };
            var gotoMessage = await _executeIdeCommand(
                IdeCommands.ForgeArtifactGoto,
                gotoArgs,
                cancellationToken).ConfigureAwait(false);
            var gotoOk = !gotoMessage.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
            return new ChatSlashCommandRunResult(true, gotoOk, displayPath, argsTail, gotoMessage);
        }

        var ctx = ForgeLensWriteClient.TryResolveContext(
            _getWorkspaceRoot?.Invoke(),
            baseUrlArg: null,
            repoArg: null);
        if (ctx is null)
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "Укажи [workspace.forge] (base_url + repo) или подключись через forge_lens.connect.");
        }

        var (ok, message) = await ForgeCommandExecuteClient.ExecuteAsync(
            ctx.BaseUrl,
            ctx.ApiToken,
            descriptor.SlashPath,
            argsTail,
            ctx.Repo,
            cancellationToken).ConfigureAwait(false);

        return new ChatSlashCommandRunResult(true, ok, displayPath, argsTail, message);
    }
}
