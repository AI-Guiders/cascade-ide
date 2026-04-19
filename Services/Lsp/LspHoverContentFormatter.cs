using System.Text;
using System.Text.Json;

namespace CascadeIDE.Services.Lsp;

/// <summary>Парсинг <c>Hover.contents</c> из ответа LSP (<see cref="textDocument/hover"/>).</summary>
internal static class LspHoverContentFormatter
{
    internal static string? Format(JsonElement contents)
    {
        return contents.ValueKind switch
        {
            JsonValueKind.String => contents.GetString(),
            JsonValueKind.Array => FormatArray(contents),
            JsonValueKind.Object => FormatObject(contents),
            _ => null,
        };
    }

    private static string? FormatArray(JsonElement arr)
    {
        var sb = new StringBuilder();
        foreach (var item in arr.EnumerateArray())
        {
            var part = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Object when item.TryGetProperty("value", out var v) => v.GetString(),
                _ => null,
            };
            if (!string.IsNullOrEmpty(part))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(part);
            }
        }

        return sb.Length == 0 ? null : sb.ToString();
    }

    private static string? FormatObject(JsonElement obj)
    {
        if (obj.TryGetProperty("value", out var v))
            return v.GetString();
        return null;
    }
}
