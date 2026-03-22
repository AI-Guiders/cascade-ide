using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;

namespace CascadeIDE.Services;

/// <summary>
/// Расширенный слой для <see cref="UiThemeSnapshot"/>: разрешённые ресурсы под текущей темой,
/// рамка главного окна, именованные регионы лэйаута, все вкладки-редакторы в док-доке.
/// </summary>
internal static class UiThemeDeepSnapshot
{
    private static readonly JsonSerializerOptions NodeOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Ключи из <c>App.axaml</c>, которых нет в <see cref="UiThemeApply.Keys"/>.</summary>
    private static readonly string[] AdditionalCascadeThemeKeys =
    [
        "CascadeTheme.PowerSolutionTreePanelBackground",
        "CascadeTheme.PowerSolutionTreeForeground",
        "CascadeTheme.PowerSolutionTreeItemSelectedBackground",
        "CascadeTheme.PowerSolutionTreeItemPointerOverBackground",
        "CascadeTheme.PowerSolutionPanelBorderSubtle",
        "CascadeTheme.PowerSolutionTaskQueueBubbleBackground",
        "CascadeTheme.PowerEditorIslandFrameBrush",
        "CascadeTheme.PowerChatIslandFrameBrush",
        "CascadeTheme.PowerSolutionIslandFrameBrush"
    ];

    /// <summary>Имена из разметки — острова, доки, чат, нижняя панель, бейдж режима.</summary>
    private static readonly string[] LayoutRegionNames =
    [
        "RootWindow",
        "MainGrid",
        "DockIslandInner",
        "DocumentsDock",
        "SolutionIslandInner",
        "ChatIslandInner",
        "ChatPanelRoot",
        "BottomPanelShell",
        "ModeBadge",
        "UiModeBloomOverlay",
        "ChatInputBox",
        "TerminalInputBox"
    ];

    public static void MergeIntoJsonRoot(JsonObject root, Application app)
    {
        root["cascade_theme_resolved"] = JsonSerializer.SerializeToNode(BuildCascadeThemeResolved(app), NodeOptions);
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mw })
        {
            root["window_frame"] = JsonSerializer.SerializeToNode(BuildWindowFrame(app, mw), NodeOptions);
            root["layout_regions"] = JsonSerializer.SerializeToNode(BuildLayoutRegions(mw), NodeOptions);
            var vm = mw.DataContext as MainWindowViewModel;
            root["dock_text_editors"] = JsonSerializer.SerializeToNode(BuildAllDockTextEditors(mw, vm), NodeOptions);
        }
        else
        {
            root["window_frame"] = null;
            root["layout_regions"] = null;
            root["dock_text_editors"] = null;
        }
    }

    private static SortedDictionary<string, string?> BuildCascadeThemeResolved(Application app)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in typeof(UiThemeApply.Keys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (f.GetValue(null) is string s && s.StartsWith("CascadeTheme.", StringComparison.Ordinal))
                keys.Add(s);
        }
        foreach (var k in AdditionalCascadeThemeKeys)
            keys.Add(k);

        var variant = app.ActualThemeVariant;
        var map = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (!app.TryGetResource(key, variant, out var raw) || raw is null)
            {
                map[key] = null;
                continue;
            }

            map[key] = raw switch
            {
                IBrush b => UiThemeSnapshot.FormatBrushForJson(b),
                _ => raw.ToString()
            };
        }

        return map;
    }

    private static Dictionary<string, object?> BuildWindowFrame(Application app, Window w)
    {
        var bg = w.Background is IBrush wb ? UiThemeSnapshot.FormatBrushForJson(wb) : null;
        return new Dictionary<string, object?>
        {
            ["title"] = w.Title,
            ["bounds_w"] = Math.Round(w.Bounds.Width, 1),
            ["bounds_h"] = Math.Round(w.Bounds.Height, 1),
            ["client_width"] = Math.Round(w.ClientSize.Width, 1),
            ["client_height"] = Math.Round(w.ClientSize.Height, 1),
            ["extend_client_area_to_decorations"] = w.ExtendClientAreaToDecorationsHint,
            ["extend_client_area_chrome_hints"] = w.ExtendClientAreaChromeHints.ToString(),
            ["extend_client_area_title_bar_height_hint"] = w.ExtendClientAreaTitleBarHeightHint,
            ["transparency_level_hint"] = w.TransparencyLevelHint.ToString(),
            ["actual_theme_variant"] = app.ActualThemeVariant.Key.ToString(),
            ["background_brush"] = bg
        };
    }

    private static Dictionary<string, Dictionary<string, object?>?> BuildLayoutRegions(TopLevel topLevel)
    {
        var dict = new Dictionary<string, Dictionary<string, object?>?>(StringComparer.Ordinal);
        foreach (var name in LayoutRegionNames)
        {
            dict[name] = UiControlAppearance.TryBuildNamedRegionSnapshot(topLevel, name);
        }

        return dict;
    }

    private static List<Dictionary<string, object?>> BuildAllDockTextEditors(Visual root, MainWindowViewModel? vm)
    {
        var list = new List<Dictionary<string, object?>>();
        foreach (var v in EnumerateVisualDescendants(root))
        {
            if (v is not DockDocumentView dockView)
                continue;
            if (dockView.FindControl<TextEditor>("Editor") is not { } ed)
                continue;
            if (dockView.DataContext is not DockDocumentViewModel dvm)
                continue;

            var doc = dvm.Doc;
            var currentPath = vm?.CurrentFilePath;
            var isActivePath = !string.IsNullOrEmpty(currentPath)
                               && string.Equals(doc.FilePath, currentPath, StringComparison.OrdinalIgnoreCase);
            var modelLen = doc.Content?.Length ?? 0;
            var editorLen = ed.Document.TextLength;
            var (effBg, effFg) = UiControlAppearance.GetEffectiveColors(ed);

            double x = 0, y = 0, w = ed.Bounds.Width, h = ed.Bounds.Height;
            var topLeft = ed.TranslatePoint(new Point(0, 0), root);
            if (topLeft is { } p)
            {
                x = p.X;
                y = p.Y;
            }

            const int previewMax = 240;
            var text = ed.Document.Text ?? "";
            var preview = text.Length <= previewMax ? text : text[..previewMax] + "…";

            list.Add(new Dictionary<string, object?>
            {
                ["file_path"] = doc.FilePath,
                ["dock_title"] = dvm.Title,
                ["matches_main_window_current_file"] = isActivePath,
                ["editor_visible"] = ed.IsVisible,
                ["document_length"] = editorLen,
                ["model_content_length"] = modelLen,
                ["length_matches_model"] = editorLen == modelLen,
                ["line_count"] = ed.Document.LineCount,
                ["bounds"] = new Dictionary<string, double>
                {
                    ["x"] = Math.Round(x, 1),
                    ["y"] = Math.Round(y, 1),
                    ["w"] = Math.Round(w, 1),
                    ["h"] = Math.Round(h, 1)
                },
                ["name"] = (ed as StyledElement)?.Name ?? "",
                ["font_family"] = ed.FontFamily?.ToString(),
                ["font_size"] = ed.FontSize,
                ["foreground"] = UiThemeSnapshot.FormatBrushForJson(ed.Foreground),
                ["background"] = UiThemeSnapshot.FormatBrushForJson(ed.Background),
                ["effective_background"] = effBg,
                ["effective_foreground"] = effFg,
                ["text_preview"] = preview
            });
        }

        return list;
    }

    private static IEnumerable<Visual> EnumerateVisualDescendants(Visual node)
    {
        foreach (var child in node.GetVisualChildren())
        {
            yield return child;
            foreach (var d in EnumerateVisualDescendants(child))
                yield return d;
        }
    }

}
