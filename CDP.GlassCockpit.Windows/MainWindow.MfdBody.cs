#nullable enable

using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>MFD page select + latch glance / stub body (CabinGlass + SoftOrganMfdGlance).
/// Editor: AvalonEdit mounts on MfdEditorHost when Forward=intercom (ADR 0120); stub only if editor stays on Forward.</summary>
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

    string CurrentMfdPage() =>
        (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString() ?? "?";

    void RefreshMfdZoneTitle()
    {
        if (MfdZoneTitle is null)
            return;
        var page = CurrentMfdPage();
        MfdZoneTitle.Text = string.Equals(page, "?", StringComparison.Ordinal)
            ? "M · MFD"
            : $"M · {page}";
    }

    void UpdateMfdBody()
    {
        RefreshMfdZoneTitle();
        RefreshSolutionExplorerTree();
        RefreshMfdEditorVisibility();
        RefreshMfdTerminalVisibility();
        RefreshMfdBuildVisibility();
        RefreshMfdTestsVisibility();
        RefreshMfdGitVisibility();
        RefreshMfdProblemsVisibility();

        if (MfdBody is null)
            return;

        var page = CurrentMfdPage();
        if (string.Equals(page, "Editor", StringComparison.OrdinalIgnoreCase)
            && ReferenceEquals(EditorChrome.Parent, MfdEditorHost))
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (IsTerminalHostActive())
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (IsBuildHostActive())
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (IsTestsHostActive())
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (IsGitHostActive())
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (IsProblemsHostActive())
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

        if (string.Equals(page, "HybridIndex", StringComparison.OrdinalIgnoreCase)
            && GlassHybridIndexGlance.TryFormatFromWorkspaceRoot(_session.WorkspaceRoot) is { } hiGlance)
        {
            MfdBody.Text = hiGlance;
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "WorkspaceHealth", StringComparison.OrdinalIgnoreCase)
            && GlassWorkspaceHealthGlance.TryFormatFromWorkspaceRoot(_session.WorkspaceRoot) is { } whGlance)
        {
            MfdBody.Text = whGlance;
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "EnvironmentReadiness", StringComparison.OrdinalIgnoreCase))
        {
            MfdBody.Text = GlassEnvironmentReadinessGlance.TryFormatCurrentProcess();
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "Hypotheses", StringComparison.OrdinalIgnoreCase)
            && GlassHypothesesGlance.TryFormatFromWorkspaceRoot(_session.WorkspaceRoot) is { } hypGlance)
        {
            MfdBody.Text = hypGlance;
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "Events", StringComparison.OrdinalIgnoreCase))
        {
            MfdBody.Text = GlassEventsGlance.TryFormatCurrentHabitat();
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "FlightDataStorage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(page, "Fds", StringComparison.OrdinalIgnoreCase))
        {
            MfdBody.Text = GlassFdsGlance.Format(_session.WorkspaceRoot);
            RefreshEicasHealth();
            return;
        }

        MfdBody.Text = page switch
        {
            "Terminal" => FormatMfdStub("Terminal", "Glass redirected TextBox", "Avalonia ConPTY SSOT · go=sys"),
            "Build" => FormatMfdStub("Build", "Glass redirected log TextBox", "Avalonia BuildMfdPageView SSOT · go=toolchain"),
            "SolutionExplorer" => FormatMfdStub(
                "SolutionExplorer",
                "SolutionExplorerView",
                "no .sln · " + (_session.WorkspaceRoot ?? "?")),
            "SemanticMap" => FormatMfdStub("SemanticMap", "WorkspaceNavigationMapView · Skia", "arch latch when live"),
            "Problems" => FormatMfdStub("Problems", "Glass ListBox host", "refresh → dotnet build parse · dbl-click open"),
            "Tests" => FormatMfdStub("Tests", "Glass redirected log TextBox", "Avalonia TestsMfdPageView SSOT · go=test_desk"),
            "DebugStack" => FormatMfdStub("DebugStack", "DebugStackMfdPageView · IdeDapDebugSession", "debug_desk latch · DAP Avalonia SSOT"),
            "Git" => FormatMfdStub("Git", "Glass redirected status TextBox", "Avalonia GitMfdPageView SSOT"),
            "RelatedFiles" => FormatMfdStub("RelatedFiles", "RelatedFilesMfdPageView", "refactor latch when live"),
            "Correspondence" => FormatMfdStub("Correspondence", "CorrespondenceMfdPageView", "CRS · no SoftOrgan"),
            "MarkdownPreview" => FormatMfdStub("MarkdownPreview", "MarkdigMarkdownPreviewRenderer", "report latch when live"),
            "WebAiPortal" => FormatMfdStub("WebAiPortal", "WebAiPortalMfdPageView", "WebView2 peel deferred"),
            "AiChatSettings" => FormatMfdStub("AiChatSettings", "options/ignite/mcp SoftOrgan", "settings.toml SSOT"),
            "WorkspaceHealth" => FormatMfdStub("WorkspaceHealth", "Glass FS status glance", "Avalonia IdeHealth SSOT"),
            "EnvironmentReadiness" => FormatMfdStub("EnvironmentReadiness", "Glass env probe glance", "Avalonia EnvReady SSOT"),
            "Events" => FormatMfdStub("Events", "Glass latch/catalog glance", "Avalonia EventsMFD SSOT"),
            "Hypotheses" => FormatMfdStub("Hypotheses", "Glass JSON status glance", "Avalonia Hypotheses SSOT"),
            "Editor" => FormatMfdStub("Editor", "on Forward", "primary_work_surface=editor"),
            "Chat" => GlassIntercomPresence.FormatChatMfdGlance(),
            "FlightDataStorage" or "Fds" => GlassFdsGlance.Format(_session.WorkspaceRoot),
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
