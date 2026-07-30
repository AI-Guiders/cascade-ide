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

    public static string StateRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            FolderName);

    public static string GetLatchPath(string fileName) => Path.Combine(StateRoot, fileName);

    public static string PresentationLatchPath => GetLatchPath(PresentationLatchFileName);

    public static string IntercomLatchPath => GetLatchPath(IntercomLatchFileName);

    /// <summary>Ensure state root exists; returns <see cref="StateRoot"/>.</summary>
    public static string EnsureStateRoot()
    {
        var root = StateRoot;
        Directory.CreateDirectory(root);
        return root;
    }
}
