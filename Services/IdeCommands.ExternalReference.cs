namespace CascadeIDE.Services;

/// <summary>Публичные справочные запросы вне workspace (IdeCommands).</summary>
public static partial class IdeCommands
{
    /// <summary>Краткая веб-справка через открытый Instant Answer DuckDuckGo (запрос уходит во внешнюю сеть; не замена полнотекстового поиска). args: query:string; returns: json; example: {\"query\":\"C# file scoped types\"}.</summary>
    public const string SearchWebPublicQuery = "search_web_public_query";

    /// <summary>Загрузить публичный HTTPS-документ по URL и вернуть тело как читаемый текст (HTML упрощается до текста, поле extraction в JSON). Запрос из машины оператора; только https; локальные/частные хосты блокируются базово (не полная SSRF-защита). args: url:string, max_chars?:integer; returns: json; example: {\"url\":\"https://learn.microsoft.com/en-us/dotnet/\"}.</summary>
    public const string FetchWebPublicUrl = "fetch_web_public_url";
}
