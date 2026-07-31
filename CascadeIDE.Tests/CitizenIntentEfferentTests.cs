using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CitizenIntentEfferentTests
{
    [Fact]
    public void ExtractIntentTexts_FindsLines()
    {
        var texts = CitizenIntentEfferent.ExtractIntentTexts("""
            hello
            @intent open path=D:/tmp/a.cs
            @intent seats_detail=full
            """);
        Assert.Equal(2, texts.Count);
        Assert.Equal("open path=D:/tmp/a.cs", texts[0]);
        Assert.Equal("seats_detail=full", texts[1]);
    }

    [Fact]
    public void MapToIde_OpenPath_BecomesOpenFile()
    {
        var a = CitizenIntentEfferent.MapToIde("open path=D:/tmp/a.cs");
        Assert.True(a.Ok);
        Assert.Equal("open_file", a.CommandId);
        Assert.NotNull(a.Args);
        Assert.Equal("D:/tmp/a.cs", a.Args!["path"].GetString());
    }

    [Fact]
    public void MapToIde_RefuseWSpray()
    {
        var a = CitizenIntentEfferent.MapToIde("seats_detail=full");
        Assert.False(a.Ok);
        Assert.Contains("refuse_w_spray", a.Reason, StringComparison.Ordinal);
        Assert.Null(a.CommandId);
    }

    [Fact]
    public void MapToIde_DrillEditor_GetEditorState()
    {
        var a = CitizenIntentEfferent.MapToIde("drill editor");
        Assert.True(a.Ok);
        Assert.Equal("get_editor_state", a.CommandId);
    }
}
