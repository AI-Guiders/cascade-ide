using System.Text.Json;
using CascadeIDE.Features.WebAiPortal.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WebAiPortalChatMixInFormatterTests
{
    [Fact]
    public void BuildForComposer_keeps_short_response()
    {
        const string body = """{"hits":[]}""";
        var r = WebAiPortalChatMixInFormatter.BuildForComposer(true, 1200, IdeCommands.CodebaseIndexSearch, body);
        Assert.False(r.UsedCompactMixer);
        Assert.Equal(body, r.TextForComposer);
    }

    [Fact]
    public void BuildForComposer_compacts_large_get_editor_state_with_hci_cascade_hints()
    {
        const string previewLeakMarker = "<<<PREVIEW_BLOB>>>";
        var dto = new EditorStateDto
        {
            FilePath = @"D:\repo\Prog.cs",
            CaretLine = 100,
            CaretColumn = 5,
            SelectionStart = 0,
            SelectionLength = 0,
            SelectionText = "",
            ContentLength = 50_000,
            IsEmpty = false,
            ContentPreview = previewLeakMarker.PadRight(15_000, '_'),
        };
        var huge = JsonSerializer.Serialize(dto);
        Assert.True(huge.Length > 1200);

        var r = WebAiPortalChatMixInFormatter.BuildForComposer(
            preferCompact: true,
            maxChatCharacters: 1200,
            executedCommandId: IdeCommands.GetEditorState,
            fullResponseText: huge);

        Assert.True(r.UsedCompactMixer);
        Assert.True(r.TextForComposer.Length < huge.Length / 10, "компакт сильно короче сырья");
        Assert.Contains("codebase_index_search", r.TextForComposer, StringComparison.Ordinal);
        Assert.Contains("codebase_index_status", r.TextForComposer, StringComparison.Ordinal);
        Assert.Contains("get_editor_content_range", r.TextForComposer, StringComparison.Ordinal);
        Assert.DoesNotContain(previewLeakMarker, r.TextForComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("```json-cascade", r.TextForComposer, StringComparison.Ordinal);
        Assert.StartsWith("▸ ", r.TextForComposer.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(static l => l.Contains("codebase_index_status", StringComparison.Ordinal)), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildForComposer_generic_oversized_mentions_explain_when_command_unknown_shape()
    {
        var slab = new string('{', 3_000);
        var r = WebAiPortalChatMixInFormatter.BuildForComposer(true, 800, "codebase_index_search", slab);
        Assert.True(r.UsedCompactMixer);
        Assert.Contains("codebase_index_explain", r.TextForComposer, StringComparison.Ordinal);
        Assert.True(r.TextForComposer.Length < slab.Length, "компакт короче бессмысленного сырья");
        Assert.DoesNotContain("```json-cascade", r.TextForComposer, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildForComposer_compacts_oversized_search_into_hit_table_not_raw_json()
    {
        const int hugeSnippet = 800;
        object hit(int id) => new
        {
            hitId = (long)id,
            path = $"src/Chunk{id}.cs",
            extension = ".cs",
            hitKind = "text_fts",
            rankScore = 0.9 - id * 0.01,
            ftsScore = (double?)0.5,
            vecScore = (double?)null,
            snippet = new string('x', hugeSnippet),
            lineStart = id,
            lineEnd = id + 20,
            chunkCharCount = 400,
            lastWriteUtcIso = (string?)null,
        };
        var hits = Enumerable.Range(1, 10).Select(hit).ToArray();
        var body = JsonSerializer.Serialize(
            new
            {
                err = (string?)null,
                indexFormatVersion = 2,
                query = "class Program",
                databasePath = "D:\\db\\hc.sqlite",
                hits,
            },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.True(body.Length > WebAiPortalChatMixInFormatter.DefaultMaxChatCharacters);

        var r = WebAiPortalChatMixInFormatter.BuildForComposer(true, 1200, IdeCommands.CodebaseIndexSearch, body);
        Assert.True(r.UsedCompactMixer);
        Assert.Contains("hit_id=1", r.TextForComposer, StringComparison.Ordinal);
        Assert.Contains("codebase_index_explain", r.TextForComposer, StringComparison.Ordinal);
        Assert.Contains("▸ ", r.TextForComposer, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 150), r.TextForComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("```json-cascade", r.TextForComposer, StringComparison.Ordinal);
        Assert.True(r.TextForComposer.Length < body.Length / 3);
    }

    [Fact]
    public void BuildForComposer_compacts_oversized_status_to_few_lines()
    {
        var exts = Enumerable.Repeat(".cs", 600).Select((_, i) => $".t{i}").ToArray();
        var body = JsonSerializer.Serialize(
            new
            {
                indexFormatVersion = 2,
                databasePath = new string('z', 2200),
                databaseExists = true,
                documentCount = 2627,
                documentCountMayBeStale = false,
                indexedAtIso = "2026-05-10T08:00:00Z",
                workspaceRoot = @"D:\ws\repo",
                lastReindexError = (string?)null,
                lastReindexErrorAtIso = (string?)null,
                settingsSource = "embedded",
                settingsParseError = (string?)null,
                effectiveSettings = new { includeCsInFts = true, extraIncludeRoots = Array.Empty<string>(), excludeRoots = Array.Empty<string>(), effectiveExtensions = exts, excludePathSegments = Array.Empty<string>(), ignoreFiles = Array.Empty<string>(), maxIndexedFileBytes = 524288, chunkLines = 110, chunkOverlapLines = 15, binaryProbeBytes = 8192 },
                reindexState = "idle",
                reindexStartedAtIso = (string?)null,
            },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.True(body.Length > WebAiPortalChatMixInFormatter.DefaultMaxChatCharacters);

        var r = WebAiPortalChatMixInFormatter.BuildForComposer(true, 1200, IdeCommands.CodebaseIndexStatus, body);
        Assert.True(r.UsedCompactMixer);
        Assert.Contains("2627", r.TextForComposer, StringComparison.Ordinal);
        Assert.Contains("расширений FTS", r.TextForComposer, StringComparison.Ordinal);
        Assert.True(r.TextForComposer.Length < body.Length / 8);
    }

    [Fact]
    public void BuildForComposer_compacts_oversized_editor_content_range()
    {
        var content = new string('q', 2600);
        var body = JsonSerializer.Serialize(
            new Dictionary<string, object?> { ["file_path"] = @"D:\a\b\Program.cs", ["start_line"] = 1, ["end_line"] = 80, ["content"] = content });
        Assert.True(body.Length > WebAiPortalChatMixInFormatter.DefaultMaxChatCharacters);

        var r = WebAiPortalChatMixInFormatter.BuildForComposer(true, 1200, IdeCommands.GetEditorContentRange, body);
        Assert.True(r.UsedCompactMixer);
        Assert.Contains("get_editor_content_range", r.TextForComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("```json-cascade", r.TextForComposer, StringComparison.Ordinal);
        Assert.DoesNotContain(content, r.TextForComposer, StringComparison.Ordinal);
        Assert.True(r.TextForComposer.Length < body.Length / 2);
    }
}
