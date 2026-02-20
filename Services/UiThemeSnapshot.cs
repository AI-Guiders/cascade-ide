using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Services;

/// <summary>
/// Снимок цветов и стилей UI для агента (ide_get_ui_theme).
/// Должен совпадать с MainWindow.axaml и MarkdownPreviewWindow.axaml.
/// </summary>
public static class UiThemeSnapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string GetJson()
    {
        var theme = new
        {
            main_window = new
            {
                background = "#F5F5F5"
            },
            menu = new
            {
                background = "#F0F0F0",
                foreground = "#1A1A1A"
            },
            menu_item = new
            {
                foreground = "#1A1A1A"
            },
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
            toolbar = new
            {
                background = "#E8E8E8"
            },
            toolbar_text = new
            {
                foreground = "#333",
                error_foreground = "#C00"
            },
            editor = new
            {
                background = "#FFF",
                foreground = "#1A1A1A",
                font_family = "Consolas, Cascadia Code, monospace",
                font_size = 13
            },
            editor_column = new
            {
                border_brush = "#CCC",
                background = "#FFF",
                current_file_foreground = "#666"
            },
            markdown_preview_panel = new
            {
                background = "#FAFAFA",
                border_brush = "#E0E0E0"
            },
            solution_explorer = new
            {
                border_brush = "#CCC",
                header_foreground = "#1A1A1A",
                tree_item_foreground = "#1A1A1A"
            },
            build_output = new
            {
                background = "#F8F8F8",
                foreground = "#1A1A1A",
                border_brush = "#CCC"
            },
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
            mcp_banner = new
            {
                background = "#E3F2FD",
                foreground = "#1565C0"
            },
            preview_window = new
            {
                background = "#F0F0F0",
                content_area_background = "#F0F0F0"
            }
        };

        return JsonSerializer.Serialize(theme, Options);
    }

    /// <summary>Срез темы для панели «Вывод сборки» (для ide_get_build_output).</summary>
    public static (string background, string foreground) GetBuildOutputTheme()
    {
        return ("#F8F8F8", "#1A1A1A");
    }
}
