#nullable enable
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// DAL: %LocalAppData%/cdp-mcp latch paths shared by Avalonia projectors and WPF glass.
/// File watchers/JSON apply stay in hosts — paths only.
/// </summary>
[IoBoundary]
public static class CdpHabitatPaths
{
    public const string FolderName = "cdp-mcp";
    public const string PresentationLatchFileName = "presentation-LATEST.json";
    public const string IntercomLatchFileName = "intercom-LATEST.json";
    /// <summary>Partner presence (idle|composing|busy) — not voice journal.</summary>
    public const string IntercomPresenceLatchFileName = "intercom-presence-LATEST.json";
    public const string SeatsLatchFileName = "seats-LATEST.json";
    public const string LandLatchFileName = "land-LATEST.json";
    public const string SharedLatchFileName = "shared-LATEST.json";
    public const string DiskLatchFileName = "disk-LATEST.json";
    /// <summary>Agent surface RPC request (CDP → Glass). Not SoftOrgan chrome.</summary>
    public const string SurfaceCmdLatchFileName = "surface-cmd-LATEST.json";
    /// <summary>Agent surface RPC reply (Glass → CDP).</summary>
    public const string SurfaceReplyLatchFileName = "surface-reply-LATEST.json";
    /// <summary>Glass Intercom → habitat citizen dialog request (poll by cdp-mcp bridge).</summary>
    public const string CitizenDialogRequestLatchFileName = "citizen-dialog-request-LATEST.json";
    /// <summary>Last AutoI wake charge (composer|habitat) — Glass Autoi consumer.</summary>
    public const string IgniteWakeLatchFileName = "ignite-wake-LATEST.json";

    public static string StateRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            FolderName);

    public static string GetLatchPath(string fileName) => Path.Combine(StateRoot, fileName);

    public static string PresentationLatchPath => GetLatchPath(PresentationLatchFileName);

    public static string IntercomLatchPath => GetLatchPath(IntercomLatchFileName);

    public static string IntercomPresenceLatchPath => GetLatchPath(IntercomPresenceLatchFileName);

    public static string SeatsLatchPath => GetLatchPath(SeatsLatchFileName);

    public static string LandLatchPath => GetLatchPath(LandLatchFileName);

    public static string SharedLatchPath => GetLatchPath(SharedLatchFileName);

    public static string DiskLatchPath => GetLatchPath(DiskLatchFileName);

    public static string SurfaceCmdLatchPath => GetLatchPath(SurfaceCmdLatchFileName);

    public static string SurfaceReplyLatchPath => GetLatchPath(SurfaceReplyLatchFileName);

    public static string CitizenDialogRequestLatchPath => GetLatchPath(CitizenDialogRequestLatchFileName);

    public static string IgniteWakeLatchPath => GetLatchPath(IgniteWakeLatchFileName);

    /// <summary>Ensure state root exists; returns <see cref="StateRoot"/>.</summary>
    public static string EnsureStateRoot()
    {
        var root = StateRoot;
        Directory.CreateDirectory(root);
        return root;
    }
}
