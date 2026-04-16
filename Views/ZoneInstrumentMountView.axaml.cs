using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

public partial class ZoneInstrumentMountView : UserControl
{
    public static readonly StyledProperty<string> InstrumentIdProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, string>(nameof(InstrumentId), "workspace_health_status_v1");

    public static readonly StyledProperty<string> SlotIdProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, string>(nameof(SlotId), "pfd");

    public static readonly StyledProperty<string> MountStyleProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, string>(nameof(MountStyle), InstrumentMountPolicyIds.V1);

    public static readonly StyledProperty<string> HeaderTextProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, string>(nameof(HeaderText), string.Empty);

    public static readonly StyledProperty<IBrush?> HostBorderBrushProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, IBrush?>(nameof(HostBorderBrush), Brush.Parse("#5A6E8C"));

    public static readonly StyledProperty<IBrush?> HeaderBrushProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, IBrush?>(nameof(HeaderBrush), Brush.Parse("#A9D9FF"));

    public static readonly StyledProperty<IBrush?> LabelBrushProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, IBrush?>(nameof(LabelBrush), Brush.Parse("#9FB4C9"));

    public static readonly StyledProperty<IBrush?> ValueBrushProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, IBrush?>(nameof(ValueBrush), Brush.Parse("#DCE8F2"));

    public static readonly StyledProperty<IBrush?> SafetyBrushProperty =
        AvaloniaProperty.Register<ZoneInstrumentMountView, IBrush?>(nameof(SafetyBrush), Brush.Parse("#C9F0FF"));

    static ZoneInstrumentMountView()
    {
        InstrumentIdProperty.Changed.AddClassHandler<ZoneInstrumentMountView>((x, _) => x.ApplyPolicyDefaults());
        SlotIdProperty.Changed.AddClassHandler<ZoneInstrumentMountView>((x, _) => x.ApplyPolicyDefaults());
        MountStyleProperty.Changed.AddClassHandler<ZoneInstrumentMountView>((x, _) => x.ApplyPolicyDefaults());
    }

    public ZoneInstrumentMountView()
    {
        InitializeComponent();
        ApplyPolicyDefaults();
    }

    public string InstrumentId
    {
        get => GetValue(InstrumentIdProperty);
        set => SetValue(InstrumentIdProperty, value);
    }

    public string SlotId
    {
        get => GetValue(SlotIdProperty);
        set => SetValue(SlotIdProperty, value);
    }

    public string MountStyle
    {
        get => GetValue(MountStyleProperty);
        set => SetValue(MountStyleProperty, value);
    }

    public string HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public IBrush? HostBorderBrush
    {
        get => GetValue(HostBorderBrushProperty);
        set => SetValue(HostBorderBrushProperty, value);
    }

    public IBrush? HeaderBrush
    {
        get => GetValue(HeaderBrushProperty);
        set => SetValue(HeaderBrushProperty, value);
    }

    public IBrush? LabelBrush
    {
        get => GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    public IBrush? ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    public IBrush? SafetyBrush
    {
        get => GetValue(SafetyBrushProperty);
        set => SetValue(SafetyBrushProperty, value);
    }

    private void ApplyPolicyDefaults()
    {
        var skin = ZoneInstrumentMountPolicy.Resolve(InstrumentId, SlotId, MountStyle);
        HeaderText = skin.HeaderText;
        HostBorderBrush = skin.HostBorderBrush;
        HeaderBrush = skin.HeaderBrush;
        LabelBrush = skin.LabelBrush;
        ValueBrush = skin.ValueBrush;
        SafetyBrush = skin.SafetyBrush;
    }
}

internal static class ZoneInstrumentMountPolicy
{
    public static ZoneInstrumentSkin Resolve(string? instrumentId, string? slotId, string? slotPolicy)
    {
        var normalizedPolicy = (slotPolicy ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedSlot = (slotId ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedInstrument = (instrumentId ?? string.Empty).Trim().ToLowerInvariant();

        if (normalizedPolicy != InstrumentMountPolicyIds.V1)
            return DefaultSkin(normalizedSlot, normalizedInstrument);

        return normalizedSlot switch
        {
            "mfd" => BuildSkin(
                "MFD STATUS",
                "#6E5A8C",
                "#DCC1FF",
                "#BEA7D6",
                "#EDE4F8",
                "#F2E8FF"),
            "forward" => BuildSkin(
                "FORWARD STATUS",
                "#8C7A5A",
                "#FFE2AD",
                "#D9BE8D",
                "#F7EFD8",
                "#FFF0CF"),
            _ => BuildSkin(
                "PFD STATUS",
                "#5A6E8C",
                "#A9D9FF",
                "#9FB4C9",
                "#DCE8F2",
                "#C9F0FF")
        };
    }

    private static ZoneInstrumentSkin DefaultSkin(string slotId, string instrumentId)
    {
        var fallbackHeader = slotId switch
        {
            "mfd" => "MFD STATUS",
            "forward" => "FORWARD STATUS",
            _ => "PFD STATUS"
        };

        if (instrumentId == "workspace_health_status_v1")
            return BuildSkin(fallbackHeader, "#5A6E8C", "#A9D9FF", "#9FB4C9", "#DCE8F2", "#C9F0FF");

        return BuildSkin($"{instrumentId.ToUpperInvariant()} [{slotId.ToUpperInvariant()}]", "#5A6E8C", "#A9D9FF", "#9FB4C9", "#DCE8F2", "#C9F0FF");
    }

    private static ZoneInstrumentSkin BuildSkin(
        string headerText,
        string hostBorderBrush,
        string headerBrush,
        string labelBrush,
        string valueBrush,
        string safetyBrush) =>
        new(
            headerText,
            Brush.Parse(hostBorderBrush),
            Brush.Parse(headerBrush),
            Brush.Parse(labelBrush),
            Brush.Parse(valueBrush),
            Brush.Parse(safetyBrush));
}

internal sealed record ZoneInstrumentSkin(
    string HeaderText,
    IBrush? HostBorderBrush,
    IBrush? HeaderBrush,
    IBrush? LabelBrush,
    IBrush? ValueBrush,
    IBrush? SafetyBrush);
