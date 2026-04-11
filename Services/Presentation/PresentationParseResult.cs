namespace CascadeIDE.Services.Presentation;

/// <summary>Результат разбора <c>presentation</c>: список экранов, на каждом — упорядоченные якоря.</summary>
public sealed class PresentationParseResult
{
    private PresentationParseResult(
        IReadOnlyList<IReadOnlyList<PresentationAnchorKind>> screens,
        string? error)
    {
        Screens = screens;
        Error = error;
    }

    public IReadOnlyList<IReadOnlyList<PresentationAnchorKind>> Screens { get; }

    /// <summary>Не null при неуспехе; тогда <see cref="Screens"/> пустой.</summary>
    public string? Error { get; }

    public bool IsSuccess => Error is null;

    public static PresentationParseResult Ok(IReadOnlyList<IReadOnlyList<PresentationAnchorKind>> screens) =>
        new(screens, null);

    public static PresentationParseResult Fail(string message) =>
        new(Array.Empty<IReadOnlyList<PresentationAnchorKind>>(), message);
}
