using System.Buffers;
using System.Text;
using System.Text.Json;

namespace CascadeIDE.Services.Lsp;

/// <summary>Чтение/запись LSP по stdio (заголовок Content-Length + тело JSON UTF-8).</summary>
internal static class LspStdioFraming
{
    public static async Task<JsonDocument?> ReadMessageAsync(Stream input, CancellationToken ct)
    {
        int? length = null;
        while (true)
        {
            var line = await ReadHeaderLineAsync(input, ct).ConfigureAwait(false);
            if (line is null)
                return null;
            if (line.Length == 0)
                break;
            const string prefix = "Content-Length:";
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var n = line.AsSpan(prefix.Length).Trim();
                if (int.TryParse(n, out var len) && len >= 0)
                    length = len;
            }
        }

        if (length is null or <= 0)
            return null;

        var rent = ArrayPool<byte>.Shared.Rent(length.Value);
        try
        {
            var mem = rent.AsMemory(0, length.Value);
            var read = 0;
            while (read < length.Value)
            {
                var n = await input.ReadAsync(mem[read..], ct).ConfigureAwait(false);
                if (n == 0)
                    return null;
                read += n;
            }

            var copy = new byte[length.Value];
            rent.AsSpan(0, length.Value).CopyTo(copy);
            return JsonDocument.Parse(copy);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    private static async Task<string?> ReadHeaderLineAsync(Stream input, CancellationToken ct)
    {
        var buf = new List<byte>(64);
        while (true)
        {
            var b = new byte[1];
            var n = await input.ReadAsync(b, ct).ConfigureAwait(false);
            if (n == 0)
                return buf.Count == 0 ? null : Encoding.ASCII.GetString(buf.ToArray());
            if (b[0] == (byte)'\n')
            {
                var s = Encoding.ASCII.GetString(buf.ToArray());
                if (s.Length > 0 && s[^1] == '\r')
                    s = s[..^1];
                return s;
            }

            buf.Add(b[0]);
            if (buf.Count > 65536)
                return null;
        }
    }

    public static async Task WriteMessageAsync(Stream output, ReadOnlyMemory<byte> utf8Body, CancellationToken ct)
    {
        var header = Encoding.ASCII.GetBytes($"Content-Length: {utf8Body.Length}\r\n\r\n");
        await output.WriteAsync(header, ct).ConfigureAwait(false);
        await output.WriteAsync(utf8Body, ct).ConfigureAwait(false);
        await output.FlushAsync(ct).ConfigureAwait(false);
    }

    public static Task WriteJsonAsync(Stream output, JsonElement payload, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        return WriteMessageAsync(output, bytes, ct);
    }
}
