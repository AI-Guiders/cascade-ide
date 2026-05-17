#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

internal enum SkiaSectionedSectionStyle
{
    /// <summary>Первая секция: крупный заголовок (тема, имя узла).</summary>
    Header,
    /// <summary>Обычное тело секции (теги, саммари, атрибуты).</summary>
    Body
}

internal readonly record struct SkiaSectionedCardSection(
    string Label,
    IReadOnlyList<string> Lines,
    SkiaSectionedSectionStyle Style = SkiaSectionedSectionStyle.Body);

internal readonly record struct SkiaSectionedCardModel(IReadOnlyList<SkiaSectionedCardSection> Sections);

internal readonly record struct SkiaSectionedCardDrawState(
    SKColor FillColor,
    bool IsHovered,
    bool IsFocused);
