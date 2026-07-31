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

    public static readonly StyledProperty<SlashCommandPreviewKind> CommandLinePreviewKindProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, SlashCommandPreviewKind>(
            nameof(CommandLinePreviewKind),
            defaultValue: SlashCommandPreviewKind.None);

    public static readonly StyledProperty<int> CommandLineCaretIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(CommandLineCaretIndex), defaultValue: 0);

    public static readonly StyledProperty<string?> ComposerPreviewProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string?>(nameof(ComposerPreview));

    public static readonly StyledProperty<SlashCommandPreviewKind> ComposerPreviewKindProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, SlashCommandPreviewKind>(
            nameof(ComposerPreviewKind),
            defaultValue: SlashCommandPreviewKind.None);

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
    private bool _composerExtendSelection;
    private float _composerScrollOffsetY;
    private int _commandLineSelectionAnchor;
    private bool _commandLineExtendSelection;
    private float _commandLineScrollOffsetX;

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

    /// <summary>Текст/caret CCL изменены (до синхронизации биндинга с VM).</summary>
    public event EventHandler? CommandLineDraftChanged;

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

    internal int CommandLineSelectionAnchor => _commandLineSelectionAnchor;

    internal bool IsCommandLineInputActive => ShowCockpitCommandLine && _commandLineFocused;

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

    public SlashCommandPreviewKind CommandLinePreviewKind
    {
        get => GetValue(CommandLinePreviewKindProperty);
        set => SetValue(CommandLinePreviewKindProperty, value);
    }

    public int CommandLineCaretIndex
    {
        get => GetValue(CommandLineCaretIndexProperty);
        set => SetValue(CommandLineCaretIndexProperty, value);
    }

    public string? ComposerPreview
    {
        get => GetValue(ComposerPreviewProperty);
        set => SetValue(ComposerPreviewProperty, value);
    }

    public SlashCommandPreviewKind ComposerPreviewKind
    {
        get => GetValue(ComposerPreviewKindProperty);
        set => SetValue(ComposerPreviewKindProperty, value);
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

        var activeInput = _navigatorSearchFocused
            || (ShowCockpitCommandLine && _commandLineFocused)
            || (ShowIntercomComposer && IsComposerEnabled && !_commandLineFocused);
        if (!activeInput)
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
        if (!ShowIntercomComposer && !_navigatorSearchFocused && !ShowCockpitCommandLine)
            return;

        _textInputClient ??= new IntercomSkiaTextInputClient(this);
        e.Client = _textInputClient;
    }

    internal Rect GetComposerCaretScreenRect()
    {
        var composerPt = IntercomFonts.ResolveComposerPt(FeedUsesForwardMetrics);
        var composerLine = IntercomFonts.ResolveComposerLineHeight(FeedUsesForwardMetrics);
        var composerPreviewPt = IntercomFonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics);
        if (_composerBounds.Width > 0
            && SkiaComposerStrip.TryGetCaretRect(
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
            composerSlashPreview: ComposerPreview,
            composerSlashPreviewFontSize: fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics),
            commandLineFontSize: fonts.ResolveCommandLinePt(FeedUsesForwardMetrics),
            commandLinePreviewFontSize: fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics));
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


    internal bool TryPlaceComposerCaretAtPoint(float x, float y, bool extendSelection)
    {
        if (_composerBounds.Width <= 0 || !IsComposerEnabled)
            return false;

        var composerPt = IntercomFonts.ResolveComposerPt(FeedUsesForwardMetrics);
        var composerLine = IntercomFonts.ResolveComposerLineHeight(FeedUsesForwardMetrics);
        var composerPreviewPt = IntercomFonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics);
        if (!SkiaComposerStrip.TryHitTestCaretAtPoint(
                _composerBounds,
                ComposerText ?? "",
                ComposerPreeditText,
                x,
                y,
                composerPt,
                composerLine,
                _composerScrollOffsetY,
                ComposerPreview,
                composerPreviewPt,
                out var index,
                ComposerPreviewKind,
                ComposerCaretIndex))
            return false;

        _composerExtendSelection = extendSelection;
        if (!extendSelection)
            _composerSelectionAnchor = index;

        ComposerCaretIndex = index;
        EnsureComposerCaretVisible();
        _textInputClient?.NotifyCursorMoved();
        ShowComposerCaretSolid();
        NotifyComposerDraftChanged();
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

        if (e.Text == " " && IsSlashAutocompleteVisible && _slashRows.Count > 0)
        {
            if (!IsKeyboardFocusWithin)
                Focus();

            ComposerKeyDown?.Invoke(this, new IntercomComposerKeyEventArgs(IntercomComposerKeyKind.CommitSlashSuggestion));
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

        if (ShowCockpitCommandLine && _commandLineFocused)
        {
            if (TryHandleCommandLineClipboardKey(e))
                return;

            var cclExtendSel = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
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
                MoveCommandLineCaret(-1, cclExtendSel);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                MoveCommandLineCaret(1, cclExtendSel);
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                MoveCommandLineCaretTo(0, cclExtendSel);
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                MoveCommandLineCaretTo((CommandLineText ?? "").Length, cclExtendSel);
                e.Handled = true;
            }

            return;
        }

        if (TryHandleComposerClipboardKey(e))
            return;

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


}
