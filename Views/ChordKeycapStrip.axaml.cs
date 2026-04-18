using Avalonia;
using Avalonia.Controls;
using CascadeIDE.Services.ChordNotation;

namespace CascadeIDE.Views;

/// <summary>Горизонтальные ряды «клавиш» для последовательности аккордов (кэпы, не plain text).</summary>
public partial class ChordKeycapStrip : UserControl
{
    public static readonly StyledProperty<ChordKeycapSequence?> SequenceProperty =
        AvaloniaProperty.Register<ChordKeycapStrip, ChordKeycapSequence?>(nameof(Sequence));

    static ChordKeycapStrip()
    {
        SequenceProperty.Changed.AddClassHandler<ChordKeycapStrip>(OnSequenceChanged);
    }

    public ChordKeycapSequence? Sequence
    {
        get => GetValue(SequenceProperty);
        set => SetValue(SequenceProperty, value);
    }

    private ItemsControl? _stepsHost;

    public ChordKeycapStrip()
    {
        InitializeComponent();
        _stepsHost = this.FindControl<ItemsControl>(nameof(StepsHost));
        SyncItemsSource();
    }

    private static void OnSequenceChanged(ChordKeycapStrip strip, AvaloniaPropertyChangedEventArgs e) =>
        strip.SyncItemsSource();

    private void SyncItemsSource()
    {
        if (_stepsHost is null)
            return;
        _stepsHost.ItemsSource = Sequence?.Steps ?? Array.Empty<ChordKeycapStep>();
    }
}
