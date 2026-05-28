#nullable enable

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Fragment ids и line hints для scroll в preview.</summary>
public sealed class MarkdownPreviewAnchorRegistry
{
    private readonly Dictionary<string, Control> _fragments = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(int Line, Control Control)> _lineAnchors = new();

    public ScrollViewer? ScrollHost { get; set; }

    public void RegisterFragment(string id, Control control)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        _fragments[id.Trim()] = control;
    }

    public void RegisterLine(int line, Control control)
    {
        if (line < 1)
            return;

        _lineAnchors.Add((line, control));
    }

    public void ScrollToFragment(string fragmentId)
    {
        if (string.IsNullOrWhiteSpace(fragmentId))
            return;

        if (!_fragments.TryGetValue(fragmentId.Trim(), out var control))
            return;

        ScrollToControl(control);
    }

    public void ScrollToLine(int line)
    {
        if (line < 1 || _lineAnchors.Count == 0)
            return;

        Control? best = null;
        var bestDelta = int.MaxValue;
        foreach (var (anchorLine, control) in _lineAnchors)
        {
            var delta = Math.Abs(anchorLine - line);
            if (delta >= bestDelta)
                continue;

            bestDelta = delta;
            best = control;
            if (delta == 0)
                break;
        }

        if (best is not null)
            ScrollToControl(best);
    }

    private void ScrollToControl(Control target)
    {
        if (ScrollHost?.Content is not Control content)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            var point = target.TranslatePoint(new Point(0, 0), content);
            if (point is null)
                return;

            ScrollHost.Offset = new Vector(
                ScrollHost.Offset.X,
                Math.Max(0, point.Value.Y - 16));
        }, DispatcherPriority.Loaded);
    }
}
