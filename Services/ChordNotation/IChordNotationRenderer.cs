namespace CascadeIDE.Services.ChordNotation;

/// <summary>
/// Рендер <see cref="NormalizedKeySequence"/> в строку для UI / подсказок (платформенный вид не смешивается с парсером).
/// </summary>
public interface IChordNotationRenderer
{
    string Render(NormalizedKeySequence? sequence);
}
