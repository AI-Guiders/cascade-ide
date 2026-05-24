#nullable enable

using CascadeIDE.Features.Chat;

namespace CascadeIDE.Services.Intercom;

/// <summary>Разбор хвоста <c>/intercom message … relate …</c> (ADR 0137 contiguous, 0138 disjoint).</summary>
public static class IntercomMessageRelateArgs
{
    public static bool TryParse(
        string? tail,
        out IReadOnlyList<ParametricIntRange> segments,
        out string codeRefTail,
        out string error)
    {
        segments = [];
        codeRefTail = "";
        error = "";

        var text = (tail ?? "").Trim();
        if (text.Length == 0)
        {
            error = "Укажи диапазон сообщений и фрагмент кода: «3:5 relate selection», «3:5 relate M:Foo» или «[3;5] [8;15] relate selection».";
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

        if (!ParametricSegmentListParser.TryParse(rangePart, out segments, out error))
            return false;

        codeRefTail = codePart;
        return true;
    }

    /// <summary>Contiguous-only parse (legacy callers).</summary>
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
        if (!TryParse(tail, out var segments, out codeRefTail, out error))
            return false;

        if (segments.Count != 1)
        {
            error = segments.Count == 0
                ? "Пустой диапазон."
                : "Ожидается один contiguous сегмент (например 3:5). Для disjoint используйте [3;5] [8;15].";
            return false;
        }

        startOrdinal = segments[0].Start;
        endOrdinal = segments[0].End;
        return true;
    }
}
