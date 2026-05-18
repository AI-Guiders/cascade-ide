#nullable enable
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views;

public partial class SkiaChatSurfaceControl
{
    public static readonly StyledProperty<bool> ShowIntercomComposerProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(ShowIntercomComposer));

    public static readonly StyledProperty<string> ComposerTextProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(ComposerText), defaultValue: "");

    public static readonly StyledProperty<int> ComposerCaretIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(ComposerCaretIndex), defaultValue: 0);

    public static readonly StyledProperty<string?> ComposerPreeditTextProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string?>(nameof(ComposerPreeditText));

    public static readonly StyledProperty<bool> IsComposerEnabledProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(IsComposerEnabled), defaultValue: true);

    public static readonly StyledProperty<string> ComposerPlaceholderProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(
            nameof(ComposerPlaceholder),
            defaultValue: "Сообщение или /команда…");

    public static readonly StyledProperty<bool> IsSlashAutocompleteVisibleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(IsSlashAutocompleteVisible));

    public static readonly StyledProperty<int> SelectedSlashSuggestionIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedSlashSuggestionIndex), -1);

    public static readonly StyledProperty<IEnumerable<ChatSlashSuggestionItem>?> SlashSuggestionsProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, IEnumerable<ChatSlashSuggestionItem>?>(nameof(SlashSuggestions));

    private IntercomSkiaTextInputClient? _textInputClient;
    private SKRect _sendButtonBounds;
    private SKRect _slashPopupBounds;
    private SKRect _composerBounds;
    private readonly List<SkiaPopupListRow> _slashRows = [];

    public event EventHandler? SendRequested;
    public event EventHandler<IntercomComposerKeyEventArgs>? ComposerKeyDown;
    public event EventHandler<int>? ThinkingToggleRequested;

    public bool ShowIntercomComposer
    {
        get => GetValue(ShowIntercomComposerProperty);
        set => SetValue(ShowIntercomComposerProperty, value);
    }

    public string ComposerText
    {
        get => GetValue(ComposerTextProperty);
        set => SetValue(ComposerTextProperty, value);
    }

    public int ComposerCaretIndex
    {
        get => GetValue(ComposerCaretIndexProperty);
        set => SetValue(ComposerCaretIndexProperty, value);
    }

    public string? ComposerPreeditText
    {
        get => GetValue(ComposerPreeditTextProperty);
        set => SetValue(ComposerPreeditTextProperty, value);
    }

    public bool IsComposerEnabled
    {
        get => GetValue(IsComposerEnabledProperty);
        set => SetValue(IsComposerEnabledProperty, value);
    }

    public string ComposerPlaceholder
    {
        get => GetValue(ComposerPlaceholderProperty);
        set => SetValue(ComposerPlaceholderProperty, value);
    }

    public bool IsSlashAutocompleteVisible
    {
        get => GetValue(IsSlashAutocompleteVisibleProperty);
        set => SetValue(IsSlashAutocompleteVisibleProperty, value);
    }

    public int SelectedSlashSuggestionIndex
    {
        get => GetValue(SelectedSlashSuggestionIndexProperty);
        set => SetValue(SelectedSlashSuggestionIndexProperty, value);
    }

    public IEnumerable<ChatSlashSuggestionItem>? SlashSuggestions
    {
        get => GetValue(SlashSuggestionsProperty);
        set => SetValue(SlashSuggestionsProperty, value);
    }

    private void InitializeIntercomComposer()
    {
        Focusable = true;
        IsTabStop = true;
        InputMethod.SetIsInputMethodEnabled(this, true);
        TextInputMethodClientRequested += OnTextInputMethodClientRequested;
        AddHandler(KeyDownEvent, OnComposerKeyDown, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, OnComposerTextInput, RoutingStrategies.Tunnel);
    }

    private void OnTextInputMethodClientRequested(object? sender, TextInputMethodClientRequestedEventArgs e)
    {
        if (!ShowIntercomComposer)
            return;

        _textInputClient ??= new IntercomSkiaTextInputClient(this);
        e.Client = _textInputClient;
    }

    internal Rect GetComposerCaretScreenRect()
    {
        if (_composerBounds.Width <= 0)
            return new Rect(Bounds.Width * 0.5, Bounds.Height - 24, 1, 18);

        var textLeft = _composerBounds.Left + SkiaComposerStrip.HorizontalPadding;
        var textTop = _composerBounds.Top + SkiaComposerStrip.VerticalPadding + 12;
        return new Rect(textLeft, textTop, 1, 18);
    }

    private float ResolveBottomChromeHeight(float width)
    {
        if (!ShowIntercomComposer)
            return 0f;

        RebuildSlashRows();
        return SkiaIntercomComposerLayout.MeasureBottomChromeHeight(
            showComposer: true,
            showSlashPopup: IsSlashAutocompleteVisible && _slashRows.Count > 0,
            slashRowCount: _slashRows.Count,
            composerText: ComposerText ?? "",
            surfaceWidth: width);
    }

    private void RebuildSlashRows()
    {
        _slashRows.Clear();
        if (SlashSuggestions is null)
            return;

        foreach (var item in SlashSuggestions)
        {
            _slashRows.Add(new SkiaPopupListRow(
                item.Group,
                item.SlashPath,
                item.Help));
        }
    }

    private void DrawIntercomBottomChrome(
        SKCanvas canvas,
        float width,
        float height,
        SkiaChatTheme theme)
    {
        if (!ShowIntercomComposer)
        {
            _composerBounds = default;
            _slashPopupBounds = default;
            _sendButtonBounds = default;
            return;
        }

        RebuildSlashRows();
        var contentWidth = Math.Max(40f, width - SkiaComposerStrip.HorizontalPadding * 2 - SkiaComposerStrip.SendButtonWidth - 24f);
        var composerHeight = SkiaComposerStrip.MeasureHeight(ComposerText ?? "", ComposerPreeditText, contentWidth);
        var popupHeight = IsSlashAutocompleteVisible && _slashRows.Count > 0
            ? SkiaPopupList.MeasureHeight(_slashRows.Count) + 4f
            : 0f;
        var bottom = height;
        var composerTop = bottom - composerHeight;

        _composerBounds = new SKRect(0, composerTop, width, bottom);
        SkiaComposerStrip.Draw(
            canvas,
            _composerBounds,
            theme,
            ComposerText ?? "",
            ComposerPreeditText,
            ComposerPlaceholder,
            IsComposerEnabled,
            ComposerCaretIndex,
            out _sendButtonBounds,
            out _);

        if (popupHeight > 0)
        {
            var popupTop = composerTop - popupHeight;
            _slashPopupBounds = new SKRect(8f, popupTop, width - 8f, composerTop - 2f);
            SkiaPopupList.Draw(canvas, _slashPopupBounds, theme, _slashRows, SelectedSlashSuggestionIndex);
        }
        else
            _slashPopupBounds = default;
    }

    private bool TryHandleIntercomPointer(Point point)
    {
        if (!ShowIntercomComposer)
            return false;

        Focus();

        if (_sendButtonBounds.Width > 0 && _sendButtonBounds.Contains((float)point.X, (float)point.Y))
        {
            if (IsComposerEnabled)
                SendRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (_slashPopupBounds.Width > 0)
        {
            var row = SkiaPopupList.HitTestRow(_slashPopupBounds, (float)point.X, (float)point.Y, _slashRows.Count);
            if (row >= 0)
            {
                SelectedSlashSuggestionIndex = row;
                ComposerKeyDown?.Invoke(this, new IntercomComposerKeyEventArgs(IntercomComposerKeyKind.CommitSlashSuggestion));
                return true;
            }
        }

        if (_composerBounds.Contains((float)point.X, (float)point.Y))
            return true;

        return false;
    }

    private void OnComposerTextInput(object? sender, TextInputEventArgs e)
    {
        if (!ShowIntercomComposer || !IsComposerEnabled || string.IsNullOrEmpty(e.Text))
            return;

        InsertComposerText(e.Text);
        e.Handled = true;
    }

    private void OnComposerKeyDown(object? sender, KeyEventArgs e)
    {
        if (!ShowIntercomComposer || !IsComposerEnabled || !IsFocused)
            return;

        var kind = MapComposerKey(e);
        if (kind is IntercomComposerKeyKind.Tab
            or IntercomComposerKeyKind.SlashUp
            or IntercomComposerKeyKind.SlashDown
            or IntercomComposerKeyKind.Escape
            or IntercomComposerKeyKind.Enter
            or IntercomComposerKeyKind.CommitSlashSuggestion)
        {
            ComposerKeyDown?.Invoke(this, new IntercomComposerKeyEventArgs(kind.Value, e));
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Back)
        {
            DeleteComposer(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            DeleteComposer(1);
            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            MoveCaret(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            MoveCaret(1);
            e.Handled = true;
        }
    }

    private static IntercomComposerKeyKind? MapComposerKey(KeyEventArgs e) => e.Key switch
    {
        Key.Tab => IntercomComposerKeyKind.Tab,
        Key.Up => IntercomComposerKeyKind.SlashUp,
        Key.Down => IntercomComposerKeyKind.SlashDown,
        Key.Escape => IntercomComposerKeyKind.Escape,
        Key.Enter => IntercomComposerKeyKind.Enter,
        _ => null,
    };

    private void InsertComposerText(string text)
    {
        ComposerPreeditText = null;
        var current = ComposerText ?? "";
        var caret = Math.Clamp(ComposerCaretIndex, 0, current.Length);
        ComposerText = current.Insert(caret, text);
        ComposerCaretIndex = caret + text.Length;
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        InvalidateVisual();
    }

    private void DeleteComposer(int direction)
    {
        ComposerPreeditText = null;
        var current = ComposerText ?? "";
        var caret = Math.Clamp(ComposerCaretIndex, 0, current.Length);
        if (direction < 0)
        {
            if (caret == 0)
                return;
            ComposerText = current.Remove(caret - 1, 1);
            ComposerCaretIndex = caret - 1;
        }
        else
        {
            if (caret >= current.Length)
                return;
            ComposerText = current.Remove(caret, 1);
            ComposerCaretIndex = caret;
        }

        _textInputClient?.NotifyTextChanged();
        InvalidateVisual();
    }

    private void MoveCaret(int delta)
    {
        var len = (ComposerText ?? "").Length;
        ComposerCaretIndex = Math.Clamp(ComposerCaretIndex + delta, 0, len);
        _textInputClient?.NotifyCursorMoved();
        InvalidateVisual();
    }

}

public enum IntercomComposerKeyKind
{
    Tab,
    SlashUp,
    SlashDown,
    Escape,
    Enter,
    CommitSlashSuggestion,
    InsertNewLine,
    Backspace,
    DeleteForward,
    MoveCaretLeft,
    MoveCaretRight,
}

public sealed class IntercomComposerKeyEventArgs(IntercomComposerKeyKind kind, KeyEventArgs? keyEvent = null) : EventArgs
{
    public IntercomComposerKeyKind Kind { get; } = kind;

    public KeyEventArgs? KeyEvent { get; } = keyEvent;
}
