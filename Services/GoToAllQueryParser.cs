namespace CascadeIDE.Services;

/// <summary>Префиксы как в Visual Studio «Перейти к» / Go to All: <c>f:</c> файлы, <c>t:</c> типы, <c>m:</c> члены, <c>x:</c> текст.</summary>
public readonly record struct GoToAllQuery(char Prefix, string Term);

public static class GoToAllQueryParser
{
    /// <summary>Парсит <c>f:</c>, <c>t:</c>, <c>m:</c>, <c>x:</c> в начале строки (без учёта регистра префикса).</summary>
    public static GoToAllQuery? TryParse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;
        var t = query.TrimStart();
        if (t.Length < 2 || t[1] != ':')
            return null;
        var p = char.ToLowerInvariant(t[0]);
        if (p is not ('f' or 't' or 'm' or 'x'))
            return null;
        var rest = t.Length > 2 ? t[2..].TrimStart() : "";
        return new GoToAllQuery(p, rest);
    }
}
