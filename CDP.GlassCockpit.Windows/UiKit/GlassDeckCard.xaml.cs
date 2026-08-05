#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows.UiKit;

/// <summary>Instrument deck card — tone via GlassDarkCockpit tokens (edit-locus).</summary>
public partial class GlassDeckCard : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(GlassDeckCard),
            new PropertyMetadata("", OnChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(GlassDeckCard),
            new PropertyMetadata("", OnChanged));

    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(nameof(Tone), typeof(string), typeof(GlassDeckCard),
            new PropertyMetadata("idle", OnChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>ok | warn | bad | idle | meta</summary>
    public string Tone
    {
        get => (string)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    public GlassDeckCard()
    {
        InitializeComponent();
        Loaded += (_, _) => Sync();
    }

    public static GlassDeckCard FromChip(GlassGlanceChip chip) =>
        new()
        {
            Label = chip.Label,
            Value = chip.Value,
            Tone = chip.Tone,
        };

    static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassDeckCard card)
            card.Sync();
    }

    void Sync()
    {
        if (LabelBlock is null || CardFrame is null)
            return;

        LabelBlock.Text = Label ?? "";
        ValueBlock.Text = Value ?? "";

        var tone = (Tone ?? "idle").Trim().ToLowerInvariant();
        var (bg, border, fg) = tone switch
        {
            "ok" => ("Glass.DeckOkBg", "Glass.DeckOkBorder", "Glass.DeckOkFg"),
            "warn" => ("Glass.DeckWarnBg", "Glass.DeckWarnBorder", "Glass.DeckWarnFg"),
            "bad" => ("Glass.DeckBadBg", "Glass.DeckBadBorder", "Glass.DeckBadFg"),
            "meta" => ("Glass.DeckMetaBg", "Glass.DeckMetaBorder", "Glass.DeckMetaFg"),
            _ => ("Glass.DeckIdleBg", "Glass.DeckIdleBorder", "Glass.DeckIdleFg"),
        };

        CardFrame.Background = Brush(bg);
        CardFrame.BorderBrush = Brush(border);
        ValueBlock.Foreground = Brush(fg);
    }

    Brush Brush(string key)
    {
        try
        {
            return (Brush)FindResource(key);
        }
        catch
        {
            return Brushes.Gray;
        }
    }
}
