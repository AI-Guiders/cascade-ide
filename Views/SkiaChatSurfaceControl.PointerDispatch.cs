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
            return dispatchChromePointerPress(hit, e);

        return dispatchFeedPointerPress(hit, point, e);
    }

    private bool dispatchChromePointerPress(in SkiaChatHit hit, PointerPressedEventArgs e)
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
                var popupPoint = e.GetPosition(this);
                var row = SkiaPopupList.HitTestRow(
                    _slashPopupBounds,
                    (float)popupPoint.X,
                    (float)popupPoint.Y,
                    _slashRows.Count,
                    _slashPopupScrollOffset,
                    ShowSlashHierarchyHeader);
                if (row < 0)
                    return false;
                SelectedSlashSuggestionIndex = row;
                ComposerKeyDown?.Invoke(this, new IntercomComposerKeyEventArgs(IntercomComposerKeyKind.CommitSlashSuggestion));
                return true;
            }
            case SkiaChatPointerAction.CommandLineFocus:
                if (!ShowIntercomComposer || !ShowCockpitCommandLine)
                    return false;
                ClearNavigatorSearchFocus();
                _commandLineFocused = true;
                Focus();
                return true;
            case SkiaChatPointerAction.ComposerFocus:
                if (!ShowIntercomComposer)
                    return false;
                ClearNavigatorSearchFocus();
                _commandLineFocused = false;
                Focus();
                var composerPoint = e.GetPosition(this);
                TryPlaceComposerCaretAtPoint(
                    (float)composerPoint.X,
                    (float)composerPoint.Y,
                    e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                return true;
            case SkiaChatPointerAction.TopicNavigatorSearchFocus:
                FocusNavigatorSearch();
                var searchPoint = e.GetPosition(this);
                TryPlaceNavigatorSearchCaretAtPoint((float)searchPoint.X, (float)searchPoint.Y);
                return true;
            case SkiaChatPointerAction.TopicTabSelect when hit.SelectThreadId is { } tabThreadId:
                if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                {
                    TopicRenameRequested?.Invoke(this, new TopicRenameRequestEventArgs(tabThreadId, showContextMenu: true));
                    return true;
                }

                if (e.ClickCount >= 2)
                {
                    TopicRenameRequested?.Invoke(this, new TopicRenameRequestEventArgs(tabThreadId, showContextMenu: false));
                    return true;
                }

                DetailThreadId = tabThreadId;
                OverviewMode = false;
                return true;
            case SkiaChatPointerAction.TopicTabCreate:
                TopicCreateRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case SkiaChatPointerAction.TopicTabOverflow:
                OverviewMode = true;
                return true;
            case SkiaChatPointerAction.TopicNavigatorToggle:
                TopicNavigatorToggleRequested?.Invoke(this, EventArgs.Empty);
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
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                TopicRenameRequested?.Invoke(this, new TopicRenameRequestEventArgs(threadId, showContextMenu: true));
                return true;
            }

            if (e.ClickCount >= 2)
            {
                TopicRenameRequested?.Invoke(this, new TopicRenameRequestEventArgs(threadId, showContextMenu: false));
                return true;
            }

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
        {
            if (TryScrollComposer((float)(e.Delta.Y * WheelPixelsPerDelta)))
                return true;
            return true;
        }

        return false;
    }

    private void registerChromePointerHits(SKRect overviewButtonBounds, SKRect navigatorToggleBounds)
    {
        if (navigatorToggleBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(navigatorToggleBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.TopicNavigatorToggle));
        }

        if (overviewButtonBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(overviewButtonBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.OverviewToggle));
        }
    }

    private void registerTopicNavigatorPointerHits(SkiaIntercomTopicNavigator.LayoutResult layout, float panelLeft)
    {
        if (layout.SearchBounds.Width > 0)
        {
            _navigatorSearchBounds = layout.SearchBounds;
            var searchBounds = layout.SearchBounds;
            if (panelLeft != 0f)
                searchBounds.Offset(panelLeft, 0f);

            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(searchBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.TopicNavigatorSearchFocus));
        }

        // RowHits.Bounds уже в координатах контрола (MapRowBoundsToPanel в Draw).
        foreach (var row in layout.RowHits)
        {
            var bounds = row.Bounds;
            if (panelLeft != 0f)
                bounds.Offset(panelLeft, 0f);

            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(bounds),
                new SkiaChatHit(null, row.ThreadId, ResetDetailMode: false));
        }
    }

    private void registerNavigationPointerHits(SkiaIntercomNavigationChrome.LayoutResult layout)
    {
        foreach (var tab in layout.TabHits)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(tab.Bounds),
                new SkiaChatHit(
                    null,
                    tab.ThreadId,
                    ResetDetailMode: false,
                    PointerAction: SkiaChatPointerAction.TopicTabSelect));
        }

        if (layout.CreateButtonBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(layout.CreateButtonBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.TopicTabCreate));
        }

        if (layout.OverflowBounds is { } overflow && layout.OverflowHiddenCount > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(overflow),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.TopicTabOverflow));
        }
    }
}
