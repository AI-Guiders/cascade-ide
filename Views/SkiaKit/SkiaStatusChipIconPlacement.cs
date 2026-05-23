#nullable enable

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Расположение глифа валидации в slash pill (TCI).</summary>
internal enum SkiaStatusChipIconPlacement
{
    Left = 0,
    Right = 1,
    /// <summary>Только рамка и акцентный текст, без глифа.</summary>
    HighlightOnly = 2,
}

internal static class TciValidationIconPlacementParser
{
    public const string Left = "left";
    public const string Right = "right";
    public const string HighlightOnly = "highlight_only";

    public static SkiaStatusChipIconPlacement Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SkiaStatusChipIconPlacement.Right;

        return value.Trim().ToLowerInvariant() switch
        {
            Left => SkiaStatusChipIconPlacement.Left,
            Right => SkiaStatusChipIconPlacement.Right,
            HighlightOnly => SkiaStatusChipIconPlacement.HighlightOnly,
            _ => SkiaStatusChipIconPlacement.Right,
        };
    }
}
