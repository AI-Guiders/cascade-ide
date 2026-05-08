using System.Threading.Channels;
using CascadeIDE.Services;
using Xunit;
using TestContext = Xunit.TestContext;

namespace CascadeIDE.Tests;

public sealed class BuildLogIngestionTests
{
    [Fact]
    public async Task DrainToAppendAsync_Single_Small_Chunk_One_Append_At_End()
    {
        var ch = Channel.CreateUnbounded<string>();
        var parts = new List<string>();
        var drain = BuildLogIngestion.DrainToAppendAsync(
            ch.Reader, s => parts.Add(s), maxBatchChars: 10_000, cancellationToken: TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("hello", TestContext.Current.CancellationToken);
        ch.Writer.Complete();
        await drain;
        Assert.Equal(new[] { "hello" }, parts);
    }

    [Fact]
    public async Task DrainToAppendAsync_Flushes_When_Threshold_Reached()
    {
        var ch = Channel.CreateUnbounded<string>();
        var parts = new List<string>();
        var drain = BuildLogIngestion.DrainToAppendAsync(
            ch.Reader, s => parts.Add(s), maxBatchChars: 10, cancellationToken: TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("1234567890", TestContext.Current.CancellationToken);
        ch.Writer.Complete();
        await drain;
        Assert.Equal(new[] { "1234567890" }, parts);
    }

    [Fact]
    public async Task DrainToAppendAsync_Multiple_Chunks_Combine_Until_Threshold_Then_Spill()
    {
        var ch = Channel.CreateUnbounded<string>();
        var parts = new List<string>();
        var drain = BuildLogIngestion.DrainToAppendAsync(
            ch.Reader, s => parts.Add(s), maxBatchChars: 5, cancellationToken: TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("12", TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("345", TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("6789", TestContext.Current.CancellationToken);
        ch.Writer.Complete();
        await drain;
        Assert.Equal(new[] { "12345", "6789" }, parts);
    }

    [Fact]
    public async Task DrainToAppendAsync_Empty_Skips_And_Still_Finishes()
    {
        var ch = Channel.CreateUnbounded<string>();
        var parts = new List<string>();
        var drain = BuildLogIngestion.DrainToAppendAsync(
            ch.Reader, s => parts.Add(s), maxBatchChars: 100, cancellationToken: TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("", TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("x", TestContext.Current.CancellationToken);
        ch.Writer.Complete();
        await drain;
        Assert.Equal(new[] { "x" }, parts);
    }

    [Fact]
    public async Task OnEachDequeuedChunk_Receives_Every_Chunk_Independently_Of_Batch()
    {
        var ch = Channel.CreateUnbounded<string>();
        var cat = new System.Text.StringBuilder();
        var drain = BuildLogIngestion.DrainToAppendAsync(
            ch.Reader,
            static _ => { },
            maxBatchChars: 10_000,
            onEachDequeuedChunk: c => cat.Append(c),
            cancellationToken: TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("a", TestContext.Current.CancellationToken);
        await ch.Writer.WriteAsync("b", TestContext.Current.CancellationToken);
        ch.Writer.Complete();
        await drain;
        Assert.Equal("ab", cat.ToString());
    }

    [Fact]
    public async Task CreateBuildLogChannel_Can_Pass_Through_Single_Chunk()
    {
        var c = BuildLogIngestion.CreateBuildLogChannel(1);
        await c.Writer.WriteAsync("x", TestContext.Current.CancellationToken);
        c.Writer.Complete();
        var r = await c.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("x", r);
    }
}
