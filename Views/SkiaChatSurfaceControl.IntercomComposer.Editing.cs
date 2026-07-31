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
    private void EnsureComposerCaretVisible()
    {
        if (_composerBounds.Width <= 0)
            return;

        var composerPt = IntercomFonts.ResolveComposerPt(FeedUsesForwardMetrics);
        var composerLine = IntercomFonts.ResolveComposerLineHeight(FeedUsesForwardMetrics);
        var composerPreviewPt = IntercomFonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics);
        var previewReserve = SkiaComposerStrip.SlashPreviewReserve(ComposerPreview, composerPreviewPt);
        var sendLeft = _composerBounds.Right - SkiaComposerStrip.HorizontalPadding - SkiaComposerStrip.SendButtonWidth;
        var innerH = Math.Max(1f, _composerBounds.Height - SkiaComposerStrip.VerticalPadding * 2 - previewReserve);
        var contentWidth = Math.Max(40f, sendLeft - 8f - (_composerBounds.Left + SkiaComposerStrip.HorizontalPadding));
        var maxScroll = SkiaComposerStrip.MaxContentScrollOffset(
            ComposerText ?? "",
            ComposerPreeditText,
            contentWidth,
            innerH,
            composerPt,
            composerLine);

        if (!SkiaComposerStrip.TryGetCaretRect(
                _composerBounds,
                ComposerText ?? "",
                ComposerPreeditText,
                ComposerCaretIndex,
                composerPt,
                composerLine,
                out var caret,
                _composerScrollOffsetY,
                ComposerPreview,
                composerPreviewPt,
                ComposerPreviewKind))
            return;

        var viewTop = _composerBounds.Top + SkiaComposerStrip.VerticalPadding;
        var viewBottom = _composerBounds.Bottom - SkiaComposerStrip.VerticalPadding - previewReserve;
        if (caret.Top < viewTop)
            _composerScrollOffsetY = Math.Max(0, _composerScrollOffsetY - (viewTop - caret.Top));
        else if (caret.Bottom > viewBottom)
            _composerScrollOffsetY = Math.Min(maxScroll, _composerScrollOffsetY + (caret.Bottom - viewBottom));

        _composerScrollOffsetY = Math.Clamp(_composerScrollOffsetY, 0f, maxScroll);
    }

    private string GetComposerDisplayText() =>
        string.IsNullOrEmpty(ComposerPreeditText)
            ? ComposerText ?? ""
            : (ComposerText ?? "") + ComposerPreeditText;

    private (int Start, int End) GetComposerSelectionRange()
    {
        var caret = Math.Clamp(ComposerCaretIndex, 0, GetComposerDisplayText().Length);
        var anchor = Math.Clamp(_composerSelectionAnchor, 0, GetComposerDisplayText().Length);
        return caret < anchor ? (caret, anchor) : (anchor, caret);
    }

    private bool HasComposerSelection => GetComposerSelectionRange().Start != GetComposerSelectionRange().End;

    internal void CollapseComposerSelection()
    {
        var len = GetComposerDisplayText().Length;
        _composerSelectionAnchor = Math.Clamp(ComposerCaretIndex, 0, len);
    }

    internal void SetComposerSelectionAnchor(int anchor)
    {
        var len = GetComposerDisplayText().Length;
        _composerSelectionAnchor = Math.Clamp(anchor, 0, len);
    }

    private bool TryHandleComposerClipboardKey(KeyEventArgs e)
    {
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (!ctrl)
            return false;

        if (e.Key == Key.A)
        {
            _composerSelectionAnchor = 0;
            ComposerCaretIndex = (ComposerText ?? "").Length;
            ShowComposerCaretSolid();
            e.Handled = true;
            return true;
        }

        if (e.Key is Key.C or Key.X)
        {
            if (!HasComposerSelection)
                return false;

            var (start, end) = GetComposerSelectionRange();
            var slice = (ComposerText ?? "")[start..end];
            _ = SetClipboardTextAsync(slice);
            if (e.Key == Key.X)
            {
                ComposerPreeditText = null;
                ComposerText = (ComposerText ?? "")[..start] + (ComposerText ?? "")[end..];
                ComposerCaretIndex = start;
                _composerSelectionAnchor = start;
                _textInputClient?.NotifyTextChanged();
                NotifyComposerDraftChanged();
            }

            e.Handled = true;
            return true;
        }

        if (e.Key == Key.V)
        {
            _ = PasteComposerFromClipboardAsync();
            e.Handled = true;
            return true;
        }

        return false;
    }

    private async Task SetClipboardTextAsync(string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;

        await clipboard.SetTextAsync(text);
    }

    private async Task PasteComposerFromClipboardAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;

        var pasted = await clipboard.TryGetTextAsync();
        if (string.IsNullOrEmpty(pasted))
            return;

        InsertComposerText(pasted);
    }

    internal bool TryScrollComposer(float deltaY)
    {
        if (_composerBounds.Width <= 0)
            return false;

        var composerPt = IntercomFonts.ResolveComposerPt(FeedUsesForwardMetrics);
        var composerLine = IntercomFonts.ResolveComposerLineHeight(FeedUsesForwardMetrics);
        var sendLeft = _composerBounds.Right - SkiaComposerStrip.HorizontalPadding - SkiaComposerStrip.SendButtonWidth;
        var innerH = Math.Max(1f, _composerBounds.Height - SkiaComposerStrip.VerticalPadding * 2);
        var contentWidth = Math.Max(40f, sendLeft - 8f - (_composerBounds.Left + SkiaComposerStrip.HorizontalPadding));
        var maxScroll = SkiaComposerStrip.MaxContentScrollOffset(
            ComposerText ?? "",
            ComposerPreeditText,
            contentWidth,
            innerH,
            composerPt,
            composerLine);
        if (maxScroll <= 0f)
            return false;

        _composerScrollOffsetY = Math.Clamp(_composerScrollOffsetY - deltaY, 0f, maxScroll);
        InvalidateComposerChrome();
        return true;
    }


    private static bool TryMapComposerTextKey(KeyEventArgs e, out string text)
    {
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
        {
            text = "";
            return false;
        }

        if (e.Key is Key.Oem2 or Key.OemQuestion or Key.Divide)
        {
            text = "/";
            return true;
        }

        text = "";
        return false;
    }

    private void NotifyComposerDraftChanged() => ComposerDraftChanged?.Invoke(this, EventArgs.Empty);

    private void NotifyCommandLineDraftChanged() => CommandLineDraftChanged?.Invoke(this, EventArgs.Empty);

    internal void NotifyComposerImeStateChanged()
    {
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        InvalidateComposerChrome();
    }

    internal void NotifyCommandLineImeStateChanged()
    {
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        InvalidateComposerChrome();
    }

    private static IntercomComposerKeyKind? MapComposerKey(KeyEventArgs e) => e.Key switch
    {
        Key.Tab => IntercomComposerKeyKind.Tab,
        Key.Up => IntercomComposerKeyKind.SlashUp,
        Key.Down => IntercomComposerKeyKind.SlashDown,
        Key.Escape => IntercomComposerKeyKind.Escape,
        Key.Enter or Key.Return => IntercomComposerKeyKind.Enter,
        _ => null,
    };

    private void InsertComposerText(string text)
    {
        if (!IsKeyboardFocusWithin)
            Focus();

        ClearNavigatorSearchFocus();
        ComposerPreeditText = null;
        var current = ComposerText ?? "";
        var caret = Math.Clamp(ComposerCaretIndex, 0, current.Length);
        if (HasComposerSelection)
        {
            var (start, end) = GetComposerSelectionRange();
            current = current[..start] + current[end..];
            caret = start;
            _composerSelectionAnchor = start;
        }

        var newText = current.Insert(caret, text);
        var newCaret = caret + text.Length;
        ComposerCaretIndex = newCaret;
        _composerSelectionAnchor = newCaret;
        ComposerText = newText;
        EnsureComposerCaretVisible();
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyComposerDraftChanged();
    }

    private void DeleteComposer(int direction)
    {
        ComposerPreeditText = null;
        var current = ComposerText ?? "";
        if (HasComposerSelection)
        {
            var (start, end) = GetComposerSelectionRange();
            ComposerText = current[..start] + current[end..];
            ComposerCaretIndex = start;
            _composerSelectionAnchor = start;
            _textInputClient?.NotifyTextChanged();
            ShowComposerCaretSolid();
            NotifyComposerDraftChanged();
            return;
        }

        var caret = Math.Clamp(ComposerCaretIndex, 0, current.Length);
        if (direction < 0)
        {
            if (caret == 0)
                return;
            ComposerCaretIndex = caret - 1;
            _composerSelectionAnchor = caret - 1;
            ComposerText = current.Remove(caret - 1, 1);
        }
        else
        {
            if (caret >= current.Length)
                return;
            ComposerText = current.Remove(caret, 1);
        }

        _textInputClient?.NotifyTextChanged();
        ShowComposerCaretSolid();
        NotifyComposerDraftChanged();
    }

    private void MoveCaret(int delta, bool extendSelection)
    {
        var len = (ComposerText ?? "").Length;
        var next = Math.Clamp(ComposerCaretIndex + delta, 0, len);
        _composerExtendSelection = extendSelection;
        if (!extendSelection)
            _composerSelectionAnchor = next;

        ComposerCaretIndex = next;
        EnsureComposerCaretVisible();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
    }

    private void MoveCaretTo(int index, bool extendSelection)
    {
        var len = (ComposerText ?? "").Length;
        index = Math.Clamp(index, 0, len);
        _composerExtendSelection = extendSelection;
        if (!extendSelection)
            _composerSelectionAnchor = index;

        ComposerCaretIndex = index;
        EnsureComposerCaretVisible();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
    }
}
