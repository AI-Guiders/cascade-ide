using CascadeIDE.Models;
using Tomlyn.Model;

namespace CascadeIDE.Services;

/// <summary>
/// Перенос плоских ключей старого <c>settings.toml</c> в секции <c>[csharp_lsp]</c>, <c>[markdown_lsp]</c>, <c>[markdown_diagrams]</c>,
/// если соответствующей секции в файле ещё нет (десериализатор CascadeToml игнорирует неизвестные корневые ключи).
/// </summary>
internal static class SettingsTomlMigration
{
    public static void ApplyLegacyFlatKeys(CascadeIdeSettings settings, string tomlText)
    {
        TomlTable? root;
        try
        {
            root = global::Tomlyn.TomlSerializer.Deserialize<TomlTable>(tomlText);
        }
        catch
        {
            return;
        }

        if (root is null)
            return;

        if (!root.ContainsKey("csharp_lsp"))
        {
            if (TryGetString(root, "c_sharp_lsp_provider", out var p))
                settings.CSharpLsp.Provider = p;
            if (TryGetString(root, "c_sharp_lsp_executable", out var e))
                settings.CSharpLsp.Executable = e;
            if (TryGetString(root, "c_sharp_lsp_arguments", out var a))
                settings.CSharpLsp.Arguments = a;
        }

        if (!root.ContainsKey("markdown_lsp"))
        {
            if (TryGetString(root, "markdown_lsp_provider", out var p))
                settings.MarkdownLsp.Provider = p;
            if (TryGetString(root, "markdown_lsp_executable", out var e))
                settings.MarkdownLsp.Executable = e;
            if (TryGetString(root, "markdown_lsp_arguments", out var a))
                settings.MarkdownLsp.Arguments = a;
        }

        if (!root.ContainsKey("markdown_diagrams"))
        {
            if (TryGetBool(root, "markdown_kroki_enabled", out var ke))
                settings.MarkdownDiagrams.KrokiEnabled = ke;
            if (TryGetString(root, "markdown_kroki_base_url", out var ku))
                settings.MarkdownDiagrams.KrokiBaseUrl = ku;
        }
    }

    private static bool TryGetString(TomlTable root, string key, out string value)
    {
        value = "";
        if (!root.TryGetValue(key, out var o) || o is null)
            return false;
        value = o switch
        {
            string s => s,
            _ => o.ToString() ?? "",
        };
        return true;
    }

    private static bool TryGetBool(TomlTable root, string key, out bool value)
    {
        value = default;
        if (!root.TryGetValue(key, out var o) || o is null)
            return false;
        value = o switch
        {
            bool b => b,
            _ => bool.TryParse(o.ToString(), out var p) && p,
        };
        return true;
    }
}
