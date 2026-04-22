using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Затемнение на весь родитель и центрированная панель (как палитра команд Ctrl+Q).
/// <see cref="PassThroughInput"/> — не перехватывать мышь (подсказки CascadeChord).
/// </summary>
public partial class ModalOverlay : UserControl
{
    public static readonly StyledProperty<object?> ChildProperty =
        AvaloniaProperty.Register<ModalOverlay, object?>(nameof(Child));

    /// <summary>Содержимое центрированной панели (первый дочерний элемент в XAML).</summary>
    [Content]
    public object? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public static readonly StyledProperty<bool> PassThroughInputProperty =
        AvaloniaProperty.Register<ModalOverlay, bool>(nameof(PassThroughInput));

    public static readonly StyledProperty<double> MaxPanelWidthProperty =
        AvaloniaProperty.Register<ModalOverlay, double>(nameof(MaxPanelWidth), 720);

    public static readonly StyledProperty<double> MaxPanelHeightProperty =
        AvaloniaProperty.Register<ModalOverlay, double>(nameof(MaxPanelHeight), 480);

    public static readonly StyledProperty<Thickness> PanelMarginProperty =
        AvaloniaProperty.Register<ModalOverlay, Thickness>(nameof(PanelMargin), new Thickness(40));

    /// <summary>Положение панели с содержимым по вертикали (палитра — по центру; лёгкий оверлей аккорда — сверху).</summary>
    public static readonly StyledProperty<VerticalAlignment> PanelVerticalAlignmentProperty =
        AvaloniaProperty.Register<ModalOverlay, VerticalAlignment>(nameof(PanelVerticalAlignment), VerticalAlignment.Center);

    public static readonly StyledProperty<IBrush?> DimmerBrushProperty =
        AvaloniaProperty.Register<ModalOverlay, IBrush?>(nameof(DimmerBrush), new SolidColorBrush(Color.Parse("#AA000000")));

    /// <summary>Если true — весь оверлей прозрачен для hit-test (клики проходят к окну).</summary>
    public bool PassThroughInput
    {
        get => GetValue(PassThroughInputProperty);
        set => SetValue(PassThroughInputProperty, value);
    }

    public double MaxPanelWidth
    {
        get => GetValue(MaxPanelWidthProperty);
        set => SetValue(MaxPanelWidthProperty, value);
    }

    public double MaxPanelHeight
    {
        get => GetValue(MaxPanelHeightProperty);
        set => SetValue(MaxPanelHeightProperty, value);
    }

    public Thickness PanelMargin
    {
        get => GetValue(PanelMarginProperty);
        set => SetValue(PanelMarginProperty, value);
    }

    public VerticalAlignment PanelVerticalAlignment
    {
        get => GetValue(PanelVerticalAlignmentProperty);
        set => SetValue(PanelVerticalAlignmentProperty, value);
    }

    public IBrush? DimmerBrush
    {
        get => GetValue(DimmerBrushProperty);
        set => SetValue(DimmerBrushProperty, value);
    }

    /// <summary>Клик по затемнению (не по панели). Не вызывается при <see cref="PassThroughInput"/>.</summary>
    public event EventHandler<PointerPressedEventArgs>? DimmerPressed;

    public ModalOverlay()
    {
        InitializeComponent();
        PassThroughInputProperty.Changed.AddClassHandler<ModalOverlay>((o, _) => o.SyncDimmerHitTest());
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataContextProperty || change.Property == ChildProperty)
            SyncChildDataContext();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SyncDimmerHitTest();
        SyncChildDataContext();
    }

    /// <summary>
    /// Логическое содержимое задаётся в <see cref="Child"/> и попадает в шаблон через <c>ContentPresenter</c>;
    /// у корневого <see cref="Child"/> иначе может не быть того же DataContext, что у окна — биндинги «молчат».
    /// </summary>
    private void SyncChildDataContext()
    {
        if (Child is Control c)
            c.DataContext = DataContext;
    }

    private void SyncDimmerHitTest()
    {
        if (Dimmer is null)
            return;
        Dimmer.IsHitTestVisible = !PassThroughInput;
    }

    private void OnDimmerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (PassThroughInput)
            return;
        DimmerPressed?.Invoke(this, e);
    }

    private void OnPanelPointerPressed(object? sender, PointerPressedEventArgs e) =>
        e.Handled = true;
}
