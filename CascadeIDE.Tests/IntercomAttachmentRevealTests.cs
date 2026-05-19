using System.Text.Json;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomAttachmentRevealTests
{
    [Fact]
    public void IntercomRevealAttachment_TryParse_AnchorJson()
    {
        var anchorJson = JsonSerializer.SerializeToElement(new
        {
            id = "a1",
            file = "src/Foo.cs",
            lineStart = 10,
            lineEnd = 20,
            memberKey = "M:Foo.Bar"
        });
        var args = new Dictionary<string, JsonElement>
        {
            ["anchor_json"] = anchorJson
        };

        Assert.True(IntercomRevealAttachmentMcpArgs.TryParse(args, out var anchor, out var select, out _, out var err), err);
        Assert.Equal("src/Foo.cs", anchor.File);
        Assert.Equal(10, anchor.LineStart);
        Assert.Equal(20, anchor.LineEnd);
        Assert.False(select);
    }

    [Fact]
    public void IntercomAttachmentRevealPlanner_Resolved_WhenFileExists()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(IntercomAttachmentRevealTests) + "_ws")).FullName;
        var rel = Path.Combine("sub", "hit.cs");
        var fullDir = Path.Combine(dir, "sub");
        Directory.CreateDirectory(fullDir);
        var full = Path.Combine(fullDir, "hit.cs");
        File.WriteAllText(full, "// x\n");

        var anchor = new AttachmentAnchor { File = rel.Replace('\\', '/'), LineStart = 1, LineEnd = 1 };
        var plan = IntercomAttachmentRevealPlan.Create(anchor, dir);

        Assert.Equal(IntercomAttachmentRevealPlan.OutcomeResolved, plan.ResolveOutcome);
        Assert.True(File.Exists(plan.AbsoluteFilePath));
        Assert.NotNull(plan.Lines);
    }

    [Fact]
    public void IntercomAttachmentRevealPlanner_FileMissing_WhenNotOnDisk()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(IntercomAttachmentRevealTests) + "_missing")).FullName;
        var anchor = new AttachmentAnchor { File = "nope/Missing.cs", LineStart = 1, LineEnd = 1 };
        var plan = IntercomAttachmentRevealPlan.Create(anchor, dir);

        Assert.Equal(IntercomAttachmentRevealPlan.OutcomeFileMissing, plan.ResolveOutcome);
        Assert.Contains("file_missing", plan.Message, StringComparison.Ordinal);
    }
}
