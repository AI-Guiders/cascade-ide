#nullable enable

using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>MFD page select + latch glance / stub body (CabinGlass + SoftOrganMfdGlance).
/// Editor Face: AvalonEdit always on MfdEditorHost when page=Editor (GlassEditorFace) — never FormatMfdStub peel.</summary>
public partial class MainWindow
{
    /// <summary>Last presentation/chord/SoftOrgan-M instrument page — seats P/F republish must not yank it.</summary>
    string? _stickyMfdPage;

    void SelectMfdPage(string? page, bool sticky = false)
    {
        if (string.IsNullOrWhiteSpace(page) || MfdPages is null)
            return;

        var trimmed = page.Trim();
        if (sticky)
            _stickyMfdPage = trimmed;

        // Channel stack first (sit/world/alert…); legacy P↔M zone when no named surface hits.
        if (CascadeIDE.GlassCore.Presentation.PresentationPmOneOfPolicy.ResolveStackSurface(
                _hosts.PmOneOfStack, trimmed) is { } surface)
            _hosts.PreferSurface(surface);
        else if (CascadeIDE.GlassCore.Presentation.PresentationPmOneOfPolicy.FromMfdPage(trimmed) is { } oneOf)
            _hosts.PreferPmOneOf(oneOf);

        if (string.Equals(CurrentMfdPage(), trimmed, StringComparison.OrdinalIgnoreCase))
        {
            // Re-select Editor while AvalonEdit still on Forward → force Face mount (no SelectionChanged).
            if (GlassEditorFace.PreferEditorHost(trimmed))
                RefreshMfdEditorVisibility();
            return;
        }

        foreach (var item in MfdPages.Items)
        {
            if (item is ListBoxItem lbi &&
                string.Equals(lbi.Content?.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                MfdPages.SelectedItem = lbi;
                return;
            }
        }

        // 0-sync: CabinGlass may name a page before XAML list catches up — ensure selectable.
        var created = new ListBoxItem { Content = trimmed };
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
        RefreshMfdTerminalVisibility();
        RefreshMfdBuildVisibility();
        RefreshMfdTestsVisibility();
        RefreshMfdGitVisibility();
        RefreshMfdProblemsVisibility();
        RefreshMfdRelatedVisibility();
        RefreshMfdHybridIndexVisibility();
        RefreshMfdGlanceCardsVisibility();
        RefreshMfdEditorVisibility();
        RefreshMfdSemanticVisibility();
        RefreshMfdCorrespondenceVisibility();
        RefreshMfdMarkdownVisibility();
        RefreshMfdAiChatSettingsVisibility();
        RefreshMfdDebugVisibility();
        RefreshMfdWebAiVisibility();
        RefreshFilesDeskVisibility();
        RefreshFindDeskVisibility();

        if (MfdBody is null)
            return;

        var page = CurrentMfdPage();
        if (GlassEditorFace.PreferEditorHost(page))
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

        if (IsRelatedHostActive()
            || IsHybridIndexHostActive()
            || IsSemanticHostActive()
            || IsCorrespondenceHostActive()
            || IsMarkdownHostActive()
            || IsGlanceCardsHostActive()
            || IsAiChatSettingsHostActive()
            || IsDebugHostActive()
            || IsWebAiHostActive())
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (GlassSolutionExplorerFace.PreferTreeHost(page))
        {
            // Face = TreeView ItemsSource (RefreshSolutionExplorerTree) — never FormatMfdStub Avalonia peel.
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "FilesDesk", StringComparison.OrdinalIgnoreCase))
        {
            // Face = FilesDesk list from files_desk latch (SE≠FM).
            if (_filesDeskRows.Count > 0)
                MfdBody.Text = "";
            else if (SoftOrganMfdGlance.TryFormatFromOrganId("files_desk") is { } fmGlance)
                MfdBody.Text = fmGlance;
            else
                MfdBody.Text = "files · idle — SoftKeys Up/Open/List · DoubleClick/Enter";
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "FindDesk", StringComparison.OrdinalIgnoreCase))
        {
            if (_findDeskRows.Count > 0)
                MfdBody.Text = "";
            else if (SoftOrganMfdGlance.TryFormatFromOrganId("find_desk") is { } findGlance)
                MfdBody.Text = findGlance;
            else
                MfdBody.Text = "find · idle — /search pattern · DoubleClick hit opens";
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

        if (string.Equals(page, "HybridIndex", StringComparison.OrdinalIgnoreCase)
            && IsHybridIndexHostActive())
        {
            MfdBody.Text = "";
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "HybridIndex", StringComparison.OrdinalIgnoreCase))
        {
            // Human face = instrument cards + scope Skia map (MfdHybridIndexHost).
            MfdBody.Text = FormatMfdStub("HybridIndex", "HCI cards · DOCS/FRESH + scope map", "Shared-SSOT index instrument");
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
            // Human face = glance card deck (MfdGlanceCardsHost); body stub only if host hidden.
            MfdBody.Text = FormatMfdStub("FlightDataStorage", "FDS card deck · PLAN/SHARE/PRESSURE/WAKE", "Shared-SSOT shelf instrument");
            RefreshEicasHealth();
            return;
        }

        if (string.Equals(page, "DomainBoard", StringComparison.OrdinalIgnoreCase)
            || string.Equals(page, "Domain", StringComparison.OrdinalIgnoreCase))
        {
            MfdBody.Text = FormatMfdStub("DomainBoard", "domain card deck · .cdp/domain", "SoftOrgan ownership instrument");
            RefreshEicasHealth();
            return;
        }

        if (SoftOrganFaceHandbook.IsSoftOrganGlancePage(page))
        {
            MfdBody.Text = FormatMfdStub(page, "HERE · situations → steps", "ADR 0014 SoftFL · не markdown wall");
            RefreshEicasHealth();
            return;
        }

        MfdBody.Text = page switch
        {
            "Terminal" => FormatMfdStub("Terminal", "Glass redirected TextBox", "ConPTY later · go=sys"),
            "Build" => FormatMfdStub("Build", "Glass log + MSBuild ListBox", "parity Avalonia log MFD"),
            "SemanticMap" => FormatMfdStub("SemanticMap", "Glass Skia + arch board", "ADR 0196 roles"),
            "Problems" => FormatMfdStub("Problems", "severity board · ERR/WARN/ALL + jump list", "Shared-SSOT quality"),
            "Tests" => FormatMfdStub("Tests", "Glass log + fail ListBox", "Avalonia TestsMfdPageView SSOT"),
            "DebugStack" => FormatMfdStub("DebugStack", "Glass live DAP latch host", "debug_desk SoftOrgan · stack/locals"),
            "Git" => FormatMfdStub("Git", "Glass porcelain+diff host", "stage/commit/push/submodule"),
            "RelatedFiles" => FormatMfdStub("RelatedFiles", "companions · Skia graph + list", "Shared-SSOT blast instrument"),
            "Correspondence" => FormatMfdStub("Correspondence", "Glass CRS thread timeline", "cards + rail"),
            "MarkdownPreview" => FormatMfdStub("MarkdownPreview", "Glass Markdig FlowDocument", "headings/links/code"),
            "WebAiPortal" => FormatMfdStub("WebAiPortal", "Glass WebView2", "embedded browser"),
            "AiChatSettings" => FormatMfdStub("AiChatSettings", "Glass settings.toml host", "provider/model/MCP"),
            "WorkspaceHealth" => FormatMfdStub("WorkspaceHealth", "Glass FS status glance", "Avalonia IdeHealth SSOT"),
            "EnvironmentReadiness" => FormatMfdStub("EnvironmentReadiness", "Glass env probe glance", "Avalonia EnvReady SSOT"),
            "Events" => FormatMfdStub("Events", "Glass latch/catalog glance", "Avalonia EventsMFD SSOT"),
            "Hypotheses" => FormatMfdStub("Hypotheses", "Glass JSON status glance", "Avalonia Hypotheses SSOT"),
            // Editor Face: PreferEditorHost early-return above — never Avalonia peel.
            "Chat" => GlassIntercomPresence.FormatChatMfdGlance(),
            "FlightDataStorage" or "Fds" => FormatMfdStub("FlightDataStorage", "FDS card deck · PLAN/SHARE/PRESSURE/WAKE", "Shared-SSOT shelf instrument"),
            "DomainBoard" or "Domain" => FormatMfdStub("DomainBoard", "domain card deck · .cdp/domain", "SoftOrgan ownership instrument"),
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
