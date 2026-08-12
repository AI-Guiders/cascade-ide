#nullable enable

using System.Text;
using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.SoftInstrument;

/// <summary>Glass MFD AiChatSettings — read-only settings.toml peel (provider/model/MCP mounts).</summary>
public static class GlassAiChatSettingsGlance
{
    public readonly record struct Snapshot(
        string SettingsPath,
        string Mode,
        string Provider,
        string Model,
        string McpMounts,
        string? RawToml);

    public static Snapshot TryLoad(string? workspaceRoot)
    {
        var settings = SettingsService.Load(workspaceRoot);
        var path = UserSettingsPaths.GetSettingsFilePath();
        UserSettingsTomlFileAccess.TryRead(out var raw, out _);

        var ai = settings.Ai;
        var mode = AiSettings.NormalizeMode(ai.Mode);
        var provider = ai.ResolveEffectiveProviderUiKey();
        var model = ResolveModel(ai);
        var mcp = FormatMcpMounts(settings.Mcp);

        return new Snapshot(path, mode, provider, model, mcp, raw);
    }

    public static string FormatHeader(Snapshot snap) =>
        $"ai · {snap.Mode} · {snap.Provider} · {snap.Model} · mcp={snap.McpMounts}";

    public static string FormatBody(Snapshot snap)
    {
        var sb = new StringBuilder();
        sb.AppendLine(FormatHeader(snap));
        sb.AppendLine($"path · {snap.SettingsPath}");
        sb.AppendLine();
        sb.AppendLine(snap.RawToml ?? "(settings.toml missing — defaults only)");
        return sb.ToString().TrimEnd();
    }

    static string ResolveModel(AiSettings ai) =>
        AiSettings.NormalizeMode(ai.Mode) switch
        {
            "local" => ai.Local.Ollama.Model,
            "acp" => string.IsNullOrWhiteSpace(ai.Acp.CursorAcpModelId) ? "(cursor-acp)" : ai.Acp.CursorAcpModelId,
            "cloud" => AiSettings.NormalizeCloudProvider(ai.Cloud.ActiveProvider),
            _ => ai.Local.Ollama.Model,
        };

    static string FormatMcpMounts(McpSettings mcp)
    {
        var json = mcp.ExternalServersJson?.Trim() ?? "[]";
        if (json.Length <= 2)
            return mcp.AcpAutoInjectIdeMcp ? "cascade-ide (auto)" : "none";

        var count = json.Count(c => c == '{');
        var auto = mcp.AcpAutoInjectIdeMcp ? "+cascade-ide" : "";
        return count > 0 ? $"{count} external{auto}" : auto.TrimStart('+');
    }
}
