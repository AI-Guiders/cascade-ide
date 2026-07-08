#nullable enable

using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class SkiaChatSurfaceControl
{
    public event EventHandler<IntercomAttachDropEventArgs>? IntercomAttachDropped;

    private void InitializeIntercomAttachDrop()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnIntercomAttachDragOver, RoutingStrategies.Tunnel);
        AddHandler(DragDrop.DropEvent, OnIntercomAttachDrop, RoutingStrategies.Tunnel);
    }

    internal bool TryHitComposerBounds(float x, float y) =>
        ShowIntercomComposer
        && _composerBounds.Width > 0
        && x >= _composerBounds.Left
        && x <= _composerBounds.Right
        && y >= _composerBounds.Top
        && y <= _composerBounds.Bottom;

    private void OnIntercomAttachDragOver(object? sender, DragEventArgs e)
    {
        if (!ShowIntercomComposer || !TryReadAttachPayload(e.DataTransfer, out _))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnIntercomAttachDrop(object? sender, DragEventArgs e)
    {
        if (!ShowIntercomComposer || !TryReadAttachPayload(e.DataTransfer, out var payload))
            return;

        var point = e.GetPosition(this);
        if (!TryHitComposerBounds((float)point.X, (float)point.Y))
            return;

        e.Handled = true;
        IntercomAttachDropped?.Invoke(this, new IntercomAttachDropEventArgs(payload, point));
    }

    private static bool TryReadAttachPayload(IDataTransfer dataTransfer, out string payload)
    {
        payload = "";
        var text = dataTransfer.TryGetText();
        if (!string.IsNullOrEmpty(text)
            && text.StartsWith(IntercomAttachDragFormats.TextPrefix, StringComparison.Ordinal))
        {
            payload = text[IntercomAttachDragFormats.TextPrefix.Length..];
            return payload.Length > 0;
        }

        return false;
    }
}

public sealed class IntercomAttachDropEventArgs : EventArgs
{
    public IntercomAttachDropEventArgs(string payload, Point positionInSurface)
    {
        Payload = payload;
        PositionInSurface = positionInSurface;
    }

    public string Payload { get; }

    public Point PositionInSurface { get; }
}

internal static class IntercomAttachDragPayload
{
    public static string ForKind(string kind) => IntercomAttachDragFormats.EncodeTextPayload(kind);

    public static string ForProblem(ProblemListItem item) =>
        IntercomAttachDragFormats.EncodeTextPayload(JsonSerializer.Serialize(new
        {
            kind = IntercomAttachDragFormats.KindProblem,
            filePath = item.FilePath,
            line = item.Line,
            column = item.Column,
            severity = item.Severity,
            id = item.Id,
            message = item.Message,
        }));
}
