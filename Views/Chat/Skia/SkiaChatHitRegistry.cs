#nullable enable

using Avalonia;
using CascadeIDE.Models.Intercom;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>
/// Единый реестр pointer hit для Skia-ленты чата: регистрация в content-space, поиск в control-space.
/// Приоритет при перекрытии: <see cref="SkiaChatHit.RevealAttachment"/> → <see cref="SkiaChatPointerAction"/> (chrome) → z-order.
/// </summary>
internal sealed class SkiaChatHitRegistry
{
    private readonly List<(Rect Bounds, SkiaChatHit Hit)> _entries = [];

    public int Count => _entries.Count;

    public void Clear() => _entries.Clear();

    /// <summary>Координаты как при Draw (до canvas.Restore): Y сдвигается на <paramref name="scrollOffset"/>.</summary>
    public void RegisterContentRect(SKRect contentRect, float scrollOffset, SkiaChatHit hit) =>
        RegisterControlRect(
            new Rect(
                contentRect.Left,
                contentRect.Top - scrollOffset,
                contentRect.Width,
                contentRect.Height),
            hit);

    public void RegisterControlRect(Rect controlRect, SkiaChatHit hit) =>
        _entries.Add((controlRect, hit));

    public int FindIndex(Point controlPoint) =>
        FindIndex(_entries, controlPoint);

    /// <summary>Общая политика hit-test (unit-тесты и реестр).</summary>
    public static int FindIndex(IReadOnlyList<(Rect Bounds, SkiaChatHit Hit)> entries, Point controlPoint)
    {
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].Bounds.Contains(controlPoint)
                && entries[i].Hit.RevealAttachment is not null)
            {
                return i;
            }
        }

        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].Bounds.Contains(controlPoint)
                && entries[i].Hit.PointerAction != SkiaChatPointerAction.None)
            {
                return i;
            }
        }

        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].Bounds.Contains(controlPoint))
                return i;
        }

        return -1;
    }

    public bool ContainsPointerAction(Point controlPoint, SkiaChatPointerAction action)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Bounds.Contains(controlPoint)
                && _entries[i].Hit.PointerAction == action)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetHit(int index, out SkiaChatHit hit)
    {
        if ((uint)index >= (uint)_entries.Count)
        {
            hit = default;
            return false;
        }

        hit = _entries[index].Hit;
        return true;
    }

    public static bool WantsHandCursor(in SkiaChatHit hit) =>
        hit.RevealAttachment is not null
        || hit.PointerAction is SkiaChatPointerAction.ComposerSend
            or SkiaChatPointerAction.OverviewToggle
            or SkiaChatPointerAction.TopicTabSelect
            or SkiaChatPointerAction.TopicTabCreate
            or SkiaChatPointerAction.TopicTabOverflow
            or SkiaChatPointerAction.TopicNavigatorToggle;

    public static bool IsChromeAction(in SkiaChatHit hit) =>
        hit.PointerAction != SkiaChatPointerAction.None;

    public static bool IsComposerAction(in SkiaChatHit hit) =>
        hit.PointerAction is SkiaChatPointerAction.ComposerSend
            or SkiaChatPointerAction.ComposerFocus
            or SkiaChatPointerAction.SlashPopup;
}
