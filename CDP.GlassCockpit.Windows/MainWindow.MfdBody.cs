#nullable enable

using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

/// <summary>MFD page select + stub body text (CabinGlass catalog peels later).</summary>
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

        MfdBody.Text = page switch
        {
            "Terminal" => "Terminal page host.\n\nConPTY / shell organ wires in later peels.\nNow: page chrome only (like CIDE MfdShell).",
            "SolutionExplorer" => "Solution Explorer host.\n\nTree of CascadeIDE.sln / open workspace — later.",
            "SemanticMap" => "Semantic Map host.\n\nGraph surface later (not adjacency dump).",
            "Tests" => "Tests page host.\n\ncdp_test / test_desk projection (CabinGlass catalog).",
            "HybridIndex" => "Hybrid Index host.\n\ncodebase_index organ → glass MFD (stub peel).",
            "RelatedFiles" => "Related Files host.\n\nfind_desk / related / refactor organ projection.",
            "Correspondence" => "Correspondence host.\n\ncrs organ projection — later inbox chrome.",
            "MarkdownPreview" => "Markdown Preview host.\n\nmd_preview / md_author projection.",
            "WebAiPortal" => "Web / AI Portal host.\n\nbrowser organ projection.",
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
