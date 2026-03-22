using System.Text.Json;
using Avalonia.Media;
using Avalonia.Threading;

namespace CascadeIDE.Services;

/// <summary>
/// Применение темы UI из JSON (тот же формат, что ide_get_ui_theme).
/// Обновляет Application.Resources; элементы с DynamicResource перерисуются.
/// </summary>
public static class UiThemeApply
{
    private static (string path, DateTime lastWrite, string json)? _themeCache;

    /// <summary>Загружает тему из файла. Перечитывает только если изменились путь или дата модификации файла.</summary>
    public static string GetThemeJsonFromFile(string filePath)
    {
        var path = Path.GetFullPath(filePath);
        if (!File.Exists(path))
            return "{}";
        var lastWrite = File.GetLastWriteTimeUtc(path);
        if (_themeCache is { } c && c.path == path && c.lastWrite == lastWrite)
            return c.json;
        var json = File.ReadAllText(path);
        _themeCache = (path, lastWrite, json);
        return json;
    }

    /// <summary>Тёмная тема: Themes/dark-theme.json рядом с exe.</summary>
    public static string GetDarkThemeJson() =>
        GetThemeJsonFromFile(Path.Combine(AppContext.BaseDirectory, "Themes", "dark-theme.json"));

    /// <summary>Светлая тема: Themes/light-theme.json рядом с exe.</summary>
    public static string GetLightThemeJson() =>
        GetThemeJsonFromFile(Path.Combine(AppContext.BaseDirectory, "Themes", "light-theme.json"));

    /// <summary>Тема «как Cursor»: тёмная с акцентом #3794FF. Themes/cursor-like-theme.json.</summary>
    public static string GetCursorLikeThemeJson() =>
        GetThemeJsonFromFile(Path.Combine(AppContext.BaseDirectory, "Themes", "cursor-like-theme.json"));

    /// <summary>Тема Power Mode: неоново-тёмная «космическая» палитра. Themes/power-theme.json.</summary>
    public static string GetPowerThemeJson() =>
        GetThemeJsonFromFile(Path.Combine(AppContext.BaseDirectory, "Themes", "power-theme.json"));

    /// <summary>Ключи ресурсов в Application.Resources. Должны совпадать с App.axaml и DynamicResource в XAML.</summary>
    public static class Keys
    {
        public const string MainWindowBackground = "CascadeTheme.MainWindowBackground";
        public const string MenuBackground = "CascadeTheme.MenuBackground";
        public const string MenuForeground = "CascadeTheme.MenuForeground";
        public const string ButtonBackground = "CascadeTheme.ButtonBackground";
        public const string ButtonForeground = "CascadeTheme.ButtonForeground";
        public const string ButtonBorderBrush = "CascadeTheme.ButtonBorderBrush";
        public const string ButtonHoverBackground = "CascadeTheme.ButtonHoverBackground";
        public const string ButtonDisabledBackground = "CascadeTheme.ButtonDisabledBackground";
        public const string ButtonDisabledForeground = "CascadeTheme.ButtonDisabledForeground";
        public const string ToolbarBackground = "CascadeTheme.ToolbarBackground";
        public const string ToolbarTextForeground = "CascadeTheme.ToolbarTextForeground";
        public const string ToolbarErrorForeground = "CascadeTheme.ToolbarErrorForeground";
        public const string EditorBackground = "CascadeTheme.EditorBackground";
        public const string EditorForeground = "CascadeTheme.EditorForeground";
        public const string EditorColumnBorderBrush = "CascadeTheme.EditorColumnBorderBrush";
        public const string WorkspacePanelBorderBrush = "CascadeTheme.WorkspacePanelBorderBrush";
        public const string EditorColumnBackground = "CascadeTheme.EditorColumnBackground";
        public const string CurrentFileForeground = "CascadeTheme.CurrentFileForeground";
        public const string MarkdownPreviewPanelBackground = "CascadeTheme.MarkdownPreviewPanelBackground";
        public const string MarkdownPreviewPanelBorderBrush = "CascadeTheme.MarkdownPreviewPanelBorderBrush";
        public const string SolutionExplorerBorderBrush = "CascadeTheme.SolutionExplorerBorderBrush";
        public const string SolutionExplorerHeaderForeground = "CascadeTheme.SolutionExplorerHeaderForeground";
        public const string PanelChromeTitleForeground = "CascadeTheme.PanelChromeTitleForeground";
        public const string PanelTitleAccentBrush = "CascadeTheme.PanelTitleAccentBrush";
        public const string PanelChromeHeaderBackground = "CascadeTheme.PanelChromeHeaderBackground";
        public const string PanelChromeHeaderSeparatorBrush = "CascadeTheme.PanelChromeHeaderSeparatorBrush";
        public const string PanelChromeMenuGlyphForeground = "CascadeTheme.PanelChromeMenuGlyphForeground";
        public const string BuildOutputBackground = "CascadeTheme.BuildOutputBackground";
        public const string BuildOutputBorderBrush = "CascadeTheme.BuildOutputBorderBrush";
        public const string ChatPanelBackground = "CascadeTheme.ChatPanelBackground";
        public const string ChatLabelForeground = "CascadeTheme.ChatLabelForeground";
        public const string ChatMessageBubbleBackground = "CascadeTheme.ChatMessageBubbleBackground";
        public const string ChatMessageContentForeground = "CascadeTheme.ChatMessageContentForeground";
        public const string SendButtonBackground = "CascadeTheme.SendButtonBackground";
        public const string SendButtonForeground = "CascadeTheme.SendButtonForeground";
        public const string TerminalBackground = "CascadeTheme.TerminalBackground";
        public const string TerminalForeground = "CascadeTheme.TerminalForeground";
        public const string TerminalInputBackground = "CascadeTheme.TerminalInputBackground";
        public const string McpBannerBackground = "CascadeTheme.McpBannerBackground";
        public const string McpBannerForeground = "CascadeTheme.McpBannerForeground";
        public const string PreviewWindowBackground = "CascadeTheme.PreviewWindowBackground";
        /// <summary>Опционально: секция power_cockpit в JSON темы (неон, safety, телеметрия).</summary>
        public const string PowerNeonBorder = "CascadeTheme.PowerNeonBorder";
        public const string PowerNeonAccent = "CascadeTheme.PowerNeonAccent";
        public const string PowerCockpitPanelBackground = "CascadeTheme.PowerCockpitPanelBackground";
        public const string PowerSafetyDockBackground = "CascadeTheme.PowerSafetyDockBackground";
        public const string PowerTelemetryStripBackground = "CascadeTheme.PowerTelemetryStripBackground";
        public const string PowerSafetyL1 = "CascadeTheme.PowerSafetyL1";
        public const string PowerSafetyL2 = "CascadeTheme.PowerSafetyL2";
        public const string PowerSafetyL3 = "CascadeTheme.PowerSafetyL3";
        public const string PowerEmergency = "CascadeTheme.PowerEmergency";
    }

    /// <summary>Применить тему из JSON (формат ide_get_ui_theme). Вызывать из UI-потока. Возвращает "OK" или сообщение об ошибке (для ответа тулу ide_set_ui_theme).</summary>
    public static string Apply(string themeJson)
    {
        if (string.IsNullOrWhiteSpace(themeJson))
            return "OK";
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(themeJson);
        }
        catch (JsonException ex)
        {
            var msg = $"Invalid JSON: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ide_set_ui_theme: {msg}");
            return msg;
        }
        using (doc)
        {
            var root = doc.RootElement;
            var res = GetResourceDictionary();
            if (res is null)
                return "Application resources not available.";

            Set(res, Keys.MainWindowBackground, GetColor(root, "main_window", "background"));
            Set(res, Keys.MenuBackground, GetColor(root, "menu", "background"));
            Set(res, Keys.MenuForeground, GetColor(root, "menu", "foreground"));
            Set(res, Keys.ButtonBackground, GetColor(root, "button", "background"));
            Set(res, Keys.ButtonForeground, GetColor(root, "button", "foreground"));
            Set(res, Keys.ButtonBorderBrush, GetColor(root, "button", "border_brush"));
            Set(res, Keys.ButtonHoverBackground, GetColor(root, "button", "hover_background"));
            Set(res, Keys.ButtonDisabledBackground, GetColor(root, "button", "disabled_background"));
            Set(res, Keys.ButtonDisabledForeground, GetColor(root, "button", "disabled_foreground"));
            Set(res, Keys.ToolbarBackground, GetColor(root, "toolbar", "background"));
            Set(res, Keys.ToolbarTextForeground, GetColor(root, "toolbar_text", "foreground"));
            Set(res, Keys.ToolbarErrorForeground, GetColor(root, "toolbar_text", "error_foreground"));
            Set(res, Keys.EditorBackground, GetColor(root, "editor", "background"));
            Set(res, Keys.EditorForeground, GetColor(root, "editor", "foreground"));
            Set(res, Keys.EditorColumnBorderBrush, GetColor(root, "editor_column", "border_brush"));
            Set(res, Keys.WorkspacePanelBorderBrush, GetWorkspacePanelBorderBrush(root));
            Set(res, Keys.EditorColumnBackground, GetColor(root, "editor_column", "background"));
            Set(res, Keys.CurrentFileForeground, GetColor(root, "editor_column", "current_file_foreground"));
            Set(res, Keys.MarkdownPreviewPanelBackground, GetColor(root, "markdown_preview_panel", "background"));
            Set(res, Keys.MarkdownPreviewPanelBorderBrush, GetColor(root, "markdown_preview_panel", "border_brush"));
            Set(res, Keys.SolutionExplorerBorderBrush, GetColor(root, "solution_explorer", "border_brush"));
            Set(res, Keys.SolutionExplorerHeaderForeground, GetColor(root, "solution_explorer", "header_foreground"));
            Set(res, Keys.PanelChromeTitleForeground, GetColor(root, "panel_chrome", "title_foreground"));
            Set(res, Keys.PanelTitleAccentBrush, GetColor(root, "panel_chrome", "accent_brush"));
            Set(res, Keys.PanelChromeHeaderBackground, GetColor(root, "panel_chrome", "header_background"));
            Set(res, Keys.PanelChromeHeaderSeparatorBrush, GetColor(root, "panel_chrome", "header_separator"));
            Set(res, Keys.PanelChromeMenuGlyphForeground, GetColor(root, "panel_chrome", "menu_glyph_foreground"));
            Set(res, Keys.BuildOutputBackground, GetColor(root, "build_output", "background"));
            Set(res, Keys.BuildOutputBorderBrush, GetColor(root, "build_output", "border_brush"));
            Set(res, Keys.ChatPanelBackground, GetColor(root, "chat_panel", "background"));
            Set(res, Keys.ChatLabelForeground, GetColor(root, "chat_panel", "label_foreground"));
            Set(res, Keys.ChatMessageBubbleBackground, GetColor(root, "chat_panel", "message_bubble_background"));
            Set(res, Keys.ChatMessageContentForeground, GetColor(root, "chat_panel", "message_content_foreground"));
            Set(res, Keys.SendButtonBackground, GetColor(root, "chat_panel", "send_button_background"));
            Set(res, Keys.SendButtonForeground, GetColor(root, "chat_panel", "send_button_foreground"));
            Set(res, Keys.TerminalBackground, GetColor(root, "terminal", "background"));
            Set(res, Keys.TerminalForeground, GetColor(root, "terminal", "foreground"));
            Set(res, Keys.TerminalInputBackground, GetColor(root, "terminal", "input_background"));
            Set(res, Keys.McpBannerBackground, GetColor(root, "mcp_banner", "background"));
            Set(res, Keys.McpBannerForeground, GetColor(root, "mcp_banner", "foreground"));
            Set(res, Keys.PreviewWindowBackground, GetColor(root, "preview_window", "background"));
            if (root.TryGetProperty("power_cockpit", out var pc))
            {
                Set(res, Keys.PowerNeonBorder, GetColorFrom(pc, "neon_border"));
                Set(res, Keys.PowerNeonAccent, GetColorFrom(pc, "neon_accent"));
                Set(res, Keys.PowerCockpitPanelBackground, GetColorFrom(pc, "panel_background"));
                Set(res, Keys.PowerSafetyDockBackground, GetColorFrom(pc, "safety_dock_background"));
                Set(res, Keys.PowerTelemetryStripBackground, GetColorFrom(pc, "telemetry_strip_background"));
                Set(res, Keys.PowerSafetyL1, GetColorFrom(pc, "safety_l1"));
                Set(res, Keys.PowerSafetyL2, GetColorFrom(pc, "safety_l2"));
                Set(res, Keys.PowerSafetyL3, GetColorFrom(pc, "safety_l3"));
                Set(res, Keys.PowerEmergency, GetColorFrom(pc, "emergency"));
            }
            return "OK";
        }
    }

    private static string? GetColorFrom(JsonElement parent, string prop)
    {
        return parent.TryGetProperty(prop, out var p) ? p.GetString() : null;
    }

    /// <summary>Запустить применение темы в UI-потоке и вернуть результат (для вызова из MCP).</summary>
    public static async Task<string> ApplyOnUiThreadAsync(string themeJson)
    {
        var json = themeJson ?? "";
        return await Dispatcher.UIThread.InvokeAsync(() => Apply(json));
    }

    private static Avalonia.Controls.IResourceDictionary? GetResourceDictionary()
    {
        return Avalonia.Application.Current?.Resources;
    }

    /// <summary>Рамки рабочей области: workspace_layout.border_brush или, если нет, editor_column.border_brush.</summary>
    private static string? GetWorkspacePanelBorderBrush(JsonElement root)
    {
        var w = GetColor(root, "workspace_layout", "border_brush");
        return !string.IsNullOrEmpty(w) ? w : GetColor(root, "editor_column", "border_brush");
    }

    private static string? GetColor(JsonElement root, string obj, string prop)
    {
        if (root.TryGetProperty(obj, out var o) && o.TryGetProperty(prop, out var p))
            return p.GetString();
        return null;
    }

    private static void Set(Avalonia.Controls.IResourceDictionary res, string key, string? hex)
    {
        if (string.IsNullOrEmpty(hex))
            return;
        try
        {
            var brush = SolidColorBrush.Parse(hex);
            res[key] = brush;
        }
        catch
        {
            // ignore invalid color
        }
    }
}
