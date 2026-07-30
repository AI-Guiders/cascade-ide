#nullable enable
using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CDP.GlassCockpit.Windows;

/// <summary>Shared CIDE settings + live topology for the WPF glass host (typed SSOT via GlassCore).</summary>
internal sealed class GlassSession
{
    public CascadeIdeSettings Settings { get; private set; }
    public string SettingsPath { get; private set; }
    public string? WorkspaceRoot { get; private set; }
    public GlassPresentationLayout.Snapshot Layout { get; private set; }

    public GlassSession(string? workspaceRoot = null)
    {
        SettingsPath = UserSettingsPaths.GetSettingsFilePath();
        WorkspaceRoot = string.IsNullOrWhiteSpace(workspaceRoot)
            ? WorkspaceCascadePaths.TryDiscoverWorkspaceRoot()
            : workspaceRoot.Trim();
        Settings = SettingsService.Load(WorkspaceRoot);
        Layout = GlassPresentationLayout.Resolve(Settings);
    }

    public void ReloadSettings()
    {
        Settings = SettingsService.Load(WorkspaceRoot);
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
        !string.Equals(Settings.Workspace.PrimaryWorkSurface, "editor", StringComparison.OrdinalIgnoreCase);
}
