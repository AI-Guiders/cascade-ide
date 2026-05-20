#nullable enable

using CascadeIDE.Features.Chat;

namespace CascadeIDE.Services.Intercom;

/// <summary>Разбор хвоста <c>/intercom message … relate …</c> (ADR 0137).</summary>
public static class IntercomMessageRelateArgs
{
    public static bool TryParse(
        string? tail,
        out int startOrdinal,
        out int endOrdinal,
        out string codeRefTail,
        out string error)
    {
        startOrdinal = 0;
        endOrdinal = 0;
        codeRefTail = "";
        error = "";

        var text = (tail ?? "").Trim();
        if (text.Length == 0)
        {
            error = "Укажи диапазон сообщений и фрагмент кода: «3:5 relate selection».";
            return false;
        }

        string rangePart;
        string codePart;
        var relateIdx = text.IndexOf(" relate ", StringComparison.OrdinalIgnoreCase);
        if (relateIdx >= 0)
        {
            rangePart = text[..relateIdx].Trim();
            codePart = text[(relateIdx + " relate ".Length)..].Trim();
        }
        else
        {
            var space = text.IndexOf(' ');
            if (space < 0)
            {
                error = "После диапазона сообщений укажи фрагмент кода (selection, L:…, [M:…]).";
                return false;
            }

            rangePart = text[..space].Trim();
            codePart = text[(space + 1)..].Trim();
        }

        if (rangePart.Length == 0 || codePart.Length == 0)
        {
            error = "Укажи диапазон gutter (1-based) и фрагмент кода.";
            return false;
        }

        if (!ChatSlashParametricArgsBuilder.TryParseLineRangeTail(rangePart, out startOrdinal, out endOrdinal, out error))
            return false;

        codeRefTail = codePart;
        return true;
    }
}
