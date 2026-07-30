#nullable enable
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;

    public MainWindow()
    {
        InitializeComponent();
        IntercomEditor.Options.EnableHyperlinks = false;
        IntercomHeader.Text = "Forward = Intercom — waiting latch…";
        IntercomEditor.Text =
            "Long-form Forward seat.\n" +
            "Messages paint from intercom-LATEST (body only — not raw JSON).\n";

        _latches = new LatchHub();
        _latches.IntercomChanged += OnIntercomChanged;
        _latches.PresentationChanged += OnPresentationChanged;
        _latches.Start();
        StatusText.Text = $"glass · watching {_latches.StateRoot}";
        Closed += (_, _) => _latches.Dispose();
    }

    void OnIntercomChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintIntercom(raw);
                IntercomHeader.Text = view.Header;
                IntercomEditor.Text = view.Body;
                StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · intercom read fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnPresentationChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintPresentation(raw);
                PlanBox.Text = view.PlanText;
                SelectMfdPage(view.MfdPage);
                StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · presentation read fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void SelectMfdPage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return;

        foreach (var item in MfdPages.Items)
        {
            if (item is ListBoxItem lbi &&
                string.Equals(lbi.Content?.ToString(), page, StringComparison.OrdinalIgnoreCase))
            {
                MfdPages.SelectedItem = lbi;
                return;
            }
        }
    }
}
