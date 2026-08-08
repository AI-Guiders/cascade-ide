#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.SoftOrgan;

/// <summary>Glass → habitat DAP command latch (CDP/netcoredbg consumer). Avalonia-free.</summary>
public static class GlassDapCommandBridge
{
    public const string Schema = "cide_debug_cmd_latch/v1";
    public const string OriginGlass = "glass";
    public const string LatchFileName = "debug-cmd-LATEST.json";

    public const string Continue = "continue";
    public const string StepInto = "step_into";
    public const string StepOver = "step_over";
    public const string StepOut = "step_out";
    public const string Variables = "variables";
    /// <summary>CIDE <c>debug_launch</c> / habitat DAP start — Glass publishes; CDP consumes.</summary>
    public const string Launch = "launch";

    static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Test hook: redirect latch root.</summary>
    public static string? RootOverrideForTests { get; set; }

    public static string StateRoot =>
        RootOverrideForTests
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "cdp-mcp");

    public static string LatchPath => Path.Combine(StateRoot, LatchFileName);

    public static bool TryPublish(string command, int? frameIndex = null)
    {
        var cmd = (command ?? "").Trim().ToLowerInvariant();
        if (cmd.Length == 0)
            return false;

        try
        {
            Directory.CreateDirectory(StateRoot);
            var doc = new DebugCmdLatchDoc
            {
                Schema = Schema,
                Origin = OriginGlass,
                StampedUtc = DateTimeOffset.UtcNow,
                Command = cmd,
                FrameIndex = frameIndex
            };
            var json = JsonSerializer.Serialize(doc, WriteOpts);
            var tmp = LatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, LatchPath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryPublishVariables(int frameIndex) =>
        TryPublish(Variables, Math.Max(0, frameIndex));

    public sealed class DebugCmdLatchDoc
    {
        public string Schema { get; set; } = GlassDapCommandBridge.Schema;
        public string Origin { get; set; } = OriginGlass;
        public DateTimeOffset StampedUtc { get; set; }
        public string Command { get; set; } = "";
        public int? FrameIndex { get; set; }
    }
}
