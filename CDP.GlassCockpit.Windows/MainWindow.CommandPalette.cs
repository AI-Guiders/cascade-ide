#nullable enable

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass Ctrl+Q command palette overlay (local catalog).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassPaletteEntry> _paletteEntries = new();

    void InitCommandPalette()
    {
        PaletteList.ItemsSource = _paletteEntries;
        PaletteQuery.TextChanged += (_, _) => RefreshPaletteFilter();
        PaletteList.MouseDoubleClick += (_, _) => ExecutePaletteSelection();
        PaletteQuery.PreviewKeyDown += PaletteQuery_OnPreviewKeyDown;
        PaletteList.PreviewKeyDown += PaletteList_OnPreviewKeyDown;
    }

    void ToggleCommandPalette()
    {
        if (PaletteOverlay.Visibility == Visibility.Visible)
        {
            CloseCommandPalette();
            return;
        }

        PaletteQuery.Text = "";
        RefreshPaletteFilter();
        CloseCascadeChord();
        PaletteOverlay.Visibility = Visibility.Visible;
        PaletteQuery.Focus();
        Keyboard.Focus(PaletteQuery);
    }

    void CloseCommandPalette()
    {
        PaletteOverlay.Visibility = Visibility.Collapsed;
        _paletteEntries.Clear();
    }

    void RefreshPaletteFilter()
    {
        var hits = GlassCommandPaletteCatalog.Filter(PaletteQuery.Text);
        _paletteEntries.Clear();
        foreach (var h in hits)
            _paletteEntries.Add(h);
        PaletteList.SelectedIndex = _paletteEntries.Count > 0 ? 0 : -1;
    }

    void PaletteQuery_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseCommandPalette();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && _paletteEntries.Count > 0)
        {
            PaletteList.Focus();
            PaletteList.SelectedIndex = 0;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecutePaletteSelection();
            e.Handled = true;
        }
    }

    void PaletteList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseCommandPalette();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecutePaletteSelection();
            e.Handled = true;
        }
    }

    void ExecutePaletteSelection()
    {
        if (PaletteList.SelectedItem is not GlassPaletteEntry entry)
        {
            if (_paletteEntries.Count == 0)
                return;
            entry = _paletteEntries[0];
        }

        if (GlassCommandPaletteCatalog.IsNonExecutableMelodyRow(entry.Id))
            return;

        CloseCommandPalette();
        RunPaletteEntry(entry.Id);
    }

    void RunPaletteEntry(string id)
    {
        switch (id)
        {
            case "open_file":
                TryPickOpenFile();
                break;
            case "save_file":
                TrySaveEditor();
                break;
            case "focus_composer":
                ComposerBox.Focus();
                break;
            case "slash_help":
                TryRunGlassSlash("/help");
                break;
            case "slash_status":
                TryRunGlassSlash("/status");
                break;
            case "slash_topics":
                TryRunGlassSlash("/topics");
                break;
            case "slash_letter":
                TryRunGlassSlash("/letter");
                break;
            case "slash_fds":
                TryRunGlassSlash("/fds");
                break;
            case "slash_attach":
                TryRunGlassSlash("/attach");
                break;
            case "slash_open":
                TryRunGlassSlash("/open");
                break;
            case "slash_citizen":
                TryRunGlassSlash("/citizen");
                break;
            case "topics_all":
                TopicAllBtn_OnClick(TopicAllBtn, new RoutedEventArgs());
                break;
            case "mfd_editor":
                SelectMfdPage("Editor");
                break;
            case "mfd_terminal":
                SelectMfdPage("Terminal");
                break;
            case "mfd_fds":
                SelectMfdPage("FlightDataStorage");
                break;
            case "mfd_build":
                SelectMfdPage("Build");
                break;
            case "mfd_tests":
                SelectMfdPage("Tests");
                break;
            case "mfd_git":
                SelectMfdPage("Git");
                break;
            case "mfd_solution_explorer":
                SelectMfdPage("SolutionExplorer");
                break;
            case "mfd_hybrid_index":
                SelectMfdPage("HybridIndex");
                break;
            case "mfd_workspace_health":
                SelectMfdPage("WorkspaceHealth");
                break;
            case "mfd_env_ready":
                SelectMfdPage("EnvironmentReadiness");
                break;
            case "mfd_events":
                SelectMfdPage("Events");
                break;
            case "mfd_hypotheses":
                SelectMfdPage("Hypotheses");
                break;
            case "mfd_chat":
                SelectMfdPage("Chat");
                break;
            default:
                StatusText.Text = $"glass · palette · unknown {id}";
                break;
        }
    }
}
