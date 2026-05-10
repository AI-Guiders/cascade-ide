using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>
/// Извлечение тела JSON команды IDE из fenced-блока Markdown с языком <c>json-cascade</c> (ADR 0108).
/// Модель помещает туда объект <c>{ "command_id", "args" }</c> для последующего <c>invokeCSharpAction</c>.
/// </summary>
public static partial class WebAiPortalJsonCascadeFence
{
    /// <summary>Имя языка в fenced-блоке (info string после открывающих ```).</summary>
    public const string MarkdownFenceTag = "json-cascade";

    [GeneratedRegex(
        """```json-cascade[ \t]*\r?\n(.*?)```""",
        RegexOptions.Singleline | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex FenceRegex();

    /// <summary>Первый блок <c>```json-cascade … ```</c> в тексте; внутреннее содержимое без обрамления fence.</summary>
    public static bool TryExtractFirst(string? markdown, [NotNullWhen(true)] out string? jsonPayload)
    {
        jsonPayload = null;
        if (string.IsNullOrEmpty(markdown))
            return false;

        var m = FenceRegex().Match(markdown);
        if (!m.Success || m.Groups.Count < 2)
            return false;

        var inner = m.Groups[1].Value.Trim();
        if (inner.Length == 0)
            return false;

        jsonPayload = inner;
        return true;
    }
}
