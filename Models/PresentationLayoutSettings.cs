namespace CascadeIDE.Models;

/// <summary>
/// Строка топологии дисплеев и токены грамматики (ADR 0017).
/// TOML: <c>[presentation]</c>, <c>[presentation.grammar]</c> (legacy; канон топологии — <c>[display.screens]</c> в <see cref="DisplayScreensSettings"/>).
/// </summary>
public sealed class PresentationLayoutSettings
{
    /// <summary>Основная строка раскладки (бывш. <c>presentation</c>).</summary>
    public string Line { get; set; } = "";

    /// <summary>Синоним <see cref="Line"/>; приоритет у <see cref="Line"/>.</summary>
    public string LineAlias { get; set; } = "";

    /// <summary>Литералы и разделители для парсера строки.</summary>
    public PresentationGrammarSettings Grammar { get; set; } = new();
}
