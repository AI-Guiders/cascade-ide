namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Загрузка <see cref="DataBusEventPolicy"/> из embedded TOML в манифесте сборки (без дискового оверлея).</summary>
public static class DataBusEventPolicyLoader
{
    /// <summary>Путь внутри манифеста <c>CascadeIDE</c> (см. <c>EmbeddedResource</c> в csproj).</summary>
    public const string BundledRelativePath = "Cockpit/DataBus/databus-event-policy.toml";

    private sealed class EventPolicyTomlRoot
    {
        public Dictionary<string, string>? Events { get; set; }
    }

    /// <summary>
    /// Читает манифест встроенного TOML (байты поставляются вместе со сборкой). Исключение — только баг: неверный
    /// <c>EmbeddedResource</c> в csproj либо неверное содержимое исходного файла, см. <see cref="BundledRelativePath"/>.
    /// </summary>
    public static DataBusEventPolicy Load()
    {
        if (!BundledAppContent.TryReadEmbeddedText(BundledRelativePath, out var text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                $"Embedded resource not in assembly manifest: {BundledRelativePath} (check EmbeddedResource in CascadeIDE.csproj).");

        if (!TryParse(text, out var policy))
            throw new InvalidOperationException(
                $"Fix {BundledRelativePath} in the repo: expected [events] with values burst or reliable.");

        return policy;
    }

    /// <summary>Для тестов: разбор TOML без I/O.</summary>
    internal static bool TryParse(string toml, out DataBusEventPolicy policy)
    {
        policy = default;
        try
        {
            var root = CascadeTomlSerializer.Deserialize<EventPolicyTomlRoot>(toml.Trim());
            if (root?.Events is not { Count: > 0 })
                return false;

            var map = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var kv in root.Events)
            {
                var name = kv.Key?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                var mode = kv.Value?.Trim();
                if (string.IsNullOrEmpty(mode))
                    continue;
                if (string.Equals(mode, "burst", StringComparison.OrdinalIgnoreCase))
                    map[name] = true;
                else if (string.Equals(mode, "reliable", StringComparison.OrdinalIgnoreCase))
                    map[name] = false;
            }

            if (map.Count == 0)
                return false;
            policy = new DataBusEventPolicy(map);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
