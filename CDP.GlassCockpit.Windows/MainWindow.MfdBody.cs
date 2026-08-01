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
            "Terminal" => FormatMfdStub("Terminal", "TerminalMfdPageView · ConPTY", "sys latch · go=sys"),
            "Build" => FormatMfdStub("Build", "BuildMfdPageView", "toolchain latch"),
            "SolutionExplorer" => FormatMfdStub(
                "SolutionExplorer",
                "SolutionExplorerView",
                "no .sln · " + (_session.WorkspaceRoot ?? "?")),
            "SemanticMap" => FormatMfdStub("SemanticMap", "WorkspaceNavigationMapView · Skia", "arch latch when live"),
            "Problems" => FormatMfdStub("Problems", "ProblemsMfdPageView", "review latch when live"),
            "Tests" => FormatMfdStub("Tests", "TestsMfdPageView", "test_desk · go=test_desk"),
            "DebugStack" => FormatMfdStub("DebugStack", "DebugStackMfdPageView", "debug_desk · go=debug_desk"),
            "HybridIndex" => FormatMfdStub("HybridIndex", "HybridIndexMfdPageView", "no SoftOrgan · go=codebase_index_*"),
            "RelatedFiles" => FormatMfdStub("RelatedFiles", "RelatedFilesMfdPageView", "refactor latch when live"),
            "Correspondence" => FormatMfdStub("Correspondence", "CorrespondenceMfdPageView", "CRS · no SoftOrgan"),
            "MarkdownPreview" => FormatMfdStub("MarkdownPreview", "MarkdigMarkdownPreviewRenderer", "report latch when live"),
            "WebAiPortal" => FormatMfdStub("WebAiPortal", "WebAiPortalMfdPageView", "WebView2 peel deferred"),
            "AiChatSettings" => FormatMfdStub("AiChatSettings", "options/ignite/mcp SoftOrgan", "settings.toml SSOT"),
            "WorkspaceHealth" => FormatMfdStub("WorkspaceHealth", "CIDE orphan page", "Glass stub"),
            "EnvironmentReadiness" => FormatMfdStub("EnvironmentReadiness", "CIDE orphan page", "LSP/dotnet glance"),
            "Events" => FormatMfdStub("Events", "CIDE orphan page", "Glass stub"),
            "Hypotheses" => FormatMfdStub("Hypotheses", "CIDE orphan page", "Glass stub"),
            "Editor" => _session.IsIntercomForward
                ? FormatMfdStub("Editor", "AvalonEdit here when Forward=intercom", "ADR 0120")
                : FormatMfdStub("Editor", "on Forward", "primary_work_surface=editor"),
            "Chat" => GlassIntercomPresence.FormatChatMfdGlance(),
            _ => FormatMfdStub(page, "instrument peel later", "CabinGlass may select")
        };
        RefreshEicasHealth();
    }

    /// <summary>Human MFD card: concise + graphic presence (not text-wall dig notes).</summary>
    static string FormatMfdStub(string title, string liveHost, string note) =>
        $"{title}\n" +
        "┌ status ──────────────┐\n" +
        "│ □ Glass peel         │\n" +
        $"│ ■ Avalonia · {TrimCard(liveHost)}\n" +
        $"│ · {TrimCard(note)}\n" +
        "└─────────────────────┘";

    static string TrimCard(string s)
    {
        s = s.Trim();
        return s.Length <= 36 ? s : s[..33] + "…";
    }
}
