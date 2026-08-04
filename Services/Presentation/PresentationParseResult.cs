namespace CascadeIDE.Services.Presentation;

/// <summary>Результат разбора <c>presentation</c>: список экранов, на каждом — упорядоченные якоря с опциональными весами и compose (<c>+</c>/<c>/</c>).</summary>
public sealed class PresentationParseResult
{
    private PresentationParseResult(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose> composes,
        string? error)
    {
        Screens = screens;
        ScreenComposes = composes;
        Error = error;
    }

    public IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> Screens { get; }

    /// <summary>Параллельно <see cref="Screens"/> — Split vs OneOf per group. Empty on failure.</summary>
    public IReadOnlyList<PresentationZoneCompose> ScreenComposes { get; }

    /// <summary>Не null при неуспехе; тогда <see cref="Screens"/> пустой.</summary>
    public string? Error { get; }

    public bool IsSuccess => Error is null;

    public static PresentationParseResult Ok(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose>? composes = null)
    {
        if (composes is null)
        {
            var allSplit = new PresentationZoneCompose[screens.Count];
            for (var i = 0; i < allSplit.Length; i++)
                allSplit[i] = PresentationZoneCompose.Split;
            return new(screens, allSplit, null);
        }

        if (composes.Count != screens.Count)
            throw new ArgumentException("ScreenComposes length must match Screens.", nameof(composes));

        return new(screens, composes, null);
    }

    public static PresentationParseResult Fail(string message) =>
        new(Array.Empty<IReadOnlyList<PresentationAnchorSlot>>(), Array.Empty<PresentationZoneCompose>(), message);
}
