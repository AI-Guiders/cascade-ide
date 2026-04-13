namespace CascadeIDE.Services.Presentation;

/// <summary>Результат разбора <c>presentation</c>: список экранов, на каждом — упорядоченные якоря с опциональными весами.</summary>
public sealed class PresentationParseResult
{
    private PresentationParseResult(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        string? error)
    {
        Screens = screens;
        Error = error;
    }

    public IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> Screens { get; }

    /// <summary>Не null при неуспехе; тогда <see cref="Screens"/> пустой.</summary>
    public string? Error { get; }

    public bool IsSuccess => Error is null;

    public static PresentationParseResult Ok(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens) =>
        new(screens, null);

    public static PresentationParseResult Fail(string message) =>
        new(Array.Empty<IReadOnlyList<PresentationAnchorSlot>>(), message);
}
