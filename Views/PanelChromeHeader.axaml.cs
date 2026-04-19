using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CascadeIDE.Views;

public partial class PanelChromeHeader : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PanelChromeHeader, string?>(nameof(Title));

    public static readonly StyledProperty<bool> ShowOverflowProperty =
        AvaloniaProperty.Register<PanelChromeHeader, bool>(nameof(ShowOverflow), defaultValue: true);

    /// <summary>Как на концепте: короткие метки панелей в верхнем регистре (латиница/кириллица).</summary>
    public static readonly StyledProperty<bool> UppercaseTitleProperty =
        AvaloniaProperty.Register<PanelChromeHeader, bool>(nameof(UppercaseTitle), defaultValue: false);

    /// <summary>Кнопка «EMERGENCY STOP» в полосе заголовка (режим Power, концепт cockpit).</summary>
    public static readonly StyledProperty<bool> ShowEmergencyStopProperty =
        AvaloniaProperty.Register<PanelChromeHeader, bool>(nameof(ShowEmergencyStop), defaultValue: false);

    public static readonly StyledProperty<ICommand?> EmergencyStopCommandProperty =
        AvaloniaProperty.Register<PanelChromeHeader, ICommand?>(nameof(EmergencyStopCommand));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowOverflow
    {
        get => GetValue(ShowOverflowProperty);
        set => SetValue(ShowOverflowProperty, value);
    }

    public bool UppercaseTitle
    {
        get => GetValue(UppercaseTitleProperty);
        set => SetValue(UppercaseTitleProperty, value);
    }

    public bool ShowEmergencyStop
    {
        get => GetValue(ShowEmergencyStopProperty);
        set => SetValue(ShowEmergencyStopProperty, value);
    }

    public ICommand? EmergencyStopCommand
    {
        get => GetValue(EmergencyStopCommandProperty);
        set => SetValue(EmergencyStopCommandProperty, value);
    }

    static PanelChromeHeader()
    {
        UppercaseTitleProperty.Changed.AddClassHandler<PanelChromeHeader>((h, _) => h.SyncTitleCapsClass());
    }

    public PanelChromeHeader()
    {
        InitializeComponent();
        SyncTitleCapsClass();
    }

    /// <summary>Класс <c>panelChromeTitleCaps</c> для межбуквенного интервала; текст задаётся привязкой <see cref="PanelChromeTitleDisplayConverter"/>.</summary>
    void SyncTitleCapsClass()
    {
        if (TitleText is null)
            return;
        TitleText.Classes.Remove("panelChromeTitleCaps");
        if (UppercaseTitle)
            TitleText.Classes.Add("panelChromeTitleCaps");
    }

    void OnOverflowButtonClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not Button { ContextMenu: { } menu } btn)
            return;
        menu.PlacementTarget = btn;
        menu.Placement = PlacementMode.BottomEdgeAlignedRight;
        menu.Open(btn);
    }

    async void OnCopyTitleMenuClick(object? sender, RoutedEventArgs e)
    {
        var text = TitleText?.Text ?? Title ?? "";
        if (string.IsNullOrEmpty(text))
            return;
        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard is { } clip)
            await clip.SetTextAsync(text);
    }
}
