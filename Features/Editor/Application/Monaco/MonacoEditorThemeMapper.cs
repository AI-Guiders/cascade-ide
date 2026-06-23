namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Maps Cascade UI theme to Monaco <c>defineTheme</c> (ADR 0163 M11).</summary>
public static class MonacoEditorThemeMapper
{
    public const string CascadeDarkThemeId = "cascade-dark";

    public static string ResolveThemeId(bool isDark) => isDark ? CascadeDarkThemeId : "vs";

    public static object BuildDefineThemePayload(bool isDark) =>
        isDark
            ? new Dictionary<string, object>
            {
                ["base"] = "vs-dark",
                ["inherit"] = true,
                ["rules"] = Array.Empty<object>(),
                ["colors"] = new Dictionary<string, string>
                {
                    ["editor.background"] = "#1e1e1e",
                    ["editor.foreground"] = "#d4d4d4",
                    ["editorLineNumber.foreground"] = "#858585",
                    ["editor.selectionBackground"] = "#264f78",
                    ["editor.inactiveSelectionBackground"] = "#3a3d41",
                },
            }
            : new Dictionary<string, object>
            {
                ["base"] = "vs",
                ["inherit"] = true,
                ["rules"] = Array.Empty<object>(),
                ["colors"] = new Dictionary<string, string>
                {
                    ["editor.background"] = "#ffffff",
                    ["editor.foreground"] = "#1e1e1e",
                },
            };
}
