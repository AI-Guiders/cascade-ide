using System.Text;
using System.Threading.Channels;

namespace CascadeIDE.Services;

/// <summary>
/// Чтение кусков лога из канала (stdout/stderr процесса) с батчированием обратных вызовов
/// <paramref name="append"/>, чтобы не вызывать UI на каждый мелкий read (ADR 0094).
/// </summary>
public static class BuildLogIngestion
{
    /// <summary>Дефолт: очередь «кусков» строки; при <see cref="BoundedChannelFullMode.Wait"/> — backpressure к продюсеру (помпы процесса).</summary>
    public const int DefaultChannelChunkCapacity = 32;

    /// <summary>Один писатель, один читатель, <see cref="BoundedChannelFullMode.Wait"/>: наполненная очередь тормозит <c>WriteAsync</c> до снятия с хвоста.</summary>
    public static Channel<string> CreateBuildLogChannel(int capacity = DefaultChannelChunkCapacity) =>
        Channel.CreateBounded<string>(new BoundedChannelOptions(Math.Max(1, capacity))
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

    /// <summary>
    /// Читает все элементы из <paramref name="reader"/>; при накоплении не менее
    /// <paramref name="maxBatchChars"/> символов сбрасывает пачку в <paramref name="append"/>.
    /// <paramref name="onEachDequeuedChunk"/> (если задан) вызывается на каждом снятом с канала куске
    /// — для накопления полного лога (MCP, парсеры) параллельно батчу в панель.
    /// </summary>
    public static async Task DrainToAppendAsync(
        ChannelReader<string> reader,
        Action<string> append,
        int maxBatchChars = 8192,
        Action<string>? onEachDequeuedChunk = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(append);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBatchChars, 1);

        var batch = new StringBuilder();
        var size = 0;

        await foreach (var chunk in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (chunk.Length == 0)
                continue;

            onEachDequeuedChunk?.Invoke(chunk);
            batch.Append(chunk);
            size += chunk.Length;
            if (size < maxBatchChars)
                continue;

            append(batch.ToString());
            batch.Clear();
            size = 0;
        }

        if (batch.Length > 0)
            append(batch.ToString());
    }
}
