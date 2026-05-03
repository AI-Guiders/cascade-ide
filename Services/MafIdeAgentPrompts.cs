#nullable enable

using System.Text;

namespace CascadeIDE.Services;

/// <summary>
/// Тексты MAF IDE-агента: <see cref="BundledRelativePath"/> — диск под exe затем EmbeddedResource (<see cref="BundledAppContent"/>).
/// Секции в файле: строки <c>## agent_system</c>, <c>## salvage_recap_system</c>, <c>## salvage_recap_user_message</c>.
/// </summary>
internal static class MafIdeAgentPrompts
{
    /// <summary>Относительно <see cref="AppContext.BaseDirectory"/>.</summary>
    internal const string BundledRelativePath = "AiPrompts/maf-ide-agent.prompts.md";

    private const string AgentSystemSection = "agent_system";
    private const string SalvageSystemSection = "salvage_recap_system";
    private const string SalvageUserSection = "salvage_recap_user_message";

    private static readonly Lazy<PromptPack> Loaded = new(ParsePromptFile, LazyThreadSafetyMode.ExecutionAndPublication);

    internal sealed record PromptPack(string AgentSystem, string SalvageRecapSystem, string SalvageUserMessageTemplate)
    {
        internal string BuildSalvageUserMessage(string userQuery, string toolPayload) =>
            SalvageUserMessageTemplate
                .Replace("{{USER_QUERY}}", userQuery ?? "", StringComparison.Ordinal)
                .Replace("{{TOOL_PAYLOAD}}", toolPayload ?? "", StringComparison.Ordinal);
    }

    internal static PromptPack Current => Loaded.Value;

    private static PromptPack ParsePromptFile()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var raw) || string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                $"Отсутствует или пустой {BundledRelativePath} (рядом с exe или EmbeddedResource сборки CascadeIDE).");

        var sections = SplitSections(raw);
        if (!sections.TryGetValue(AgentSystemSection, out var agent) || string.IsNullOrWhiteSpace(agent))
            throw new InvalidOperationException($"{BundledRelativePath}: нужна секция ## {AgentSystemSection}.");
        if (!sections.TryGetValue(SalvageSystemSection, out var salvageSys) || string.IsNullOrWhiteSpace(salvageSys))
            throw new InvalidOperationException($"{BundledRelativePath}: нужна секция ## {SalvageSystemSection}.");
        if (!sections.TryGetValue(SalvageUserSection, out var salvageUser) || string.IsNullOrWhiteSpace(salvageUser))
            throw new InvalidOperationException($"{BundledRelativePath}: нужна секция ## {SalvageUserSection}.");
        if (!salvageUser.Contains("{{USER_QUERY}}", StringComparison.Ordinal) ||
            !salvageUser.Contains("{{TOOL_PAYLOAD}}", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"{BundledRelativePath}: в ## {SalvageUserSection} должны быть плейсхолдеры {{USER_QUERY}} и {{TOOL_PAYLOAD}}.");

        return new PromptPack(
            agent.Trim(),
            salvageSys.Trim(),
            salvageUser.Trim());
    }

    private static Dictionary<string, string> SplitSections(string raw)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(raw);
        string? currentKey = null;
        var sb = new StringBuilder();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal) && line.Length > 3)
            {
                if (currentKey is not null)
                    sections[currentKey] = sb.ToString().TrimEnd();
                sb.Clear();
                var keyPart = line[3..].Trim();
                var firstToken = keyPart.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
                currentKey = firstToken.Length > 0 ? firstToken[0].Trim().Trim('\uFEFF') : null;
                if (string.IsNullOrEmpty(currentKey))
                    currentKey = null;
                continue;
            }

            if (currentKey is not null)
            {
                sb.AppendLine(line);
            }
        }

        if (currentKey is not null)
            sections[currentKey] = sb.ToString().TrimEnd();

        return sections;
    }
}
