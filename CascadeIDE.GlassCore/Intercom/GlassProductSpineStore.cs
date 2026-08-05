#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.Intercom;

/// <summary>Glass latch for ADR 0096 product spine — toolkit-agnostic JSON under habitat StateRoot.</summary>
public static class GlassProductSpineStore
{
    public const string LatchFileName = "product-spine-LATEST.json";

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string LatchPath => CdpHabitatPaths.GetLatchPath(LatchFileName);

    public static ChatProductSpine LoadOrEmpty()
    {
        try
        {
            var path = LatchPath;
            if (!File.Exists(path))
                return ChatProductSpine.Empty;
            var dto = JsonSerializer.Deserialize<SpineDto>(File.ReadAllText(path), JsonOpts);
            if (dto is null)
                return ChatProductSpine.Empty;
            var milestones = dto.Milestones ?? [];
            return new ChatProductSpine(
                dto.LineTitle ?? "",
                dto.CurrentFocus ?? "",
                milestones,
                dto.IncludeInAgentContext);
        }
        catch
        {
            return ChatProductSpine.Empty;
        }
    }

    public static void Save(ChatProductSpine spine)
    {
        var path = LatchPath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var dto = new SpineDto(
            spine.LineTitle,
            spine.CurrentFocus,
            spine.Milestones.ToList(),
            spine.IncludeInAgentContext);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(dto, JsonOpts));
        File.Move(tmp, path, overwrite: true);
    }

    public static string FormatStrip(ChatProductSpine spine)
    {
        if (!spine.HasContent)
            return "";
        var title = ChatProductSpinePresentation.ResolveLineTitle(spine);
        var focus = ChatProductSpinePresentation.FormatDetailStripFocus(spine.CurrentFocus);
        return title + " · " + focus;
    }

    sealed record SpineDto(
        string? LineTitle,
        string? CurrentFocus,
        List<string>? Milestones,
        bool IncludeInAgentContext);
}
