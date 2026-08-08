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

    /// <summary>CIDE <c>Workspace.SolutionPath</c> — .sln/.csproj/.fsproj or folder.</summary>
    public string? SolutionPath { get; private set; }

    /// <summary>CIDE solution tree root from <see cref="SolutionParser"/> / <see cref="FolderWorkspaceTreeBuilder"/>.</summary>
    public SolutionItem? SolutionRoot { get; private set; }

    public string? SolutionLoadError { get; private set; }

    public GlassPresentationLayout.Snapshot Layout { get; private set; }

    public GlassSession(string? workspaceRoot = null)
    {
        SettingsPath = UserSettingsPaths.GetSettingsFilePath();
        WorkspaceRoot = string.IsNullOrWhiteSpace(workspaceRoot)
            ? WorkspaceCascadePaths.TryDiscoverWorkspaceRoot()
            : workspaceRoot.Trim();
        SolutionPath = null;
        SolutionRoot = null;
        SolutionLoadError = null;
        Settings = SettingsService.Load(WorkspaceRoot);
        Layout = GlassPresentationLayout.Resolve(Settings);
    }

    public void ReloadSettings()
    {
        Settings = SettingsService.Load(WorkspaceRoot);
        Layout = GlassPresentationLayout.Resolve(Settings, Layout.Topology);
    }

    /// <summary>CIDE open-folder path: <see cref="FolderWorkspaceTreeBuilder.TryBuild"/>.</summary>
    public bool SetWorkspaceRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var root = path.Trim();
        if (!Directory.Exists(root))
            return false;

        var tree = FolderWorkspaceTreeBuilder.TryBuild(root, out var err);
        if (tree is null)
        {
            SolutionLoadError = err ?? "Не удалось открыть папку.";
            return false;
        }

        WorkspaceRoot = root;
        SolutionPath = root;
        SolutionRoot = tree;
        SolutionLoadError = null;
        ReloadSettings();
        return true;
    }

    /// <summary>
    /// CIDE <c>LoadSolution</c> SSOT: <see cref="SolutionParser.Load"/> for file,
    /// <see cref="FolderWorkspaceTreeBuilder"/> for directory.
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

        var root = SolutionParser.Load(trimmed, out var err);
        if (root is null)
        {
            SolutionLoadError = err ?? "Не удалось загрузить решение.";
            return false;
        }

        SolutionPath = root.FullPath ?? trimmed;
        SolutionRoot = root;
        SolutionLoadError = null;
        // Same as CIDE WorkspaceDirectoryFromSolutionPath.Resolve
        var ws = Path.GetDirectoryName(CanonicalFilePath.Normalize(SolutionPath)) ?? "";
        if (string.IsNullOrWhiteSpace(ws) || !Directory.Exists(ws))
            return false;
        WorkspaceRoot = ws;
        ReloadSettings();
        return true;
    }

    public GlassPresentationLayout.Snapshot ApplyTopology(string? topology)
    {
        if (!string.IsNullOrWhiteSpace(topology))
            Layout = GlassPresentationLayout.Resolve(Settings, topology);
        else
            Layout = GlassPresentationLayout.Resolve(Settings);
        return Layout;
    }

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
