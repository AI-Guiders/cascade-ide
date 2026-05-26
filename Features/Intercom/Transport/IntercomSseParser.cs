using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace CascadeIDE.Features.Intercom.Transport;

internal static class IntercomSseParser
{
    public static async IAsyncEnumerable<IntercomTransportEventEnvelopeDto> ReadEnvelopesAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var dataLines = new List<string>();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
                break;

            if (line.Length == 0)
            {
                if (dataLines.Count == 0)
                    continue;

                var json = string.Join('\n', dataLines);
                dataLines.Clear();
                IntercomTransportEventEnvelopeDto? envelope;
                try
                {
                    envelope = JsonSerializer.Deserialize<IntercomTransportEventEnvelopeDto>(json, IntercomTransportJson.Web);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (envelope is not null)
                    yield return envelope;
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
                dataLines.Add(line["data:".Length..].TrimStart());
        }
    }
}
