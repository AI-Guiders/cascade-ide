using System.Text.Json;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>
/// Стабильный ключ для того, чтобы не выполнять одну и ту же команду со страницы в цикле poll.
/// </summary>
internal static class WebAiPortalBridgePayloadDedup
{
    private static readonly JsonSerializerOptions Canonical =
        new()
        {
            WriteIndented = false,
        };

    /// <summary>
    /// Детерминированная строка по разобранному JSON (пробелы/порядок полей на входе не должны плодить дублей).
    /// </summary>
    public static bool TryCanonicalKey(string jsonPayload, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? key)
    {
        key = null;
        try
        {
            using var doc = JsonDocument.Parse(jsonPayload);
            key = JsonSerializer.Serialize(doc.RootElement, Canonical);
            return !string.IsNullOrEmpty(key);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
