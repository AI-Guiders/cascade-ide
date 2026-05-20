#nullable enable
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
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
            defaultValue: "Сообщение, /команда или [M:Method]…");

    public static readonly StyledProperty<bool> IsSlashAutocompleteVisibleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(IsSlashAutocompleteVisible));

    public static readonly StyledProperty<int> SelectedSlashSuggestionIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedSlashSuggestionIndex), -1);

    public static readonly StyledProperty<IEnumerable<ChatSlashSuggestionItem>?> SlashSuggestionsProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, IEnumerable<ChatSlashSuggestionItem>?>(nameof(SlashSuggestions));

    private IntercomSkiaTextInputClient? _textInputClient;
    private DispatcherTimer? _composerCaretBlinkTimer;
    private bool _composerCaretBlinkVisible = true;
    private SKRect _sendButtonBounds;
    private SKRect _slashPopupBounds;
    private SKRect _composerBounds;
    private readonly List<SkiaPopupListRow> _slashRows = [];

    public event EventHandler? SendRequested;
    public event EventHandler<IntercomComposerKeyEventArgs>? ComposerKeyDown;
    public event EventHandler<int>? ThinkingToggleRequested;
    public event EventHandler<IntercomAttachmentRevealEventArgs>? AttachmentRevealRequested;

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
        GotFocus += OnComposerFocusChanged;
        LostFocus += OnComposerFocusChanged;
    }

    private void OnComposerFocusChanged(object? sender, RoutedEventArgs e)
    {
        if (IsKeyboardFocusWithin)
            StartComposerCaretBlink();
        else
            StopComposerCaretBlink();
    }

    private void StartComposerCaretBlink()
    {
        _composerCaretBlinkVisible = true;
        if (_composerCaretBlinkTimer is null)
        {
            _composerCaretBlinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
            _composerCaretBlinkTimer.Tick += OnComposerCaretBlinkTick;
        }

        if (!_composerCaretBlinkTimer.IsEnabled)
            _composerCaretBlinkTimer.Start();
        InvalidateVisual();
    }

    private void StopComposerCaretBlink()
    {
        if (_composerCaretBlinkTimer is not null)
            _composerCaretBlinkTimer.Stop();
        _composerCaretBlinkVisible = false;
        InvalidateVisual();
    }

    private void ShowComposerCaretSolid()
    {
        if (!IsKeyboardFocusWithin)
            return;
        _composerCaretBlinkVisible = true;
        InvalidateVisual();
    }

    private void OnComposerCaretBlinkTick(object? sender, EventArgs e)
    {
        if (!ShowIntercomComposer || !IsComposerEnabled || !IsKeyboardFocusWithin)
        {
            StopComposerCaretBlink();
            return;
        }

        _composerCaretBlinkVisible = !_composerCaretBlinkVisible;
        InvalidateVisual();
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
        if (_composerBounds.Width > 0
            && SkiaComposerStrip.TryGetCaretRect(
                _composerBounds,
                ComposerText ?? "",
                ComposerPreeditText,
                ComposerCaretIndex,
                out var caret))
            return new Rect(caret.Left, caret.Top, Math.Max(2, caret.Width), caret.Height);

        var textLeft = _composerBounds.Width > 0
            ? _composerBounds.Left + SkiaComposerStrip.HorizontalPadding
            : Bounds.Width * 0.5f;
        var textTop = _composerBounds.Width > 0
            ? _composerBounds.Top + SkiaComposerStrip.VerticalPadding + 2f
            : Bounds.Height - 24;
        return new Rect(textLeft, textTop, 2, SkiaComposerStrip.LineHeight - 4f);
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
        SkiaChatTheme theme,
        float layoutScale)
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
        var showCaret = IsComposerEnabled && IsKeyboardFocusWithin;
        SkiaComposerStrip.Draw(
            canvas,
            _composerBounds,
            theme,
            ComposerText ?? "",
            ComposerPreeditText,
            ComposerPlaceholder,
            IsComposerEnabled,
            ComposerCaretIndex,
            showCaret,
            showCaret && _composerCaretBlinkVisible,
            out _sendButtonBounds,
            out _);

        if (popupHeight > 0)
        {
            var popupTop = composerTop - popupHeight;
            _slashPopupBounds = new SKRect(8f, popupTop, width - 8f, composerTop - 2f);
            SkiaPopupList.Draw(canvas, _slashPopupBounds, theme, _slashRows, SelectedSlashSuggestionIndex, layoutScale);
        }
        else
            _slashPopupBounds = default;
    }

    private bool TryHandleIntercomPointer(Point point)
    {
        if (!ShowIntercomComposer)
            return false;

        if (_sendButtonBounds.Width > 0 && _sendButtonBounds.Contains((float)point.X, (float)point.Y))
        {
            Focus();
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
        {
            Focus();
            return true;
        }

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
        if (!ShowIntercomComposer || !IsComposerEnabled)
            return;

        var kind = MapComposerKey(e);
        var popupActive = IsSlashAutocompleteVisible && _slashRows.Count > 0;
        if (kind is IntercomComposerKeyKind.SlashUp or IntercomComposerKeyKind.SlashDown)
        {
            if (!popupActive)
                return;

            if (!IsKeyboardFocusWithin)
                Focus();

            ComposerKeyDown?.Invoke(this, new IntercomComposerKeyEventArgs(kind.Value, e));
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (!IsKeyboardFocusWithin)
            return;

        if (kind is IntercomComposerKeyKind.Tab
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
        if (!IsKeyboardFocusWithin)
            Focus();

        ComposerPreeditText = null;
        var current = ComposerText ?? "";
        var caret = Math.Clamp(ComposerCaretIndex, 0, current.Length);
        ComposerText = current.Insert(caret, text);
        ComposerCaretIndex = caret + text.Length;
        _textInputClient?.NotifyTextChanged();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
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
        ShowComposerCaretSolid();
    }

    private void MoveCaret(int delta)
    {
        var len = (ComposerText ?? "").Length;
        ComposerCaretIndex = Math.Clamp(ComposerCaretIndex + delta, 0, len);
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
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
