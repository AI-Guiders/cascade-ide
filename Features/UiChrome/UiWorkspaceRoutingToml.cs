namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Секция <c>[routing]</c> в <c>workspace.toml</c>: внимание и слоты инструментов.
/// В файле удобно задавать одну таблицу <c>[routing]</c> с inline-картами
/// <c>attention = { … }</c> и <c>instruments = { … }</c> — при двух отдельных заголовках
/// <c>[routing.attention]</c> и <c>[routing.instruments]</c> Tomlyn 2.x может перезаписывать вложенный объект.
/// </summary>
public sealed class UiWorkspaceRoutingToml
{
    /// <summary>ADR 0021/0051: intent → зона.</summary>
    public Dictionary<string, string>? Attention { get; set; }

    /// <summary>ADR 0050: слот → alias инструмента.</summary>
    public Dictionary<string, string>? Instruments { get; set; }
}
