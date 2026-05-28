#nullable enable

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Канон слоёв и видов связи correspondence (ADR 0155 §6), <c>wire/correspondence/correspondence-kinds.v1.json</c>.</summary>
public static class CorrespondenceKindsCatalog
{
    private static CorrespondenceKindsDocument? _cached;

    public static CorrespondenceKindsDocument Load()
    {
        if (_cached is not null)
            return _cached;

        var asm = Assembly.GetExecutingAssembly();
        const string resourceName = "CascadeIDE.wire.correspondence.correspondence-kinds.v1.json";
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource {resourceName}.");
        _cached = JsonSerializer.Deserialize<CorrespondenceKindsDocument>(stream, JsonOptions)
            ?? throw new InvalidOperationException("correspondence-kinds.v1.json deserialized to null.");
        return _cached;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<CorrespondenceLayerDefinition> Layers => Load().Layers;

    public static bool TryGetLayer(string layerId, out CorrespondenceLayerDefinition layer)
    {
        layer = Layers.FirstOrDefault(l => string.Equals(l.Id, layerId, StringComparison.OrdinalIgnoreCase))
            ?? new CorrespondenceLayerDefinition("", "", "", "");
        return layer.Id.Length > 0;
    }
}

public sealed class CorrespondenceKindsDocument
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("layers")]
    public List<CorrespondenceLayerDefinition> Layers { get; init; } = [];

    [JsonPropertyName("kinds")]
    public List<CorrespondenceKindDefinition> Kinds { get; init; } = [];
}

public sealed record CorrespondenceLayerDefinition(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("adr")] string Adr);

public sealed class CorrespondenceKindDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("family")]
    public string Family { get; init; } = "";

    [JsonPropertyName("layers")]
    public List<string> Layers { get; init; } = [];

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";
}
