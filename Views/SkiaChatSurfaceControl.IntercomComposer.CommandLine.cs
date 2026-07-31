#nullable enable
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Controls;
using Avalonia.Threading;
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views;

public partial class SkiaChatSurfaceControl
{
    internal Rect GetCommandLineCaretScreenRect()
    {
        var cclPt = IntercomFonts.ResolveCommandLinePt(FeedUsesForwardMetrics);
        if (_commandLineBounds.Width > 0
            && SkiaCommandLineStrip.TryGetCaretRect(
                _commandLineBounds,
                CommandLineText ?? "/",
                CommandLineCaretIndex,
                cclPt,
                _commandLineScrollOffsetX,
                out var caret,
                CommandLinePreviewKind))
            return new Rect(caret.Left, caret.Top, Math.Max(2, caret.Width), caret.Height);

        var region = SkiaCommandLineStrip.ComputeInputRegion(
            _commandLineBounds,
            cclPt,
            SkiaCommandLineStrip.ShouldReserveLeadingChip(CommandLinePreviewKind, CommandLineText ?? "/"));
        var lineH = SkiaCommandLineStrip.InputLineHeightFor(cclPt);
        return new Rect(region.TextBounds.Left, region.TextBounds.Top + 2f, 2, lineH - 4f);
    }

    internal bool TryPlaceCommandLineCaretAtPoint(float x, float y, bool extendSelection)
    {
        if (_commandLineBounds.Width <= 0 || !ShowCockpitCommandLine)
            return false;

        var cclPt = IntercomFonts.ResolveCommandLinePt(FeedUsesForwardMetrics);
        if (!SkiaCommandLineStrip.TryHitTestCaretAtPoint(
                _commandLineBounds,
                CommandLineText ?? "/",
                x,
                y,
                cclPt,
                _commandLineScrollOffsetX,
                out var index,
                CommandLinePreviewKind))
            return false;

        _commandLineExtendSelection = extendSelection;
        if (!extendSelection)
            _commandLineSelectionAnchor = index;

        CommandLineCaretIndex = index;
        EnsureCommandLineCaretVisible();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyCommandLineDraftChanged();
        return true;
    }

    private void EnsureCommandLineCaretVisible()
    {
        if (_commandLineBounds.Width <= 0)
            return;

        var cclPt = IntercomFonts.ResolveCommandLinePt(FeedUsesForwardMetrics);
        var cclText = CommandLineText ?? "/";
        var reserveChip = SkiaCommandLineStrip.ShouldReserveLeadingChip(CommandLinePreviewKind, cclText);
        var region = SkiaCommandLineStrip.ComputeInputRegion(_commandLineBounds, cclPt, reserveChip);
        var maxScroll = SkiaCommandLineStrip.MaxHorizontalScroll(
            cclText,
            region.ContentWidth,
            cclPt);

        if (!SkiaCommandLineStrip.TryGetCaretRect(
                _commandLineBounds,
                cclText,
                CommandLineCaretIndex,
                cclPt,
                _commandLineScrollOffsetX,
                out var caret,
                CommandLinePreviewKind))
            return;

        var viewLeft = region.TextBounds.Left;
        var viewRight = region.TextBounds.Right;
        if (caret.Left < viewLeft)
            _commandLineScrollOffsetX = Math.Max(0, _commandLineScrollOffsetX - (viewLeft - caret.Left));
        else if (caret.Right > viewRight)
            _commandLineScrollOffsetX = Math.Min(maxScroll, _commandLineScrollOffsetX + (caret.Right - viewRight));

        _commandLineScrollOffsetX = Math.Clamp(_commandLineScrollOffsetX, 0f, maxScroll);
    }

    internal bool TryScrollCommandLine(float deltaX)
    {
        if (_commandLineBounds.Width <= 0)
            return false;

        var cclPt = IntercomFonts.ResolveCommandLinePt(FeedUsesForwardMetrics);
        var cclText = CommandLineText ?? "/";
        var region = SkiaCommandLineStrip.ComputeInputRegion(
            _commandLineBounds,
            cclPt,
            SkiaCommandLineStrip.ShouldReserveLeadingChip(CommandLinePreviewKind, cclText));
        var maxScroll = SkiaCommandLineStrip.MaxHorizontalScroll(
            cclText,
            region.ContentWidth,
            cclPt);
        if (maxScroll <= 0f)
            return false;

        _commandLineScrollOffsetX = Math.Clamp(_commandLineScrollOffsetX - deltaX, 0f, maxScroll);
        InvalidateComposerChrome();
        return true;
    }

    private void InsertCommandLineText(string text)
    {
        if (!IsKeyboardFocusWithin)
            Focus();

        ClearNavigatorSearchFocus();
        _commandLineFocused = true;
        var current = CommandLineText ?? "";
        var caret = Math.Clamp(CommandLineCaretIndex, 0, current.Length);
        if (HasCommandLineSelection)
        {
            var (start, end) = GetCommandLineSelectionRange();
            current = current[..start] + current[end..];
            caret = start;
            _commandLineSelectionAnchor = start;
        }

        CommandLineCaretIndex = caret + text.Length;
        _commandLineSelectionAnchor = CommandLineCaretIndex;
        CommandLineText = current.Insert(caret, text);
        EnsureCommandLineCaretVisible();
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyCommandLineDraftChanged();
    }

    private void DeleteCommandLine(int direction)
    {
        var current = CommandLineText ?? "";
        var caret = Math.Clamp(CommandLineCaretIndex, 0, current.Length);
        if (HasCommandLineSelection)
        {
            var (start, end) = GetCommandLineSelectionRange();
            CommandLineText = current[..start] + current[end..];
            CommandLineCaretIndex = start;
            _commandLineSelectionAnchor = start;
        }
        else if (direction < 0)
        {
            if (caret == 0)
                return;
            CommandLineText = current.Remove(caret - 1, 1);
            CommandLineCaretIndex = caret - 1;
            _commandLineSelectionAnchor = CommandLineCaretIndex;
        }
        else
        {
            if (caret >= current.Length)
                return;
            CommandLineText = current.Remove(caret, 1);
            _commandLineSelectionAnchor = caret;
        }

        EnsureCommandLineCaretVisible();
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyCommandLineDraftChanged();
    }

    private (int Start, int End) GetCommandLineSelectionRange()
    {
        var len = (CommandLineText ?? "").Length;
        var caret = Math.Clamp(CommandLineCaretIndex, 0, len);
        var anchor = Math.Clamp(_commandLineSelectionAnchor, 0, len);
        return caret < anchor ? (caret, anchor) : (anchor, caret);
    }

    private bool HasCommandLineSelection => GetCommandLineSelectionRange().Start != GetCommandLineSelectionRange().End;

    internal void CollapseCommandLineSelection()
    {
        var len = (CommandLineText ?? "").Length;
        _commandLineSelectionAnchor = Math.Clamp(CommandLineCaretIndex, 0, len);
    }

    internal void SetCommandLineSelectionAnchor(int anchor)
    {
        var len = (CommandLineText ?? "").Length;
        _commandLineSelectionAnchor = Math.Clamp(anchor, 0, len);
    }

    private void MoveCommandLineCaret(int delta, bool extendSelection)
    {
        var len = (CommandLineText ?? "").Length;
        var next = Math.Clamp(CommandLineCaretIndex + delta, 0, len);
        _commandLineExtendSelection = extendSelection;
        if (!extendSelection)
            _commandLineSelectionAnchor = next;

        CommandLineCaretIndex = next;
        EnsureCommandLineCaretVisible();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyCommandLineDraftChanged();
    }

    private void MoveCommandLineCaretTo(int index, bool extendSelection)
    {
        var len = (CommandLineText ?? "").Length;
        index = Math.Clamp(index, 0, len);
        _commandLineExtendSelection = extendSelection;
        if (!extendSelection)
            _commandLineSelectionAnchor = index;

        CommandLineCaretIndex = index;
        EnsureCommandLineCaretVisible();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyCommandLineDraftChanged();
    }

    private bool TryHandleCommandLineClipboardKey(KeyEventArgs e)
    {
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (!ctrl)
            return false;

        if (e.Key == Key.A)
        {
            _commandLineSelectionAnchor = 0;
            CommandLineCaretIndex = (CommandLineText ?? "").Length;
            ShowComposerCaretSolid();
            e.Handled = true;
            return true;
        }

        if (e.Key is Key.C or Key.X)
        {
            if (!HasCommandLineSelection)
                return false;

            var (start, end) = GetCommandLineSelectionRange();
            var slice = (CommandLineText ?? "")[start..end];
            _ = SetClipboardTextAsync(slice);
            if (e.Key == Key.X)
            {
                CommandLineText = (CommandLineText ?? "")[..start] + (CommandLineText ?? "")[end..];
                CommandLineCaretIndex = start;
                _commandLineSelectionAnchor = start;
                _textInputClient?.NotifyTextChanged();
                NotifyCommandLineDraftChanged();
            }

            e.Handled = true;
            return true;
        }

        if (e.Key == Key.V)
        {
            _ = PasteCommandLineFromClipboardAsync();
            e.Handled = true;
            return true;
        }

        return false;
    }

    private async Task PasteCommandLineFromClipboardAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;

        var pasted = await clipboard.TryGetTextAsync();
        if (string.IsNullOrEmpty(pasted))
            return;

        InsertCommandLineText(pasted);
    }
}
