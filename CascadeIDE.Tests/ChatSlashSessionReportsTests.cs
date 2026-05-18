using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashSessionReportsTests
{
    private static readonly Guid MainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void TopicList_IncludesFlagsAndCounts()
    {
        var snapshot = SampleSnapshot();
        var text = ChatSlashSessionReports.FormatTopicList(snapshot);
        Assert.Contains("Темы сессии (2)", text);
        Assert.Contains("[main, active]", text);
        Assert.Contains("1 сообщ.", text);
    }

    [Fact]
    public void TopicTree_ShowsBranchUnderMain()
    {
        var snapshot = SampleSnapshot();
        var text = ChatSlashSessionReports.FormatTopicTree(snapshot);
        Assert.Contains("Дерево тем", text);
        Assert.Contains("Основная тема", text);
        Assert.Contains("Ветка", text);
    }

    [Fact]
    public void SpineList_Empty_ReturnsHint()
    {
        var text = ChatSlashSessionReports.FormatSpineList(ChatProductSpine.Empty);
        Assert.Contains("Spine пуст", text);
    }

    [Fact]
    public void SpineTree_WithMilestones_DrawsBranches()
    {
        var spine = new ChatProductSpine("Линия A", "Фокус B", ["M1", "M2"], true);
        var text = ChatSlashSessionReports.FormatSpineTree(spine);
        Assert.Contains("Линия A", text);
        Assert.Contains("M1", text);
        Assert.Contains("M2", text);
    }

    [Fact]
    public void Catalog_ResolvesTopicList_AsLocalIntercom()
    {
        var parse = ChatSlashCommandParser.TryParse("/topic list");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal(ChatSlashCommandExecutionKind.LocalIntercom, d.ExecutionKind);
        Assert.Equal("/topic list", d.SlashPath);
    }

    [Fact]
    public void Catalog_ResolvesTopicListText_AsLocalReport()
    {
        var parse = ChatSlashCommandParser.TryParse("/topic list text");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal(ChatSlashCommandExecutionKind.LocalReport, d.ExecutionKind);
        Assert.Equal("/topic list text", d.SlashPath);
    }

    [Fact]
    public void TryFormat_TopicListText_Route()
    {
        var snapshot = SampleSnapshot();
        var text = ChatSlashSessionReports.TryFormat("/topic list text", snapshot);
        Assert.NotNull(text);
        Assert.Contains("Темы сессии", text);
    }

    [Fact]
    public void Catalog_ReportRoutes_HaveReportHandler()
    {
        foreach (var route in IntentSlashCatalog.SlashRoutes.Values)
        {
            if (route.ExecutionKind != ChatSlashCommandExecutionKind.LocalReport)
                continue;

            Assert.False(string.IsNullOrWhiteSpace(route.ReportHandlerId), route.SlashPath);
            Assert.True(ChatSlashReportHandlers.IsKnown(route.ReportHandlerId!), route.SlashPath);
        }
    }

    [Fact]
    public void Catalog_IntercomRoutes_HaveIntercomHandler()
    {
        foreach (var route in IntentSlashCatalog.SlashRoutes.Values)
        {
            if (route.ExecutionKind != ChatSlashCommandExecutionKind.LocalIntercom)
                continue;

            Assert.False(string.IsNullOrWhiteSpace(route.IntercomHandlerId), route.SlashPath);
            Assert.True(ChatSlashIntercomHandlers.IsKnown(route.IntercomHandlerId!), route.SlashPath);
        }
    }

    [Fact]
    public void Catalog_ResolvesCard_AsLocalIntercomTopicCreate()
    {
        var parse = ChatSlashCommandParser.TryParse("/card My title");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal(ChatSlashCommandExecutionKind.LocalIntercom, d.ExecutionKind);
        Assert.Equal("/card", d.SlashPath);
        Assert.True(IntentSlashCatalog.TryGetRoute("/card", out var route));
        Assert.Equal(ChatSlashIntercomHandlers.Ids.TopicCreate, route.IntercomHandlerId);
    }

    private static ChatSurfaceSnapshot SampleSnapshot()
    {
        var main = new ChatThreadNode(MainId, "t-main", "Основная тема", true, true, null, null, 0, 0);
        var branch = new ChatThreadNode(BranchId, "t-branch", "Ветка", false, false, MainId, null, 1, 1);
        var msgMain = new ChatMessageNode(
            Guid.NewGuid(), "m1", MainId, null, 0, "user", "hi", false, false, null, null, null);
        var msgBranch = new ChatMessageNode(
            Guid.NewGuid(), "m2", BranchId, msgMain.MessageId, 1, "user", "branch", false, true, null, null, null);
        var state = new ChatSurfaceState(
            [main, branch],
            [msgMain, msgBranch],
            [],
            [],
            MainId,
            "Chat");
        var layout = new ChatSurfaceLayout(
            [
                new ChatThreadOverviewItem(MainId, "Основная тема", "hi", true, true, 0, 1),
                new ChatThreadOverviewItem(BranchId, "Ветка", "branch", false, false, 1, 1),
            ],
            [
                new ChatSurfaceLane(main, []),
                new ChatSurfaceLane(branch, []),
            ]);
        return new ChatSurfaceSnapshot(state, layout, ChatProductSpine.Empty);
    }
}
