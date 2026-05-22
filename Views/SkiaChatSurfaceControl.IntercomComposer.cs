#nullable enable
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
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

    public static readonly StyledProperty<string?> SlashAutocompletePathPrefixProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string?>(nameof(SlashAutocompletePathPrefix));

    public static readonly StyledProperty<string?> SlashAutocompleteNextStepProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string?>(nameof(SlashAutocompleteNextStep));

    public static readonly StyledProperty<string?> SlashAutocompleteBreadcrumbProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string?>(nameof(SlashAutocompleteBreadcrumb));

    public static readonly StyledProperty<bool> ShowCockpitCommandLineProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(ShowCockpitCommandLine));

    public static readonly StyledProperty<string> CommandLineTextProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(CommandLineText), defaultValue: "/");

    public static readonly StyledProperty<string?> CommandLinePreviewProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string?>(nameof(CommandLinePreview));

    public static readonly StyledProperty<int> CommandLineCaretIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(CommandLineCaretIndex), defaultValue: 0);

    private IntercomSkiaTextInputClient? _textInputClient;
    private DispatcherTimer? _composerCaretBlinkTimer;
    private bool _composerCaretBlinkVisible = true;
    private SKRect _sendButtonBounds;
    private SKRect _deckBounds;
    private SKRect _slashPopupBounds;
    private SKRect _composerBounds;
    private SKRect _commandLineBounds;
    private bool _commandLineFocused;
    private readonly List<SkiaPopupListRow> _slashRows = [];
    private int _slashPopupScrollOffset;
    private int _slashPopupLastRowCount;
    private int _composerSelectionAnchor;
    private float _composerScrollOffsetY;

    public event EventHandler? SendRequested;
    public event EventHandler<IntercomComposerKeyEventArgs>? ComposerKeyDown;
    public event EventHandler<int>? ThinkingToggleRequested;
    public event EventHandler<IntercomAttachmentRevealEventArgs>? AttachmentRevealRequested;
    public event EventHandler<int>? MessageSelectContextRequested;
    public event EventHandler? TopicCreateRequested;
    public event EventHandler? TopicNavigatorToggleRequested;

    /// <summary>Переименовать тему (ПКМ / двойной клик / F2 в Nav или на вкладке).</summary>
    public event EventHandler<TopicRenameRequestEventArgs>? TopicRenameRequested;

    /// <summary>Текст/caret composer изменены (до синхронизации биндинга с VM).</summary>
    public event EventHandler? ComposerDraftChanged;

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

    internal int ComposerSelectionAnchor => _composerSelectionAnchor;

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

    public string? SlashAutocompletePathPrefix
    {
        get => GetValue(SlashAutocompletePathPrefixProperty);
        set => SetValue(SlashAutocompletePathPrefixProperty, value);
    }

    public string? SlashAutocompleteNextStep
    {
        get => GetValue(SlashAutocompleteNextStepProperty);
        set => SetValue(SlashAutocompleteNextStepProperty, value);
    }

    public string? SlashAutocompleteBreadcrumb
    {
        get => GetValue(SlashAutocompleteBreadcrumbProperty);
        set => SetValue(SlashAutocompleteBreadcrumbProperty, value);
    }

    private bool ShowSlashHierarchyHeader =>
        !string.IsNullOrWhiteSpace(SlashAutocompletePathPrefix)
        || !string.IsNullOrWhiteSpace(SlashAutocompleteNextStep)
        || !string.IsNullOrWhiteSpace(SlashAutocompleteBreadcrumb);

    public bool ShowCockpitCommandLine
    {
        get => GetValue(ShowCockpitCommandLineProperty);
        set => SetValue(ShowCockpitCommandLineProperty, value);
    }

    public string CommandLineText
    {
        get => GetValue(CommandLineTextProperty);
        set => SetValue(CommandLineTextProperty, value);
    }

    public string? CommandLinePreview
    {
        get => GetValue(CommandLinePreviewProperty);
        set => SetValue(CommandLinePreviewProperty, value);
    }

    public int CommandLineCaretIndex
    {
        get => GetValue(CommandLineCaretIndexProperty);
        set => SetValue(CommandLineCaretIndexProperty, value);
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
        InvalidateComposerChrome();
    }

    private void StopComposerCaretBlink()
    {
        if (_composerCaretBlinkTimer is not null)
            _composerCaretBlinkTimer.Stop();
        _composerCaretBlinkVisible = false;
        InvalidateComposerChrome();
    }

    private void ShowComposerCaretSolid()
    {
        if (!IsKeyboardFocusWithin)
            return;
        _composerCaretBlinkVisible = true;
        InvalidateComposerChrome();
    }

    private void OnComposerCaretBlinkTick(object? sender, EventArgs e)
    {
        if (!IsKeyboardFocusWithin)
        {
            StopComposerCaretBlink();
            return;
        }

        if (!_navigatorSearchFocused && (!ShowIntercomComposer || !IsComposerEnabled))
        {
            StopComposerCaretBlink();
            return;
        }

        _composerCaretBlinkVisible = !_composerCaretBlinkVisible;
        InvalidateComposerChrome();
    }

    internal void InvalidateComposerChrome()
    {
        _chromeOnlyInvalidation = true;
        InvalidateVisual();
    }

    private void OnTextInputMethodClientRequested(object? sender, TextInputMethodClientRequestedEventArgs e)
    {
        if (!ShowIntercomComposer && !_navigatorSearchFocused)
            return;

        _textInputClient ??= new IntercomSkiaTextInputClient(this);
        e.Client = _textInputClient;
    }

    internal Rect GetComposerCaretScreenRect()
    {
        var composerPt = IntercomFonts.ResolveComposerPt(FeedUsesForwardMetrics);
        var composerLine = IntercomFonts.ResolveComposerLineHeight(FeedUsesForwardMetrics);
        if (_composerBounds.Width > 0
            && SkiaComposerStrip.TryGetCaretRect(
                _composerBounds,
                ComposerText ?? "",
                ComposerPreeditText,
                ComposerCaretIndex,
                composerPt,
                composerLine,
                out var caret,
                _composerScrollOffsetY))
            return new Rect(caret.Left, caret.Top, Math.Max(2, caret.Width), caret.Height);

        var textLeft = _composerBounds.Width > 0
            ? _composerBounds.Left + SkiaComposerStrip.HorizontalPadding
            : Bounds.Width * 0.5f;
        var textTop = _composerBounds.Width > 0
            ? _composerBounds.Top + SkiaComposerStrip.VerticalPadding + 2f
            : Bounds.Height - 24;
        return new Rect(textLeft, textTop, 2, composerLine - 4f);
    }

    private float ResolveBottomChromeHeight(float width)
    {
        if (!ShowIntercomComposer)
            return 0f;

        RebuildSlashRows();
        var fonts = IntercomFonts;
        return SkiaIntercomCommandDeckLayout.MeasureTotalHeight(
            width,
            showComposer: true,
            showCommandLine: ShowCockpitCommandLine,
            commandLinePreview: CommandLinePreview,
            composerText: ComposerText ?? "",
            showSlashPopup: IsSlashAutocompleteVisible && _slashRows.Count > 0,
            slashRowCount: _slashRows.Count,
            composerPreeditText: ComposerPreeditText,
            showSlashHierarchyHeader: ShowSlashHierarchyHeader,
            fonts.ResolveComposerPt(FeedUsesForwardMetrics),
            fonts.ResolveComposerLineHeight(FeedUsesForwardMetrics),
            fonts.ResolveCommandLinePt(FeedUsesForwardMetrics),
            fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics));
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
                item.ListTitle,
                item.ListSubtitle));
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
            _deckBounds = default;
            _composerBounds = default;
            _commandLineBounds = default;
            _slashPopupBounds = default;
            _sendButtonBounds = default;
            return;
        }

        RebuildSlashRows();
        var fonts = IntercomFonts;
        var deck = SkiaIntercomCommandDeckLayout.Compute(
            width,
            height,
            showComposer: true,
            showCommandLine: ShowCockpitCommandLine,
            commandLinePreview: CommandLinePreview,
            composerText: ComposerText ?? "",
            showSlashPopup: IsSlashAutocompleteVisible && _slashRows.Count > 0,
            slashRowCount: _slashRows.Count,
            composerPreeditText: ComposerPreeditText,
            showSlashHierarchyHeader: ShowSlashHierarchyHeader,
            fonts.ResolveComposerPt(FeedUsesForwardMetrics),
            fonts.ResolveComposerLineHeight(FeedUsesForwardMetrics),
            fonts.ResolveCommandLinePt(FeedUsesForwardMetrics),
            fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics));

        _deckBounds = deck.DeckBounds;
        _composerBounds = deck.ComposerBounds;
        _commandLineBounds = deck.CommandLineBounds;
        _slashPopupBounds = deck.SlashPopupBounds;

        if (deck.HasDeck)
            SkiaIntercomCommandDeckLayout.DrawDeckChrome(canvas, _deckBounds, theme);

        if (_slashPopupBounds.Width > 0)
        {
            if (_slashRows.Count != _slashPopupLastRowCount)
            {
                _slashPopupLastRowCount = _slashRows.Count;
                _slashPopupScrollOffset = 0;
            }

            var selected = SelectedSlashSuggestionIndex;
            if (selected < 0 && _slashRows.Count > 0)
                selected = 0;
            _slashPopupScrollOffset = SkiaPopupList.EnsureSelectionVisible(
                selected,
                _slashPopupScrollOffset,
                _slashRows.Count);

            SkiaPopupList.Draw(
                canvas,
                _slashPopupBounds,
                theme,
                _slashRows,
                selected,
                _slashPopupScrollOffset,
                layoutScale,
                SlashAutocompletePathPrefix,
                SlashAutocompleteNextStep,
                SlashAutocompleteBreadcrumb);
        }
        else
        {
            _slashPopupScrollOffset = 0;
            _slashPopupLastRowCount = 0;
        }

        if (_commandLineBounds.Width > 0)
        {
            var cclCaret = _commandLineFocused && IsKeyboardFocusWithin;
            SkiaCommandLineStrip.Draw(
                canvas,
                _commandLineBounds,
                theme,
                CommandLineText ?? "/",
                CommandLinePreview,
                "/intercom … · /anchor peek …",
                IsComposerEnabled,
                CommandLineCaretIndex,
                cclCaret,
                cclCaret && _composerCaretBlinkVisible,
                fonts.ResolveCommandLinePt(FeedUsesForwardMetrics),
                fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics));
        }

        var showCaret = !_commandLineFocused && IsComposerEnabled && IsKeyboardFocusWithin;
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
            fonts.ResolveComposerPt(FeedUsesForwardMetrics),
            fonts.ResolveComposerLineHeight(FeedUsesForwardMetrics),
            out _sendButtonBounds,
            out _,
            _composerSelectionAnchor,
            _composerScrollOffsetY);

        RegisterComposerPointerHits();
    }

    internal bool TryPlaceComposerCaretAtPoint(float x, float y, bool extendSelection)
    {
        if (_composerBounds.Width <= 0 || !IsComposerEnabled)
            return false;

        var composerPt = IntercomFonts.ResolveComposerPt(FeedUsesForwardMetrics);
        var composerLine = IntercomFonts.ResolveComposerLineHeight(FeedUsesForwardMetrics);
        if (!SkiaComposerStrip.TryHitTestCaretAtPoint(
                _composerBounds,
                ComposerText ?? "",
                ComposerPreeditText,
                x,
                y,
                composerPt,
                composerLine,
                _composerScrollOffsetY,
                out var index))
            return false;

        if (!extendSelection)
            _composerSelectionAnchor = index;

        ComposerCaretIndex = index;
        EnsureComposerCaretVisible();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyComposerDraftChanged();
        return true;
    }

    private void EnsureComposerCaretVisible()
    {
        if (_composerBounds.Width <= 0)
            return;

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

        if (!SkiaComposerStrip.TryGetCaretRect(
                _composerBounds,
                ComposerText ?? "",
                ComposerPreeditText,
                ComposerCaretIndex,
                composerPt,
                composerLine,
                out var caret,
                _composerScrollOffsetY))
            return;

        var viewTop = _composerBounds.Top + SkiaComposerStrip.VerticalPadding;
        var viewBottom = _composerBounds.Bottom - SkiaComposerStrip.VerticalPadding;
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

    private void RegisterComposerPointerHits()
    {
        if (_sendButtonBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(_sendButtonBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.ComposerSend));
        }

        if (_slashPopupBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(_slashPopupBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.SlashPopup));
        }

        if (_commandLineBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(_commandLineBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.CommandLineFocus));
        }

        if (_composerBounds.Width > 0)
        {
            _chatHits.RegisterControlRect(
                SkiaChatHitGeometry.ToControlRect(_composerBounds),
                new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.ComposerFocus));
        }
    }

    private void OnComposerTextInput(object? sender, TextInputEventArgs e)
    {
        if (TryHandleNavigatorSearchTextInput(e))
            return;

        if (!ShowIntercomComposer || !IsComposerEnabled || string.IsNullOrEmpty(e.Text))
            return;

        if (ShowCockpitCommandLine && _commandLineFocused)
        {
            InsertCommandLineText(e.Text);
            e.Handled = true;
            return;
        }

        InsertComposerText(e.Text);
        e.Handled = true;
    }

    private void OnComposerKeyDown(object? sender, KeyEventArgs e)
    {
        if (TryHandleNavigatorSearchKeyDown(e))
            return;

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

        if (kind is IntercomComposerKeyKind.Tab
            or IntercomComposerKeyKind.Escape
            or IntercomComposerKeyKind.Enter
            or IntercomComposerKeyKind.CommitSlashSuggestion)
        {
            if (!IsKeyboardFocusWithin)
                Focus();

            ComposerKeyDown?.Invoke(this, new IntercomComposerKeyEventArgs(kind.Value, e));
            e.Handled = true;
            return;
        }

        if (TryMapComposerTextKey(e, out var textKey))
        {
            if (!IsKeyboardFocusWithin)
                Focus();

            if (ShowCockpitCommandLine && _commandLineFocused)
                InsertCommandLineText(textKey);
            else
                InsertComposerText(textKey);

            e.Handled = true;
            return;
        }

        if (!IsKeyboardFocusWithin)
            return;

        if (TryHandleComposerClipboardKey(e))
            return;

        if (ShowCockpitCommandLine && _commandLineFocused)
        {
            if (e.Key == Key.Back)
            {
                DeleteCommandLine(-1);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteCommandLine(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                MoveCommandLineCaret(-1);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                MoveCommandLineCaret(1);
                e.Handled = true;
            }

            return;
        }

        var extendSel = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

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
            MoveCaret(-1, extendSel);
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            MoveCaret(1, extendSel);
            e.Handled = true;
        }
        else if (e.Key == Key.Home)
        {
            MoveCaretTo(0, extendSel);
            e.Handled = true;
        }
        else if (e.Key == Key.End)
        {
            MoveCaretTo((ComposerText ?? "").Length, extendSel);
            e.Handled = true;
        }
    }

    private void InsertCommandLineText(string text)
    {
        if (!IsKeyboardFocusWithin)
            Focus();

        ClearNavigatorSearchFocus();
        _commandLineFocused = true;
        var current = CommandLineText ?? "";
        var caret = Math.Clamp(CommandLineCaretIndex, 0, current.Length);
        CommandLineText = current.Insert(caret, text);
        CommandLineCaretIndex = caret + text.Length;
        ShowComposerCaretSolid();
        InvalidateVisual();
        NotifyComposerDraftChanged();
    }

    private void DeleteCommandLine(int direction)
    {
        var current = CommandLineText ?? "";
        var caret = Math.Clamp(CommandLineCaretIndex, 0, current.Length);
        if (direction < 0)
        {
            if (caret == 0)
                return;
            CommandLineText = current.Remove(caret - 1, 1);
            CommandLineCaretIndex = caret - 1;
        }
        else
        {
            if (caret >= current.Length)
                return;
            CommandLineText = current.Remove(caret, 1);
            CommandLineCaretIndex = caret;
        }

        ShowComposerCaretSolid();
        InvalidateVisual();
    }

    private void MoveCommandLineCaret(int delta)
    {
        var len = (CommandLineText ?? "").Length;
        CommandLineCaretIndex = Math.Clamp(CommandLineCaretIndex + delta, 0, len);
        ShowComposerCaretSolid();
        InvalidateVisual();
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
        if (!extendSelection)
            _composerSelectionAnchor = index;

        ComposerCaretIndex = index;
        EnsureComposerCaretVisible();
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
