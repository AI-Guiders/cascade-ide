#nullable enable

using Avalonia;
using Avalonia.Input;
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views;

public partial class SkiaChatSurfaceControl
{
    private bool TryDispatchPointerPress(Point point, PointerPressedEventArgs e)
    {
        var index = _chatHits.FindIndex(point);
        if (index < 0 || !_chatHits.TryGetHit(index, out var hit))
            return false;

        if (SkiaChatHitRegistry.IsChromeAction(hit))
            return dispatchChromePointerPress(hit, point);

        return dispatchFeedPointerPress(hit, point, e);
    }

    private bool dispatchChromePointerPress(in SkiaChatHit hit, Point point)
    {
        switch (hit.PointerAction)
        {
            case SkiaChatPointerAction.OverviewToggle:
                OverviewMode = !OverviewMode;
                return true;
            case SkiaChatPointerAction.ComposerSend:
                if (!ShowIntercomComposer)
                    return false;
                Focus();
                if (IsComposerEnabled)
                    SendRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case SkiaChatPointerAction.SlashPopup when ShowIntercomComposer && _slashPopupBounds.Width > 0:
            {
                var row = SkiaPopupList.HitTestRow(
                    _slashPopupBounds,
                    (float)point.X,
                    (float)point.Y,
                    _slashRows.Count,
                    _slashPopupScrollOffset);
                if (row < 0)
                    return false;
                SelectedSlashSuggestionIndex = row;
                ComposerKeyDown?.Invoke(this, new IntercomComposerKeyEventArgs(IntercomComposerKeyKind.CommitSlashSuggestion));
                return true;
            }
            case SkiaChatPointerAction.CommandLineFocus:
                if (!ShowIntercomComposer || !ShowCockpitCommandLine)
                    return false;
                _commandLineFocused = true;
                Focus();
                return true;
            case SkiaChatPointerAction.ComposerFocus:
                if (!ShowIntercomComposer)
                    return false;
                _commandLineFocused = false;
                Focus();
                return true;
            default:
                return false;
        }
    }

    private bool dispatchFeedPointerPress(in SkiaChatHit hit, Point point, PointerPressedEventArgs e)
    {
        if (hit.RevealAttachment is { } attachAnchor)
        {
            var select = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            AttachmentRevealRequested?.Invoke(
                this,
                new IntercomAttachmentRevealEventArgs(attachAnchor, select, hit.MessageIndex));
            return true;
        }

        if (hit.ResetDetailMode)
        {
            OverviewMode = true;
            return true;
        }

        if (hit.SelectThreadId is { } threadId)
        {
            DetailThreadId = threadId;
            OverviewMode = false;
            return true;
        }

        if (hit.MessageIndex is not { } messageIndex)
            return false;

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            MessageSelectContextRequested?.Invoke(this, messageIndex);
            return true;
        }

        if (hit.ToggleThinking && e.ClickCount >= 2)
            ThinkingToggleRequested?.Invoke(this, messageIndex);

        return true;
    }

    private bool TryDispatchPointerWheel(Point point, PointerWheelEventArgs e)
    {
        if (_chatHits.ContainsPointerAction(point, SkiaChatPointerAction.SlashPopup)
            && _slashRows.Count > 0)
        {
            var deltaRows = e.Delta.Y > 0 ? -1 : e.Delta.Y < 0 ? 1 : 0;
            if (deltaRows == 0)
                return false;

            _slashPopupScrollOffset = SkiaPopupList.ClampScrollOffset(
                _slashPopupScrollOffset + deltaRows,
                _slashRows.Count);
            return true;
        }

        if (_chatHits.ContainsPointerAction(point, SkiaChatPointerAction.ComposerFocus))
            return true;

        return false;
    }

    private void registerChromePointerHits(SKRect overviewButtonBounds)
    {
        if (overviewButtonBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(overviewButtonBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.OverviewToggle));
        }
    }
}
