namespace CascadeIDE.Models;

/// <summary>
/// Токены грамматики строки топологии (ADR 0017).
/// TOML: <c>[presentation.grammar]</c>.
/// </summary>
public sealed class PresentationGrammarSettings
{
    /// <summary>Два символа границы одного экрана (по умолчанию <c>()</c>).</summary>
    public string Brackets { get; set; } = "()";

    /// <summary>Разделитель между группами экранов.</summary>
    public string BetweenScreens { get; set; } = " ";

    /// <summary>Разделитель якорей внутри экрана (часто <c>+</c>).</summary>
    public string BetweenZones { get; set; } = "+";

    /// <summary>Литерал якоря PFD в строке.</summary>
    public string Pfd { get; set; } = "PFD";

    /// <summary>Литерал якоря Forward.</summary>
    public string Forward { get; set; } = "Forward";

    /// <summary>Литерал якоря MFD.</summary>
    public string Mfd { get; set; } = "MFD";
}
