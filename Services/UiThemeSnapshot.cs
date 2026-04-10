using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace CascadeIDE.Services;

/// <summary>
/// Снимок цветов UI для агента (<c>ide_get_ui_theme</c>): читает <see cref="Application.Current"/>.<see cref="StyledElement.Resources"/>
/// (те же ключи <c>CascadeTheme.*</c>, что и <see cref="UiThemeApply"/> / <c>App.axaml</c>).
/// </summary>
public static class UiThemeSnapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>JSON для MCP: актуальные кисти из Application.Resources (с UI-потока).</summary>
    public static string GetJson()
    {
        if (Application.Current is null)
            return SerializeStaticFallback();

        if (UiScheduler.Default.CheckAccess())
            return CaptureAndSerialize(Application.Current);

        return UiScheduler.Default.Invoke(() =>
        {
            if (Application.Current is null)
                return SerializeStaticFallback();
            return CaptureAndSerialize(Application.Current);
        });
    }

    private static string CaptureAndSerialize(Application app)
    {
        var res = app.Resources;
        var variantName = app.ActualThemeVariant.Key.ToString();
        var requestedName = app.RequestedThemeVariant?.Key.ToString() ?? "Default";

        static string? H(IResourceDictionary d, string k) => TryBrushString(d, k);

        var mainBg = H(res, UiThemeApply.Keys.MainWindowBackground);
        var menuBg = H(res, UiThemeApply.Keys.MenuBackground);
        var menuFg = H(res, UiThemeApply.Keys.MenuForeground);

        var theme = new
        {
            _snapshot = new
            {
                source = "application.resources",
                actual_theme_variant = variantName,
                requested_theme_variant = requestedName
            },
            main_window = new { background = mainBg },
            menu = new { background = menuBg, foreground = menuFg },
            menu_item = new { foreground = menuFg },
            button = new
            {
                background = H(res, UiThemeApply.Keys.ButtonBackground),
                foreground = H(res, UiThemeApply.Keys.ButtonForeground),
                border_brush = H(res, UiThemeApply.Keys.ButtonBorderBrush),
                border_thickness = 1,
                hover_background = H(res, UiThemeApply.Keys.ButtonHoverBackground),
                disabled_background = H(res, UiThemeApply.Keys.ButtonDisabledBackground),
                disabled_foreground = H(res, UiThemeApply.Keys.ButtonDisabledForeground)
            },
            toolbar = new { background = H(res, UiThemeApply.Keys.ToolbarBackground) },
            toolbar_text = new
            {
                foreground = H(res, UiThemeApply.Keys.ToolbarTextForeground),
                error_foreground = H(res, UiThemeApply.Keys.ToolbarErrorForeground)
            },
            editor = new
            {
                background = H(res, UiThemeApply.Keys.EditorBackground),
                foreground = H(res, UiThemeApply.Keys.EditorForeground),
                font_family = "Consolas, Cascadia Code, monospace",
                font_size = 13
            },
            editor_column = new
            {
                border_brush = H(res, UiThemeApply.Keys.EditorColumnBorderBrush),
                background = H(res, UiThemeApply.Keys.EditorColumnBackground),
                current_file_foreground = H(res, UiThemeApply.Keys.CurrentFileForeground)
            },
            workspace_layout = new
            {
                border_brush = H(res, UiThemeApply.Keys.WorkspacePanelBorderBrush)
            },
            markdown_preview_panel = new
            {
                background = H(res, UiThemeApply.Keys.MarkdownPreviewPanelBackground),
                border_brush = H(res, UiThemeApply.Keys.MarkdownPreviewPanelBorderBrush)
            },
            solution_explorer = new
            {
                border_brush = H(res, UiThemeApply.Keys.SolutionExplorerBorderBrush),
                header_foreground = H(res, UiThemeApply.Keys.SolutionExplorerHeaderForeground),
                tree_item_foreground = H(res, UiThemeApply.Keys.SolutionExplorerHeaderForeground)
            },
            panel_chrome = new
            {
                title_foreground = H(res, UiThemeApply.Keys.PanelChromeTitleForeground),
                accent_brush = H(res, UiThemeApply.Keys.PanelTitleAccentBrush),
                header_background = H(res, UiThemeApply.Keys.PanelChromeHeaderBackground),
                header_separator = H(res, UiThemeApply.Keys.PanelChromeHeaderSeparatorBrush),
                menu_glyph_foreground = H(res, UiThemeApply.Keys.PanelChromeMenuGlyphForeground)
            },
            build_output = new
            {
                background = H(res, UiThemeApply.Keys.BuildOutputBackground),
                foreground = H(res, UiThemeApply.Keys.TerminalForeground),
                border_brush = H(res, UiThemeApply.Keys.BuildOutputBorderBrush)
            },
            chat_panel = new
            {
                background = H(res, UiThemeApply.Keys.ChatPanelBackground),
                label_foreground = H(res, UiThemeApply.Keys.ChatLabelForeground),
                secondary_foreground = H(res, UiThemeApply.Keys.ChatLabelForeground),
                message_bubble_background = H(res, UiThemeApply.Keys.ChatMessageBubbleBackground),
                message_role_foreground = H(res, UiThemeApply.Keys.ChatLabelForeground),
                message_content_foreground = H(res, UiThemeApply.Keys.ChatMessageContentForeground),
                send_button_background = H(res, UiThemeApply.Keys.SendButtonBackground),
                send_button_foreground = H(res, UiThemeApply.Keys.SendButtonForeground),
                checkbox_foreground = H(res, UiThemeApply.Keys.ChatLabelForeground)
            },
            terminal = new
            {
                background = H(res, UiThemeApply.Keys.TerminalBackground),
                foreground = H(res, UiThemeApply.Keys.TerminalForeground),
                input_background = H(res, UiThemeApply.Keys.TerminalInputBackground),
                font_family = "Consolas, monospace",
                font_size = 12
            },
            mcp_banner = new
            {
                background = H(res, UiThemeApply.Keys.McpBannerBackground),
                foreground = H(res, UiThemeApply.Keys.McpBannerForeground)
            },
            preview_window = new
            {
                background = H(res, UiThemeApply.Keys.PreviewWindowBackground),
                content_area_background = H(res, UiThemeApply.Keys.PreviewWindowBackground)
            },
            power_cockpit = new
            {
                neon_border = H(res, UiThemeApply.Keys.PowerNeonBorder),
                neon_accent = H(res, UiThemeApply.Keys.PowerNeonAccent),
                panel_background = H(res, UiThemeApply.Keys.PowerCockpitPanelBackground),
                safety_dock_background = H(res, UiThemeApply.Keys.PowerSafetyDockBackground),
                telemetry_strip_background = H(res, UiThemeApply.Keys.PowerWorkspaceHealthStripBackground),
                safety_l1 = H(res, UiThemeApply.Keys.PowerSafetyL1),
                safety_l2 = H(res, UiThemeApply.Keys.PowerSafetyL2),
                safety_l3 = H(res, UiThemeApply.Keys.PowerSafetyL3),
                emergency = H(res, UiThemeApply.Keys.PowerEmergency)
            },
            solution_explorer_tree_power = new
            {
                panel_background = H(res, "CascadeTheme.PowerSolutionTreePanelBackground"),
                tree_foreground = H(res, "CascadeTheme.PowerSolutionTreeForeground"),
                item_selected_background = H(res, "CascadeTheme.PowerSolutionTreeItemSelectedBackground"),
                item_pointer_over_background = H(res, "CascadeTheme.PowerSolutionTreeItemPointerOverBackground"),
                panel_border_subtle = H(res, "CascadeTheme.PowerSolutionPanelBorderSubtle"),
                task_queue_bubble_background = H(res, "CascadeTheme.PowerSolutionTaskQueueBubbleBackground")
            },
            power_island_frame_brushes = new
            {
                editor = H(res, "CascadeTheme.PowerEditorIslandFrameBrush"),
                chat = H(res, "CascadeTheme.PowerChatIslandFrameBrush"),
                solution = H(res, "CascadeTheme.PowerSolutionIslandFrameBrush")
            }
        };

        var node = JsonSerializer.SerializeToNode(theme, Options) as JsonObject;
        if (node is not null)
            UiThemeDeepSnapshot.MergeIntoJsonRoot(node, app);
        return node?.ToJsonString(Options) ?? JsonSerializer.Serialize(theme, Options);
    }

    /// <summary>Строка для JSON: solid как <c>#RGB</c>/<c>#AARRGGBB</c>, <see cref="LinearGradientBrush"/> как <c>linear(...)</c>.</summary>
    public static string? FormatBrushForJson(IBrush? brush) =>
        brush is null ? null : FormatBrushForJson((object)brush);

    /// <summary>Как <see cref="FormatBrushForJson(IBrush?)"/>, для произвольного объекта из ресурсов.</summary>
    public static string? FormatBrushForJson(object? raw) => FormatBrushObject(raw);

    /// <summary>Статический шаблон, если приложение ещё не поднято (тесты, сравнение формата).</summary>
    private static string SerializeStaticFallback()
    {
        var theme = new
        {
            _snapshot = new { source = "static_fallback", detail = "Application.Current is null" },
            main_window = new { background = "#F5F5F5" },
            menu = new { background = "#F0F0F0", foreground = "#1A1A1A" },
            menu_item = new { foreground = "#1A1A1A" },
            button = new
            {
                background = "#E0E0E0",
                foreground = "#1A1A1A",
                border_brush = "#BBB",
                border_thickness = 1,
                hover_background = "#D0D0D0",
                disabled_background = "#EEE",
                disabled_foreground = "#888"
            },
            toolbar = new { background = "#E8E8E8" },
            toolbar_text = new { foreground = "#333", error_foreground = "#C00" },
            editor = new
            {
                background = "#FFF",
                foreground = "#1A1A1A",
                font_family = "Consolas, Cascadia Code, monospace",
                font_size = 13
            },
            editor_column = new { border_brush = "#CCC", background = "#FFF", current_file_foreground = "#666" },
            workspace_layout = new { border_brush = "#CCC" },
            markdown_preview_panel = new { background = "#FAFAFA", border_brush = "#E0E0E0" },
            solution_explorer = new { border_brush = "#CCC", header_foreground = "#1A1A1A", tree_item_foreground = "#1A1A1A" },
            panel_chrome = new
            {
                title_foreground = "#1A1A1A",
                accent_brush = "#5B4FC9",
                header_background = "#E8E8EC",
                header_separator = "#C8C8D0",
                menu_glyph_foreground = "#666666"
            },
            build_output = new { background = "#F8F8F8", foreground = "#CCC", border_brush = "#CCC" },
            chat_panel = new
            {
                background = "#FFF",
                label_foreground = "#333",
                secondary_foreground = "#666",
                message_bubble_background = "#F0F0F0",
                message_role_foreground = "#555",
                message_content_foreground = "#1A1A1A",
                send_button_background = "#4CAF50",
                send_button_foreground = "White",
                checkbox_foreground = "#333"
            },
            terminal = new
            {
                background = "#1E1E1E",
                foreground = "#CCC",
                input_background = "#2D2D2D",
                font_family = "Consolas, monospace",
                font_size = 12
            },
            mcp_banner = new { background = "#E3F2FD", foreground = "#1565C0" },
            preview_window = new { background = "#F0F0F0", content_area_background = "#F0F0F0" },
            power_cockpit = new
            {
                neon_border = "#00F0FF",
                neon_accent = "#E090FF",
                panel_background = "#070E18",
                safety_dock_background = "#020814",
                telemetry_strip_background = "#040A14",
                safety_l1 = "#1A5088",
                safety_l2 = "#9A7818",
                safety_l3 = "#6230B0",
                emergency = "#FF3355"
            },
            solution_explorer_tree_power = (object?)null,
            power_island_frame_brushes = (object?)null
        };
        var fallbackNode = JsonSerializer.SerializeToNode(theme, Options) as JsonObject;
        if (fallbackNode is not null)
        {
            fallbackNode["cascade_theme_resolved"] = null;
            fallbackNode["window_frame"] = null;
            fallbackNode["layout_regions"] = null;
            fallbackNode["dock_text_editors"] = null;
            fallbackNode["dock_open_documents"] = null;
            fallbackNode["top_levels"] = null;
        }

        return fallbackNode?.ToJsonString(Options) ?? JsonSerializer.Serialize(theme, Options);
    }

    private static string? TryBrushString(IResourceDictionary res, string key)
    {
        if (!res.TryGetValue(key, out var raw) || raw is null)
            return null;
        return FormatBrushObject(raw);
    }

    private static string? FormatBrushObject(object? raw)
    {
        switch (raw)
        {
            case null:
                return null;
            case LinearGradientBrush lg:
                return SerializeLinearGradient(lg);
            case ISolidColorBrush sc:
                return ColorToHex(sc.Color);
            default:
                return null;
        }
    }

    private static string ColorToHex(Color c)
    {
        if (c.A == byte.MaxValue)
            return FormattableString.Invariant($"#{c.R:X2}{c.G:X2}{c.B:X2}");
        return FormattableString.Invariant($"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}");
    }

    private static string SerializeLinearGradient(LinearGradientBrush lg)
    {
        var sp = lg.StartPoint.Point;
        var ep = lg.EndPoint.Point;
        var stops = new List<string>();
        foreach (var gs in lg.GradientStops)
            stops.Add(string.Create(CultureInfo.InvariantCulture, $"{gs.Offset:R}:{ColorToHex(gs.Color)}"));
        return string.Create(CultureInfo.InvariantCulture,
            $"linear({sp.X:R},{sp.Y:R}->{ep.X:R},{ep.Y:R};{string.Join(",", stops)})");
    }

    /// <summary>Цвета панели «Вывод сборки» (фон журнала и текст — как в <c>BottomPanelView</c>: <c>TerminalForeground</c>).</summary>
    public static (string background, string foreground) GetBuildOutputTheme()
    {
        const string defBg = "#F8F8F8";
        const string defFg = "#CCC";

        if (Application.Current is null)
            return (defBg, defFg);

        if (UiScheduler.Default.CheckAccess())
            return ReadBuildOutput(Application.Current.Resources);

        return UiScheduler.Default.Invoke(() =>
            Application.Current is { } app
                ? ReadBuildOutput(app.Resources)
                : (defBg, defFg));
    }

    private static (string background, string foreground) ReadBuildOutput(IResourceDictionary res)
    {
        const string defBg = "#F8F8F8";
        const string defFg = "#CCC";
        var bg = TrySolidHexOnly(res, UiThemeApply.Keys.BuildOutputBackground) ?? defBg;
        var fg = TrySolidHexOnly(res, UiThemeApply.Keys.TerminalForeground) ?? defFg;
        return (bg, fg);
    }

    private static string? TrySolidHexOnly(IResourceDictionary res, string key)
    {
        if (!res.TryGetValue(key, out var raw) || raw is null)
            return null;
        return raw switch
        {
            ISolidColorBrush s => ColorToHex(s.Color),
            _ => null
        };
    }
}
