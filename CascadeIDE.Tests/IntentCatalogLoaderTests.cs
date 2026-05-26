using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntentCatalogLoaderTests
{
    [Fact]
    public void BundledCatalog_HasTopicOpen_WithAutoRun()
    {
        Assert.True(IntentSlashCatalog.TryGetRoute("/intercom topic open", out var route));
        Assert.True(route.AutoRunOnCommit);
        Assert.False(route.AutoRunRequiresArgs);
        Assert.Equal("topic_open", route.IntercomHandlerId);
    }

    [Fact]
    public void ParseBundleForTests_RejectsDuplicateSlashPath()
    {
        const string toml = """
            [[command]]
            command_id = "x"
            [[command.form.slash]]
            path = "/dup"
            help = "one"
            [[command]]
            command_id = "y"
            [[command.form.slash]]
            path = "/dup"
            help = "two"
            """;

        Assert.Throws<InvalidOperationException>(() => IntentMelodyAliases.ParseBundleForTests(toml));
    }
}
