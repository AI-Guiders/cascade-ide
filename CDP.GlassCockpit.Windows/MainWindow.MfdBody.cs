#nullable enable

using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>MFD page select + latch glance / stub body (CabinGlass + SoftOrganMfdGlance).</summary>
public partial class MainWindow
{
    void SelectMfdPage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page) || MfdPages is null)
            return;

        foreach (var item in MfdPages.Items)
        {
            if (item is ListBoxItem lbi &&
                string.Equals(lbi.Content?.ToString(), page, StringComparison.OrdinalIgnoreCase))
            {
                MfdPages.SelectedItem = lbi;
                return;
            }
        }

        // 0-sync: CabinGlass may name a page before XAML list catches up — ensure selectable.
        var created = new ListBoxItem { Content = page.Trim() };
        MfdPages.Items.Add(created);
        MfdPages.SelectedItem = created;
    }

    void MfdPages_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateMfdBody();

    void UpdateMfdBody()
    {
        RefreshSolutionExplorerTree();
        RefreshMfdEditorVisibility();

        if (MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString() ?? "?";
        if (string.Equals(page, "Editor", StringComparison.OrdinalIgnoreCase)
            && ReferenceEquals(EditorChrome.Parent, MfdEditorHost))
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "SolutionExplorer", StringComparison.OrdinalIgnoreCase)
            && MfdSolutionExplorerTree is { Items.Count: > 0 })
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (SoftOrganMfdGlance.TryOrganIdForMfdPage(page) is { } organId
            && SoftOrganMfdGlance.TryFormatFromOrganId(organId) is { } glance)
        {
            MfdBody.Text = glance;
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "SolutionExplorer", StringComparison.OrdinalIgnoreCase)
            && GlassSolutionExplorerGlance.TryFormatFromWorkspaceRoot(_session.WorkspaceRoot) is { } seGlance)
        {
            MfdBody.Text = seGlance;
            RefreshEicasHealth();
            return;
        }

        MfdBody.Text = page switch
        {
            "Terminal" => "Terminal page host (Glass WPF).\n\nConPTY SSOT = CIDE Avalonia Views/TerminalMfdPageView + Features/Terminal (IntegratedShellLaunch).\nGlass stays SoftOrgan sys latch glance until a Windows terminal control peel.\n\n(sys latch glance missing — go=sys when seat live.)",
            "Build" => "Build page host (Glass WPF).\n\nMSBuild/output SSOT = CIDE Avalonia Views/BuildMfdPageView + Features/Build (BuildOutputPanelViewModel).\nGlass stays SoftOrgan toolchain latch glance until a WPF build-log host peel.\n\n(toolchain latch glance missing.)",
            "SolutionExplorer" => "Solution Explorer host (Glass WPF).\n\nNo .sln under workspace root yet.\nFull tree SSOT = CIDE Avalonia SolutionExplorerView.\nCabinGlass pin files_desk/explorer/fm → this MFD; SoftOrganKind.FilesDesk exists (FM utility ADR-0016) but SoftOrganMfdGlance unbound — Glass .sln TreeView/glance is instrument peel (do not overlay FM latch).\n\n(workspace: " + (_session.WorkspaceRoot ?? "?") + ")",
            "SemanticMap" => "Semantic Map host.\n\nGraph surface later (not adjacency dump).",
            "Tests" => "Tests page host (Glass WPF).\n\nLive host SSOT = CIDE Avalonia Views/TestsMfdPageView.\nGlass stays SoftOrgan test_desk latch glance until a WPF test-results host peel.\n\n(test_desk latch glance missing — go=test_desk when seat live.)",
            "DebugStack" => "DebugStack page host (Glass WPF).\n\nLive host SSOT = CIDE Avalonia Views/DebugStackMfdPageView.\nGlass stays SoftOrgan debug_desk latch glance until a WPF DAP stack host peel.\n\n(debug_desk latch glance missing — go=debug_desk when seat live.)",
            "HybridIndex" => "Hybrid Index host (Glass WPF).\n\nHCI live host SSOT = CIDE Avalonia Views/HybridIndexMfdPageView + Features/HybridIndex (HybridIndexOrchestrator).\nCabinGlass pin hybrid_index/hci/codebase_index → this MFD; no SoftOrganKind — SoftOrganMfdGlance unbound (do not invent SoftOrgan).\n\n(dig reject SoftOrgan glance — go=codebase_index_* / Avalonia HIS when live.)",
            "RelatedFiles" => "Related Files host (Glass WPF).\n\nSoftOrganMfdGlance ← refactor SoftOrgan (debt/blast latch).\nCabinGlass also pins find_desk/search → this MFD (+ chrome); SoftOrganKind.FindDesk DoD via pin — SoftOrganMfdGlance stays refactor (1:1 MFD map; search ≠ debt/blast).\n\n(find_usages host later.)",
            "Correspondence" => "Correspondence host (Glass WPF).\n\nCRS live host SSOT = CIDE Avalonia Views/CorrespondenceMfdPageView + WorkspaceNavigationMapViewModel.Correspondence (ADR 0155/0156) + cdp_analysis_scene feature=correspondence.\nGlass stays CabinGlass MFD pin stub until a WPF CRS host peel (do not fork Avalonia doc↔code surface into TextBlock; SoftOrganMfdGlance unbound — no SoftOrganKind for CRS).\n\n(Glass CRS peel deferred.)",
            "MarkdownPreview" => "Markdown Preview host (Glass WPF).\n\nLive host SSOT = CIDE Avalonia MarkdigMarkdownPreviewRenderer + MarkdownPreviewToolViewModel / MarkdownPreviewWindow (editor+chat preview). SoftOrganMfdGlance ← report SoftOrgan (domain/learn/onboard/evidence/pfd pins → this MFD). Glass stays latch glance + CabinGlass stub until a WPF md host peel (do not fork Markdig Avalonia Control into TextBlock).\n\n(Glass MarkdownPreview peel deferred.)",
            "WebAiPortal" => "Web / AI Portal host (Glass WPF).\n\nLive host SSOT = CIDE Avalonia Views/WebAiPortalMfdPageView + Features/WebAiPortal (WebAiPortalCommandBridge, ADR 0108 WebView tool bridge) + show_web_ai_portal_page. SoftOrganMfdGlance unbound (no SoftOrganKind for portal). Glass stays CabinGlass MFD pin stub until a WPF WebView2 portal peel (do not fork Avalonia portal into TextBlock; CDP browser organ ≠ this MFD).\n\n(Glass WebAiPortal peel deferred.)",
            "AiChatSettings" => "AI Chat Settings host.\n\noptions / ignite / mcp SoftOrgan projection (settings.toml SSOT).",
            "WorkspaceHealth" => "Workspace Health host.\n\nCIDE MfdShellPage orphan — Glass stub (0-sync reverse).",
            "EnvironmentReadiness" => "Environment Readiness host.\n\nCIDE MfdShellPage orphan — LSP/dotnet glance stub.",
            "Events" => "Events host.\n\nCIDE MfdShellPage orphan — Glass stub (0-sync reverse).",
            "Hypotheses" => "Hypotheses host.\n\nCIDE MfdShellPage orphan — Glass stub (0-sync reverse).",
            "Editor" => _session.IsIntercomForward
                ? "Editor page — AvalonEdit mounts here when Forward=intercom (ADR 0120)."
                : "Editor is on Forward (primary_work_surface=editor).",
            "Chat" => "Chat/Intercom also on M when needed; primary Intercom is Forward.",
            _ => $"{page} page host.\n\nInstrument content peels later. (CabinGlass catalog may select this.)"
        };
        RefreshEicasHealth();
    }
}
