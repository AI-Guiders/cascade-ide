using System.Text.Json;
using Tomlyn;

namespace CascadeIDE.Services;

/// <summary>Паритет с Tomlyn 1.x <c>Toml.ToModel</c>: ключи в TOML в snake_case, свойства моделей в PascalCase.</summary>
internal static class CascadeTomlSerializer
{
    public static readonly TomlSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static T? Deserialize<T>(string text) => TomlSerializer.Deserialize<T>(text, Options);

    public static string Serialize<T>(T value) => TomlSerializer.Serialize(value, Options);
}
