using System.Text.RegularExpressions;
using CascadeIDE.Contracts;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Построение паттерна ripgrep для префиксов Go-to палитры (<c>x:</c>/<c>t:</c>/<c>m:</c>).</summary>
[PresentationProjection("goto-palette-ripgrep-pattern")]
public static class GoToPaletteRipgrepPatternBuilder
{
    public static (string Pattern, bool FixedString, string? Glob) Build(GoToAllQuery q)
    {
        var term = q.Term.Trim();
        return q.Prefix switch
        {
            'x' => (term, true, null),
            't' => TypeNameSearchPattern(term),
            'm' => MemberNameSearchPattern(term),
            _ => (term, true, null),
        };
    }

    private static (string Pattern, bool FixedString, string? Glob) TypeNameSearchPattern(string term)
    {
        var esc = Regex.Escape(term);
        var pattern = $@"(class|interface|enum|record|struct)\s+\S*{esc}\S*";
        return (pattern, false, "*.cs");
    }

    private static (string Pattern, bool FixedString, string? Glob) MemberNameSearchPattern(string term)
    {
        var esc = Regex.Escape(term);
        var pattern = $@"\b{esc}\b\s*\(";
        return (pattern, false, "*.cs");
    }
}
