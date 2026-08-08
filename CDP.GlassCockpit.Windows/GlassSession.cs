#nullable enable
using System.IO;
using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Presentation;

namespace CDP.GlassCockpit.Windows;

/// <summary>Shared CIDE settings + live topology for the WPF glass host (typed SSOT via GlassCore).</summary>
internal sealed class GlassSession
{
    public CascadeIdeSettings Settings { get; private set; }
    public string SettingsPath { get; private set; }
    public string? WorkspaceRoot { get; private set; }

    /// <summary>CIDE <c>Workspace.SolutionPath</c> peel — .sln/.csproj/.fsproj or folder path.</summary>
    public string? SolutionPath { get; private set; }

    public GlassPresentationLayout.Snapshot Layout { get; private set; }

    public GlassSession(string? workspaceRoot = null)
    {
        SettingsPath = UserSettingsPaths.GetSettingsFilePath();
        WorkspaceRoot = string.IsNullOrWhiteSpace(workspaceRoot)
            ? WorkspaceCascadePaths.TryDiscoverWorkspaceRoot()
            : workspaceRoot.Trim();
        SolutionPath = null;
        Settings = SettingsService.Load(WorkspaceRoot);
        Layout = GlassPresentationLayout.Resolve(Settings);
    }

    public void ReloadSettings()
    {
        Settings = SettingsService.Load(WorkspaceRoot);
        Layout = GlassPresentationLayout.Resolve(Settings, Layout.Topology);
    }

    /// <summary>Open folder as workspace (CIDE <c>FolderWorkspaceTreeBuilder</c> path).</summary>
    public bool SetWorkspaceRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var root = path.Trim();
        if (!Directory.Exists(root))
            return false;
        WorkspaceRoot = root;
        SolutionPath = root;
        ReloadSettings();
        return true;
    }

    /// <summary>
    /// CIDE <c>LoadSolution</c> peel: keep file as <see cref="SolutionPath"/>;
    /// workspace dir = directory of that file (same rule as <c>WorkspaceDirectoryFromSolutionPath</c>).
    /// </summary>
    public bool SetSolutionOrProjectPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var trimmed = path.Trim();
        if (Directory.Exists(trimmed))
            return SetWorkspaceRoot(trimmed);
        if (!File.Exists(trimmed))
            return false;

        SolutionPath = trimmed;
        // Same semantics as Features/Workspace/Application/WorkspaceDirectoryFromSolutionPath.Resolve
        var ws = Path.GetDirectoryName(CanonicalFilePath.Normalize(trimmed)) ?? "";
        if (string.IsNullOrWhiteSpace(ws) || !Directory.Exists(ws))
            return false;
        WorkspaceRoot = ws;
        ReloadSettings();
        return true;
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

    /// <summary>
    /// Keep session SSOT in sync with live single-TopLevel OneOf XOR
    /// (<see cref="GlassHostWindows.PreferSurface"/>) so /status cols match paint.
    /// </summary>
    public void PatchScanOneOfActive(string surface)
    {
        var s = surface.Trim().ToLowerInvariant();
        if (s.Length == 0)
            return;

        var cols = GlassPresentationLayout.ColumnDefsForScanOneOfActive(s);
        var pack = Layout.SurfacePack;
        if (pack is { IsSuccess: true, Slots: [{ Role: PresentationScanRole.PmOneOf } slot] }
            && slot.Stack.Contains(s, StringComparer.Ordinal))
        {
            pack = PresentationSurfacePack.Ok([slot with { Active = s }]);
        }

        Layout = Layout with { ColumnDefinitions = cols, SurfacePack = pack };
    }

    public bool IsIntercomForward =>
        !string.Equals(Settings.Workspace.PrimaryWorkSurface, "editor", StringComparison.OrdinalIgnoreCase);
}
