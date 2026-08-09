#nullable enable

using System.Text;
using System.Text.RegularExpressions;
using CascadeIDE.Features.Settings.DataAcquisition;

namespace CascadeIDE.Intercom;

/// <summary>
/// Patch-only access to Face open_ai_model in %LocalAppData%/CascadeIDE/ai-keys.toml.
/// Does not round-trip via AiKeysStorage (that type omits model/base_url and would wipe them).
/// </summary>
public static class GlassCitizenAiKeys
{
    static readonly Regex OpenAiModelLine = new(
        @"^\s*open_ai_model\s*=\s*""([^""]*)""\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string KeysPath =>
        Path.Combine(UserSettingsPaths.GetSettingsDirectory(), "ai-keys.toml");

    public static string? TryReadOpenAiModel(string? path = null)
    {
        try
        {
            var p = path ?? KeysPath;
            if (!File.Exists(p))
                return null;
            var text = File.ReadAllText(p);
            var m = OpenAiModelLine.Match(text);
            if (!m.Success)
                return null;
            var v = m.Groups[1].Value.Trim();
            return v.Length == 0 ? null : v;
        }
        catch
        {
            return null;
        }
    }

    public static bool TryWriteOpenAiModel(string modelId, out string? error, string? path = null)
    {
        error = null;
        var id = (modelId ?? "").Trim();
        if (id.Length == 0 || string.Equals(id, "—", StringComparison.Ordinal))
        {
            error = "empty model id";
            return false;
        }

        try
        {
            var p = path ?? KeysPath;
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            var text = File.Exists(p)
                ? File.ReadAllText(p)
                : "# CDP-ADR-0026 — local secrets; do not commit\n";
            var line = "open_ai_model = \"" + EscapeTomlString(id) + "\"";
            if (OpenAiModelLine.IsMatch(text))
                text = OpenAiModelLine.Replace(text, line, 1);
            else
            {
                if (text.Length > 0 && !text.EndsWith('\n'))
                    text += "\n";
                text += line + "\n";
            }

            File.WriteAllText(p, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    static string EscapeTomlString(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
