#nullable enable

using Avalonia;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;

namespace CascadeIDE.Views;

public partial class SkiaChatSurfaceControl
{
    private bool _navigatorSearchFocused;
    private int _navigatorSearchCaretIndex;
    private SKRect _navigatorSearchBounds;

    /// <summary>Активен ввод в поле поиска Topic Navigator (Skia).</summary>
    internal bool IsNavigatorSearchInputActive => _navigatorSearchFocused;

    internal int NavigatorSearchCaretIndex
    {
        get => _navigatorSearchCaretIndex;
        set
        {
            var len = (TopicNavigatorSearchQuery ?? "").Length;
            _navigatorSearchCaretIndex = Math.Clamp(value, 0, len);
        }
    }

    /// <summary>Текст поиска изменён до синхронизации с VM.</summary>
    public event EventHandler? NavigatorSearchDraftChanged;

    internal static void OnTopicNavigatorSearchQueryChanged(
        SkiaChatSurfaceControl control,
        AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is string text)
            control.NavigatorSearchCaretIndex = text.Length;

        control.InvalidateVisual();
    }

    internal Rect GetNavigatorSearchCaretScreenRect()
    {
        if (_navigatorSearchBounds.Width <= 0)
            return default;

        var pt = Math.Max(10f, IntercomFonts.ResolveChromeSubtitlePt());
        using var font = new SKFont(SKTypeface.FromFamilyName(IntercomFonts.ResolveProseFamily()), pt);
        var text = TopicNavigatorSearchQuery ?? "";
        var caret = Math.Clamp(_navigatorSearchCaretIndex, 0, text.Length);
        var prefix = text[..caret];
        var textLeft = _navigatorSearchBounds.Left + 8f;
        var caretX = textLeft + (prefix.Length > 0 ? font.MeasureText(prefix) : 0f);
        var midY = _navigatorSearchBounds.MidY + 4f;
        var lineH = font.Metrics.CapHeight + 4f;
        return new Rect(caretX, midY - lineH * 0.55f, 2, lineH);
    }

    private void FocusNavigatorSearch()
    {
        _navigatorSearchFocused = true;
        _commandLineFocused = false;
        NavigatorSearchCaretIndex = (TopicNavigatorSearchQuery ?? "").Length;
        Focus();
        StartComposerCaretBlink();
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        InvalidateVisual();
    }

    private void ClearNavigatorSearchFocus()
    {
        if (!_navigatorSearchFocused)
            return;

        _navigatorSearchFocused = false;
        InvalidateVisual();
    }

    private void NotifyNavigatorSearchDraftChanged()
    {
        NavigatorSearchDraftChanged?.Invoke(this, EventArgs.Empty);
        InvalidateVisual();
    }

    private void InsertNavigatorSearchText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (!IsKeyboardFocusWithin)
            Focus();

        _navigatorSearchFocused = true;
        _commandLineFocused = false;
        var current = TopicNavigatorSearchQuery ?? "";
        var caret = Math.Clamp(_navigatorSearchCaretIndex, 0, current.Length);
        TopicNavigatorSearchQuery = current.Insert(caret, text);
        NavigatorSearchCaretIndex = caret + text.Length;
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyNavigatorSearchDraftChanged();
    }

    private void DeleteNavigatorSearch(int direction)
    {
        var current = TopicNavigatorSearchQuery ?? "";
        var caret = Math.Clamp(_navigatorSearchCaretIndex, 0, current.Length);
        if (direction < 0)
        {
            if (caret == 0)
                return;

            TopicNavigatorSearchQuery = current.Remove(caret - 1, 1);
            NavigatorSearchCaretIndex = caret - 1;
        }
        else
        {
            if (caret >= current.Length)
                return;

            TopicNavigatorSearchQuery = current.Remove(caret, 1);
            NavigatorSearchCaretIndex = caret;
        }

        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyNavigatorSearchDraftChanged();
    }

    private void MoveNavigatorSearchCaret(int delta)
    {
        NavigatorSearchCaretIndex = _navigatorSearchCaretIndex + delta;
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        InvalidateVisual();
    }

    private bool TryHandleNavigatorSearchKeyDown(KeyEventArgs e)
    {
        if (!_navigatorSearchFocused)
            return false;

        if (!IsKeyboardFocusWithin)
            Focus();

        if (e.Key == Key.Escape)
        {
            ClearNavigatorSearchFocus();
            e.Handled = true;
            return true;
        }

        if (TryMapComposerTextKey(e, out var textKey))
        {
            InsertNavigatorSearchText(textKey);
            e.Handled = true;
            return true;
        }

        switch (e.Key)
        {
            case Key.Back:
                DeleteNavigatorSearch(-1);
                e.Handled = true;
                return true;
            case Key.Delete:
                DeleteNavigatorSearch(1);
                e.Handled = true;
                return true;
            case Key.Left:
                MoveNavigatorSearchCaret(-1);
                e.Handled = true;
                return true;
            case Key.Right:
                MoveNavigatorSearchCaret(1);
                e.Handled = true;
                return true;
            case Key.Home:
                NavigatorSearchCaretIndex = 0;
                ShowComposerCaretSolid();
                e.Handled = true;
                return true;
            case Key.End:
                NavigatorSearchCaretIndex = (TopicNavigatorSearchQuery ?? "").Length;
                ShowComposerCaretSolid();
                e.Handled = true;
                return true;
        }

        return false;
    }

    private bool TryHandleNavigatorSearchTextInput(TextInputEventArgs e)
    {
        if (!_navigatorSearchFocused || string.IsNullOrEmpty(e.Text))
            return false;

        InsertNavigatorSearchText(e.Text);
        e.Handled = true;
        return true;
    }
}
