#nullable enable

using System.Windows;
using System.Windows.Controls;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows.UiKit;

/// <summary>WPF adapt of Avalonia CascadeStatusChip — electric Glass indication, Dark Cockpit quiet-default.</summary>
public partial class GlassStatusChip : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(GlassStatusChip), new PropertyMetadata(""));

    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(GlassChipLevel), typeof(GlassStatusChip),
            new PropertyMetadata(GlassChipLevel.Quiet, OnLevelChanged));

    public static readonly DependencyProperty TipProperty =
        DependencyProperty.Register(nameof(Tip), typeof(string), typeof(GlassStatusChip),
            new PropertyMetadata(null, OnLevelChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public GlassChipLevel Level
    {
        get => (GlassChipLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public string? Tip
    {
        get => (string?)GetValue(TipProperty);
        set => SetValue(TipProperty, value);
    }

    public GlassStatusChip()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLevel();
    }

    static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassStatusChip chip)
            chip.ApplyLevel();
    }

    void ApplyLevel()
    {
        if (ChipFrame is null || ChipLabel is null)
            return;

        var (bg, border, fg) = Level switch
        {
            GlassChipLevel.Fail => ("Glass.FailBg", "Glass.FailBorder", "Glass.FailFg"),
            GlassChipLevel.Warn => ("Glass.WarnBg", "Glass.WarnBorder", "Glass.WarnFg"),
            GlassChipLevel.Caution => ("Glass.CautionBg", "Glass.CautionBorder", "Glass.CautionFg"),
            _ => ("Glass.ChipQuietBg", "Glass.ChipQuietBorder", "Glass.ChipQuietFg"),
        };

        ChipFrame.Background = (System.Windows.Media.Brush)FindResource(bg);
        ChipFrame.BorderBrush = (System.Windows.Media.Brush)FindResource(border);
        ChipLabel.Foreground = (System.Windows.Media.Brush)FindResource(fg);
        ToolTip = string.IsNullOrWhiteSpace(Tip) ? null : Tip;
    }
}
