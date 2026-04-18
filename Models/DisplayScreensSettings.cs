namespace CascadeIDE.Models;

/// <summary>
/// Топология экранов пресета под <see cref="DisplaySettings"/> (ADR 0017 § <c>display.screens</c> / <c>topology</c>).
/// Канон TOML: <c>[display.screens]</c>, <c>topology</c>, <c>[display.screens.grammar]</c>.
/// При пустом <see cref="Topology"/> используются legacy <c>[presentation]</c> / <c>line</c> и <c>[presentation.grammar]</c>.
/// </summary>
public sealed class DisplayScreensSettings
{
    /// <summary>Строка якорей по «экранам» (бывш. корневой <c>presentation.line</c>).</summary>
    public string Topology { get; set; } = "";

    /// <summary>Токены грамматики для <see cref="Topology"/>.</summary>
    public PresentationGrammarSettings Grammar { get; set; } = new();
}
