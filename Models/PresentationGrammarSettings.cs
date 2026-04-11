namespace CascadeIDE.Models;

/// <summary>
/// Токены грамматики строк <c>presentation</c> / <c>zone_screen_layout</c> (ADR 0017).
/// В <c>settings.toml</c> — секция <c>[presentation_grammar]</c>.
/// </summary>
public sealed class PresentationGrammarSettings
{
    /// <summary>Маркеры границы одного экрана (два символа), по умолчанию <c>()</c>.</summary>
    public string ScreenMarkers { get; set; } = "()";

    /// <summary>Разделитель между группами экранов.</summary>
    public string ScreenSeparator { get; set; } = " ";

    /// <summary>Разделитель якорей внутри одного экрана (часто <c>+</c>).</summary>
    public string ZoneSeparator { get; set; } = "+";

    /// <summary>Литерал якоря PFD в строке презентации.</summary>
    public string PfdZoneIdentifier { get; set; } = "PFD";

    /// <summary>Литерал якоря лобового экрана.</summary>
    public string ForwardZoneIdentifier { get; set; } = "Forward";

    /// <summary>Литерал якоря MFD.</summary>
    public string MfdZoneIdentifier { get; set; } = "MFD";
}
