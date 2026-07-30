#nullable enable
using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.GlassCore.Settings;

namespace CDP.GlassCockpit.Windows;

/// <summary>Shared CIDE settings + live topology for the WPF glass host.</summary>
internal sealed class GlassSession
{
    public IdeGlassSettings Settings { get; private set; }
    public GlassPresentationLayout.Snapshot Layout { get; private set; }

    public GlassSession(string? workspaceRoot = null)
    {
        Settings = IdeGlassSettings.Load(workspaceRoot: workspaceRoot);
        Layout = GlassPresentationLayout.Resolve(Settings);
    }

    public void ReloadSettings()
    {
        Settings = IdeGlassSettings.Load(
            settingsPath: Settings.SettingsPath,
            workspaceRoot: Settings.WorkspaceRoot);
        Layout = GlassPresentationLayout.Resolve(Settings, Layout.Topology);
    }

    /// <summary>Apply live presentation latch topology (same patch idea as CIDE ApplyPresentationGlassPatch).</summary>
    public GlassPresentationLayout.Snapshot ApplyTopology(string? topology)
    {
        if (!string.IsNullOrWhiteSpace(topology))
            Layout = GlassPresentationLayout.Resolve(Settings, topology);
        else
            Layout = GlassPresentationLayout.Resolve(Settings);
        return Layout;
    }

    public bool IsIntercomForward =>
        !string.Equals(Settings.PrimaryWorkSurface, "editor", StringComparison.OrdinalIgnoreCase);
}
