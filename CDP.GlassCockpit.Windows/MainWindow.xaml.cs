#nullable enable
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

public partial class MainWindow : Window
{
    readonly LatchHub _latches;

    public MainWindow()
    {
        InitializeComponent();
        // Peel0: plain text; Markdown highlighter not always shipped in AvalonEdit defs.
        IntercomEditor.Options.EnableHyperlinks = false;
        IntercomEditor.Text =
            "# Intercom\n\n" +
            "Long-form Forward seat.\n" +
            "Watching %LocalAppData%/cdp-mcp/intercom-LATEST.json\n\n" +
            "(peel0 — wire paint only; composer/send later)\n";

        _latches = new LatchHub();
        _latches.IntercomChanged += OnIntercomChanged;
        _latches.PresentationChanged += OnPresentationChanged;
        _latches.Start();
        StatusText.Text = $"glass · spike0 · watching {_latches.StateRoot}";
        Closed += (_, _) => _latches.Dispose();
    }

    void OnIntercomChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var body = File.ReadAllText(path);
                IntercomEditor.Text = body;
                StatusText.Text = $"glass · intercom latch · {DateTime.Now:HH:mm:ss}";
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
                var body = File.ReadAllText(path);
                PlanBox.Text = "presentation-LATEST\n\n" + body;
                StatusText.Text = $"glass · presentation latch · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · presentation read fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }
}
