#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Forge.Infrastructure;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

public sealed partial class ChatSlashCommandRunner
{
    private bool TryBuildPathArgs(
        ChatSlashCommandDescriptor descriptor,
        string? argTail,
        out IReadOnlyDictionary<string, JsonElement>? args,
        out string? error)
    {
        args = null;
        error = null;

        if (descriptor.CommandId is not (IdeCommands.OpenFile or IdeCommands.LoadSolution))
            return true;

        var pathArg = argTail;
        var workspaceRoot = _getWorkspaceRoot?.Invoke();
        if (!ChatSlashWorkspacePathHelper.TryNormalizePathArgument(
                pathArg,
                workspaceRoot,
                out var fullPath,
                out error))
        {
            return false;
        }

        if (descriptor.CommandId == IdeCommands.OpenFile && !File.Exists(fullPath!))
        {
            error = "Файл не найден: " + fullPath;
            return false;
        }

        if (descriptor.CommandId == IdeCommands.LoadSolution
            && !File.Exists(fullPath!)
            && !Directory.Exists(fullPath!))
        {
            error = "Путь не найден: " + fullPath;
            return false;
        }

        args = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["path"] = JsonSerializer.SerializeToElement(fullPath),
        };
        return true;
    }

    private static IReadOnlyDictionary<string, JsonElement>? BuildArgs(
        ChatSlashCommandDescriptor descriptor,
        string? argTail)
    {
        if (!string.IsNullOrEmpty(descriptor.MfdPage))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["page"] = JsonSerializer.SerializeToElement(descriptor.MfdPage),
            };
        }

        if (!string.IsNullOrEmpty(descriptor.PrimarySurface))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["surface"] = JsonSerializer.SerializeToElement(descriptor.PrimarySurface),
            };
        }

        if (!string.IsNullOrEmpty(descriptor.MapLevel))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["level"] = JsonSerializer.SerializeToElement(descriptor.MapLevel),
            };
        }

        if (string.IsNullOrWhiteSpace(argTail))
            return null;

        return descriptor.CommandId switch
        {
            IdeCommands.ChatSetProductSpine => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["current_focus"] = JsonSerializer.SerializeToElement(argTail),
            },
            IdeCommands.ChatExportReadable => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["write_file"] = JsonSerializer.SerializeToElement(false),
            },
            IdeCommands.GitCommit => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["message"] = JsonSerializer.SerializeToElement(ChatSlashArgsTail.NormalizeFreeText(argTail)),
            },
            IdeCommands.GitDiff when !string.IsNullOrWhiteSpace(argTail) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["path"] = JsonSerializer.SerializeToElement(argTail.Trim()),
                },
            IdeCommands.GitLog when int.TryParse(argTail.Trim(), out var n) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["n"] = JsonSerializer.SerializeToElement(n),
                },
            IdeCommands.SearchWorkspaceText when !string.IsNullOrWhiteSpace(argTail) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["pattern"] = JsonSerializer.SerializeToElement(argTail.Trim()),
                },
            IdeCommands.CreateProjectInSolution
                when TryGetSolutionNewTemplate(descriptor.SlashPath, out var template) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["template"] = JsonSerializer.SerializeToElement(template),
                    ["project_name"] = JsonSerializer.SerializeToElement(argTail.Trim()),
                },
            _ => null,
        };
    }

    private static bool TryGetSolutionNewTemplate(string slashPath, out string template)
    {
        const string prefix = "/solution new ";
        if (slashPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            template = slashPath[prefix.Length..].Trim().ToLowerInvariant();
            if (template is "console" or "classlib" or "webapi")
                return true;
        }

        template = "";
        return false;
    }

    private static string? ValidateRequiredArgs(
        ChatSlashCommandDescriptor descriptor,
        string? argTail)
    {
        if (descriptor.CommandId == IdeCommands.GitCommit && string.IsNullOrWhiteSpace(argTail))
            return "Укажи сообщение коммита: /git commit <message>";

        if (descriptor.CommandId == IdeCommands.SearchWorkspaceText && string.IsNullOrWhiteSpace(argTail))
            return "Укажи шаблон поиска: /search <pattern>";

        if (descriptor.CommandId is IdeCommands.OpenFile or IdeCommands.LoadSolution
            && string.IsNullOrWhiteSpace(argTail))
        {
            return descriptor.CommandId == IdeCommands.OpenFile
                ? "Укажи путь к файлу: /file open <path>"
                : "Укажи путь: /solution load <path>";
        }

        if (descriptor.CommandId == IdeCommands.CreateProjectInSolution)
        {
            if (!TryGetSolutionNewTemplate(descriptor.SlashPath, out var template))
                return "Укажи шаблон: /solution new console|classlib|webapi <имя>";
            if (string.IsNullOrWhiteSpace(argTail))
                return $"Укажи имя проекта: /solution new {template} <имя>";
        }

        if (IntentMelodyCatalog.TryGetParametricRootByCommandId(descriptor.CommandId, out var parametricRoot)
            && ChatSlashParametricArgsBuilder.RequiresNonEmptyArgsTail(parametricRoot)
            && string.IsNullOrWhiteSpace(argTail))
        {
            return parametricRoot.WireClass switch
            {
                "int_chain_colon_space" =>
                    "Укажи строки (1-based): одну, «5 10» или «5:10».",
                _ => "Укажи параметры команды в хвосте строки.",
            };
        }

        return null;
    }

    /// <summary>Текст под командой только если есть что показать; «ok» / <c>{"ok":true}</c> не выводим.</summary>
    private static string? FormatSuccessDetail(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var trimmed = json.Trim();
        if (trimmed.Length == 0 || IsTrivialAck(trimmed))
            return null;

        return trimmed.Length <= 400 ? trimmed : $"Ответ ({trimmed.Length} символов).";
    }

    private static bool IsTrivialAck(string trimmed)
    {
        if (string.Equals(trimmed, "ok", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!trimmed.StartsWith('{'))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            if (!doc.RootElement.TryGetProperty("ok", out var ok))
                return false;
            if (ok.ValueKind is JsonValueKind.True)
                return doc.RootElement.GetPropertyCount() <= 2;
            if (ok.ValueKind is JsonValueKind.String
                && string.Equals(ok.GetString(), "true", StringComparison.OrdinalIgnoreCase))
                return doc.RootElement.GetPropertyCount() <= 2;
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
