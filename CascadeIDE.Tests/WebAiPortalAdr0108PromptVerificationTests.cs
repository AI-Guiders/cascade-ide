using System.Text.Json;
using System.Text.RegularExpressions;
using CascadeIDE.Features.WebAiPortal.Application;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Контракт артефактов ADR 0108 в <c>AiPrompts/</c>: .md (json-cascade) и .chat-paste.txt (голый однострочный JSON) против whitelist моста.</summary>
public sealed class WebAiPortalAdr0108PromptVerificationTests
{
    private static readonly Regex JsonCascadeFence = new(
        """```json-cascade[ \t]*\r?\n(.*?)```""",
        RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static string PromptPath =>
        Path.Combine(AppContext.BaseDirectory, "AiPrompts", "web-ai-portal-bridge-adr0108.prompts.md");

    private static string ChatPastePath =>
        Path.Combine(AppContext.BaseDirectory, "AiPrompts", "web-ai-portal-bridge-adr0108.chat-paste.txt");

    [Fact]
    public void Prompt_file_shipped_next_to_tests()
    {
        Assert.True(File.Exists(PromptPath), "ожидается CopyToOutputDirectory в CascadeIDE.Tests.csproj: " + PromptPath);
    }

    [Fact]
    public void Chat_paste_plain_file_shipped_next_to_tests()
    {
        Assert.True(File.Exists(ChatPastePath), "ожидается CopyToOutputDirectory в CascadeIDE.Tests.csproj: " + ChatPastePath);
    }

    /// <summary>Текст без Markdown: каждая однострочная JSON-команда должна распознаваться мостом как голый буфер и входить в whitelist.</summary>
    [Fact]
    public void Chat_paste_plain_examples_are_allowlisted_when_parsed_like_clipboard_payload()
    {
        var txt = File.ReadAllText(ChatPastePath);
        Assert.Contains("0108-web-ai-portal-host-object-tools-bridge.md", txt, StringComparison.Ordinal);

        var exampleLines = 0;
        foreach (var segment in txt.Split('\n'))
        {
            var line = segment.Trim('\r').Trim();
            if (line.Length < 18 || line[0] != '{')
                continue;
            if (!WebAiPortalBridgePayloadResolution.TryParseBareExecuteCommand(line, out var json))
                continue;
            Assert.True(WebAiPortalBridgePayloadResolution.TryGetCommandId(json!, out var id));
            Assert.True(
                WebAiPortalCommandBridge.Whitelist.Contains(id!),
                $"строка-пример с command_id «{id}» вне whitelist");
            exampleLines++;
        }

        Assert.True(exampleLines >= 5, $"ожидалось ≥5 однострочных примеров JSON, получено {exampleLines}");
    }

    [Fact]
    public void Prompt_md_points_reader_to_chat_paste_plain_sibling_for_models_that_strip_markdown()
    {
        Assert.Contains(
            "web-ai-portal-bridge-adr0108.chat-paste.txt",
            File.ReadAllText(PromptPath),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Prompt_references_canonical_ADR_doc_path_and_each_fence_is_allowlisted_execute_command()
    {
        var md = File.ReadAllText(PromptPath);
        Assert.Contains("0108-web-ai-portal-host-object-tools-bridge.md", md, StringComparison.Ordinal);
        Assert.Contains("ADR 0108", md, StringComparison.Ordinal);

        var matches = JsonCascadeFence.Matches(md);
        Assert.True(matches.Count >= 2, "промпт должен содержать хотя бы два fenced json-cascade");

        foreach (Match m in matches)
        {
            Assert.True(m.Success && m.Groups.Count >= 2);
            var inner = m.Groups[1].Value.Trim();
            Assert.False(string.IsNullOrWhiteSpace(inner));

            using var doc = JsonDocument.Parse(inner);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
            Assert.True(doc.RootElement.TryGetProperty("command_id", out var cid));
            Assert.Equal(JsonValueKind.String, cid.ValueKind);
            var id = cid.GetString();
            Assert.False(string.IsNullOrWhiteSpace(id));
            Assert.True(
                WebAiPortalCommandBridge.Whitelist.Contains(id!),
                $"command_id «{id}» не из whitelist моста ADR 0108");
            Assert.False(
                string.Equals(IdeCommands.CodebaseIndexReindex, id, StringComparison.Ordinal),
                "промпт ADR0108 только read-PoC: reindex не в whitelist моста");
        }
    }
}
