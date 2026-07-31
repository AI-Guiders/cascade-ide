#nullable enable
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views;

/// <summary>
/// Skia-центричный chat surface: overview веток, секции тредов и карточки сообщений/уточнений.
/// </summary>
public partial class SkiaChatSurfaceControl : Control
{
    private const double WheelPixelsPerDelta = 48;

    private readonly SkiaChatHitRegistry _chatHits = new();
    private double _scrollOffset;
    private double _cachedContentHeight;
    private int _hoveredItem = -1;
    private SkiaChatTheme _theme = SkiaChatTheme.DarkFallback;
    private WriteableBitmap? _skiaFrame;
    private int _skiaFrameWidth;
    private int _skiaFrameHeight;
    private float _skiaFrameLayoutScale = 1f;
    private bool _chromeOnlyInvalidation;
    private IReadOnlyList<SkiaChatPlacedEntity>? _feedLayoutCache;
    private string? _feedLayoutCacheKey;

    public static readonly StyledProperty<ChatSurfaceSnapshot> SnapshotProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, ChatSurfaceSnapshot>(nameof(Snapshot), ChatSurfaceSnapshot.Empty);

    public static readonly StyledProperty<int> SelectedMessageIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedMessageIndex), -1);

    public static readonly StyledProperty<Guid> DetailThreadIdProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, Guid>(nameof(DetailThreadId), Guid.Empty);

    public static readonly StyledProperty<bool> OverviewModeProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(OverviewMode), false);

    /// <summary>Forward chrome: toolbar, spine, вкладки (не метрики ленты).</summary>
    public static readonly StyledProperty<bool> ForwardHostProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(ForwardHost), false);

    /// <summary>Compact tier side panel (ADR 0171): feed-first, navigator toggle.</summary>
    public static readonly StyledProperty<bool> CompactSideHostProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(CompactSideHost), false);

    /// <summary>Comfortable feed/composer metrics; false — legacy compact Forward feed (prose_pt_forward + SkiaChatDensity).</summary>
    public static readonly StyledProperty<bool> ComfortableFeedProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(ComfortableFeed), true);

    /// <summary>Topic Navigator (ADR 0127-E): видимая боковая панель (MFD — pinned; Forward — toggle).</summary>
    public static readonly StyledProperty<bool> TopicNavigatorVisibleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(TopicNavigatorVisible), false);

    public static readonly StyledProperty<string> TopicNavigatorSearchQueryProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(TopicNavigatorSearchQuery), "");

    /// <summary>Типографика Skia-ленты из <c>[fonts.intercom]</c>.</summary>
    public static readonly StyledProperty<IntercomFontsSettings> IntercomFontsProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, IntercomFontsSettings>(
            nameof(IntercomFonts),
            IntercomFontDefaults.Intercom);

    public static readonly StyledProperty<string> ChromeTitleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(ChromeTitle), "Intercom");

    public static readonly StyledProperty<string> LoadingStatusTextProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(LoadingStatusText), "");

    public static readonly StyledProperty<string> FmUsageSubtitleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(FmUsageSubtitle), "");

    public static readonly StyledProperty<bool> IsChatLoadingProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(IsChatLoading), false);

    public ChatSurfaceSnapshot Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public int SelectedMessageIndex
    {
        get => GetValue(SelectedMessageIndexProperty);
        set => SetValue(SelectedMessageIndexProperty, value);
    }

    public Guid DetailThreadId
    {
        get => GetValue(DetailThreadIdProperty);
        set => SetValue(DetailThreadIdProperty, value);
    }

    public bool OverviewMode
    {
        get => GetValue(OverviewModeProperty);
        set => SetValue(OverviewModeProperty, value);
    }

    public bool ForwardHost
    {
        get => GetValue(ForwardHostProperty);
        set => SetValue(ForwardHostProperty, value);
    }

    public bool CompactSideHost
    {
        get => GetValue(CompactSideHostProperty);
        set => SetValue(CompactSideHostProperty, value);
    }

    public bool ComfortableFeed
    {
        get => GetValue(ComfortableFeedProperty);
        set => SetValue(ComfortableFeedProperty, value);
    }

    /// <summary>Compact feed metrics (инверсия <see cref="ComfortableFeed"/>).</summary>
    private bool FeedUsesForwardMetrics => ForwardHost && !ComfortableFeed;

    public bool TopicNavigatorVisible
    {
        get => GetValue(TopicNavigatorVisibleProperty);
        set => SetValue(TopicNavigatorVisibleProperty, value);
    }

    public string TopicNavigatorSearchQuery
    {
        get => GetValue(TopicNavigatorSearchQueryProperty);
        set => SetValue(TopicNavigatorSearchQueryProperty, value);
    }

    private float _navigatorScrollOffset;

    public IntercomFontsSettings IntercomFonts
    {
        get => GetValue(IntercomFontsProperty);
        set => SetValue(IntercomFontsProperty, value);
    }

    public string ChromeTitle
    {
        get => GetValue(ChromeTitleProperty);
        set => SetValue(ChromeTitleProperty, value);
    }

    public string LoadingStatusText
    {
        get => GetValue(LoadingStatusTextProperty);
        set => SetValue(LoadingStatusTextProperty, value);
    }

    public string FmUsageSubtitle
    {
        get => GetValue(FmUsageSubtitleProperty);
        set => SetValue(FmUsageSubtitleProperty, value);
    }

    public bool IsChatLoading
    {
        get => GetValue(IsChatLoadingProperty);
        set => SetValue(IsChatLoadingProperty, value);
    }

    static SkiaChatSurfaceControl()
    {
        FocusableProperty.OverrideDefaultValue<SkiaChatSurfaceControl>(true);
        AffectsRender<SkiaChatSurfaceControl>(
            SnapshotProperty,
            SelectedMessageIndexProperty,
            DetailThreadIdProperty,
            OverviewModeProperty,
            ForwardHostProperty,
            CompactSideHostProperty,
            ComfortableFeedProperty,
            TopicNavigatorVisibleProperty,
            TopicNavigatorSearchQueryProperty,
            IntercomFontsProperty,
            ChromeTitleProperty,
            LoadingStatusTextProperty,
            FmUsageSubtitleProperty,
            IsChatLoadingProperty,
            ShowIntercomComposerProperty,
            ComposerTextProperty,
            ComposerCaretIndexProperty,
            ComposerPreeditTextProperty,
            IsComposerEnabledProperty,
            IsSlashAutocompleteVisibleProperty,
            SelectedSlashSuggestionIndexProperty,
            SlashSuggestionsProperty,
            SlashAutocompletePathPrefixProperty,
            SlashAutocompleteNextStepProperty,
            SlashAutocompleteBreadcrumbProperty,
            ShowCockpitCommandLineProperty,
            CommandLineTextProperty,
            CommandLinePreviewProperty,
            CommandLinePreviewKindProperty,
            CommandLineCaretIndexProperty,
            ComposerPreviewProperty,
            ComposerPreviewKindProperty);

        ShowCockpitCommandLineProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnShowCockpitCommandLineChanged);
        CommandLineTextProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnCommandLineTextChanged);
        CommandLineCaretIndexProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnCommandLineCaretIndexChanged);
        ComposerTextProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnComposerTextChanged);
        ComposerCaretIndexProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnComposerCaretIndexChanged);
        ComposerPreviewProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnComposerPreviewChanged);
        ComposerPreviewKindProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnComposerPreviewChanged);
        CommandLinePreviewProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnCommandLinePreviewChanged);
        CommandLinePreviewKindProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnCommandLinePreviewChanged);
        TopicNavigatorSearchQueryProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnTopicNavigatorSearchQueryChanged);
    }

    private static void OnShowCockpitCommandLineChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            control._commandLineFocused = true;
            control.CollapseCommandLineSelection();
            control.InvalidateVisual();
        }
    }

    private static void OnCommandLineTextChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is null)
            return;

        control.CollapseCommandLineSelection();
        control.InvalidateComposerChrome();
    }

    private static void OnCommandLineCaretIndexChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not int)
            return;

        if (!control._commandLineExtendSelection)
            control.CollapseCommandLineSelection();

        control._commandLineExtendSelection = false;
        control.InvalidateComposerChrome();
    }

    private static void OnComposerTextChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is null)
            return;

        control.CollapseComposerSelection();
        control.InvalidateComposerChrome();
    }

    private static void OnComposerCaretIndexChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not int)
            return;

        if (!control._composerExtendSelection)
            control.CollapseComposerSelection();

        control._composerExtendSelection = false;
        control.InvalidateComposerChrome();
    }

    private static void OnComposerPreviewChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e) =>
        control.InvalidateComposerChrome();

    private static void OnCommandLinePreviewChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e) =>
        control.InvalidateComposerChrome();

    public SkiaChatSurfaceControl()
    {
        ClipToBounds = true;
        MinWidth = 160;
        MinHeight = 120;
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
        AddHandler(PointerExitedEvent, OnPointerExited, RoutingStrategies.Bubble);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Bubble);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
        InitializeIntercomComposer();
        InitializeIntercomAttachDrop();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
        RefreshTheme();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopComposerCaretBlink();
        ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        _skiaFrame?.Dispose();
        _skiaFrame = null;
        _skiaFrameWidth = 0;
        _skiaFrameHeight = 0;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SnapshotProperty)
            _feedLayoutCacheKey = null;

        if (change.Property == SnapshotProperty
            || change.Property == OverviewModeProperty
            || change.Property == DetailThreadIdProperty)
        {
            if (change.Property == OverviewModeProperty || change.Property == DetailThreadIdProperty)
                _scrollOffset = 0;

            var next = Snapshot ?? ChatSurfaceSnapshot.Empty;
            if (change.Property == SnapshotProperty)
            {
                if (DetailThreadId == Guid.Empty && next.State.ActiveThreadId != Guid.Empty)
                    DetailThreadId = next.State.ActiveThreadId;
                else if (DetailThreadId != Guid.Empty
                         && !next.Layout.Lanes.Any(lane => lane.Thread.ThreadId == DetailThreadId))
                    DetailThreadId = next.State.ActiveThreadId;
            }
            else if (change.Property == DetailThreadIdProperty
                     && !next.Layout.Lanes.Any(lane => lane.Thread.ThreadId == DetailThreadId)
                     && next.State.ActiveThreadId != Guid.Empty)
            {
                DetailThreadId = next.State.ActiveThreadId;
            }
            ClampScrollToContent();
            InvalidateVisual();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ClampScrollToContent();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? MinWidth : availableSize.Width;
        var h = double.IsInfinity(availableSize.Height) ? MinHeight : availableSize.Height;
        return new Size(Math.Max(MinWidth, w), Math.Max(MinHeight, h));
    }

    protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);


    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        RefreshTheme();
        InvalidateVisual();
    }

    private bool ShouldShowTopicNavigator(int topicCount) =>
        topicCount > 0
        && !OverviewMode
        && (CompactSideHost
            ? TopicNavigatorVisible
            : TopicNavigatorVisible || !ForwardHost);

    private void RefreshTheme()
    {
        _theme = SkiaChatTheme.Resolve(this);
        var surface = _theme.Surface;
        if (surface.Alpha < byte.MaxValue)
            _theme = _theme with { Surface = surface.WithAlpha(byte.MaxValue) };
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2
            && !_navigatorSearchFocused
            && DetailThreadId != Guid.Empty
            && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0)
        {
            TopicRenameRequested?.Invoke(this, new TopicRenameRequestEventArgs(DetailThreadId, showContextMenu: false));
            e.Handled = true;
            return;
        }

        if (e.Key != Key.C || (e.KeyModifiers & KeyModifiers.Control) == 0)
            return;

        if (!await TryCopySelectedMessageAsync().ConfigureAwait(true))
            return;

        e.Handled = true;
    }

    private async Task<bool> TryCopySelectedMessageAsync()
    {
        if (SelectedMessageIndex < 0)
            return false;

        var body = ChatSurfaceSnapshotMessageLookup.TryGetMessageBody(Snapshot, SelectedMessageIndex);
        if (string.IsNullOrEmpty(body))
            return false;

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard is not { } clipboard)
            return false;

        await clipboard.SetTextAsync(body).ConfigureAwait(true);
        return true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var p = e.GetPosition(this);
        if (TryDispatchPointerWheel(p, e))
        {
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        _scrollOffset -= e.Delta.Y * WheelPixelsPerDelta;
        ClampScrollToContent();
        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var index = _chatHits.FindIndex(e.GetPosition(this));
        if (index == _hoveredItem)
            return;
        _hoveredItem = index;
        var hand = index >= 0 && _chatHits.TryGetHit(index, out var hoverHit) && SkiaChatHitRegistry.WantsHandCursor(hoverHit);
        Cursor = hand ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!TryDispatchPointerPress(e.GetPosition(this), e))
            return;

        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_hoveredItem < 0)
            return;
        _hoveredItem = -1;
        Cursor = new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private void ClampScrollToContent(bool? showOverviewCatalog = null, string? statusSubtitle = null, float? bottomChrome = null)
    {
        var catalog = showOverviewCatalog ?? (OverviewMode && (Snapshot?.Layout.Overview.Count ?? 0) > 0);
        var subtitle = statusSubtitle;
        if (subtitle is null && ForwardHost && Snapshot is { } snap)
        {
            var showTabs = !OverviewMode && snap.Layout.Overview.Count > 0;
            subtitle = ChatIntercomChromeStatusPresentation.FormatSubtitle(snap, OverviewMode, DetailThreadId, showTabs);
        }

        var chromeTop = SkiaChatChromeRenderer.ResolveTopChromeHeight(
            ForwardHost,
            catalog,
            !string.IsNullOrWhiteSpace(subtitle),
            OverviewMode,
            Snapshot?.Layout.Overview.Count ?? 0,
            HasScopeStrip(Snapshot));
        var chromeBottom = bottomChrome ?? (float)ResolveBottomChromeHeight((float)Math.Max(160, Bounds.Width));
        var viewport = Math.Max(1, Bounds.Height - chromeTop - chromeBottom);
        var max = Math.Max(0, _cachedContentHeight - viewport);
        if (_scrollOffset > max)
            _scrollOffset = max;
        if (_scrollOffset < 0)
            _scrollOffset = 0;
    }

    private static string? MergeStatusSubtitles(string? primary, string? fmUsage)
    {
        var p = primary?.Trim();
        var f = fmUsage?.Trim();
        if (string.IsNullOrEmpty(p))
            return string.IsNullOrEmpty(f) ? null : f;
        if (string.IsNullOrEmpty(f))
            return p;
        return p + " · " + f;
    }

}
