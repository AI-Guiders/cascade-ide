#nullable enable
using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

public partial class ZoneHostWindow : Window
{
    public ZoneHostWindow()
    {
        InitializeComponent();
    }

    public void SetBadge(string text) => HostBadge.Text = text;

    public void Mount(UIElement? content)
    {
        HostSlot.Content = content;
    }

    public bool HasMountedContent => HostSlot.Content is not null;

    public UIElement? Dismount()
    {
        if (HostSlot.Content is not UIElement el)
            return null;
        HostSlot.Content = null;
        return el;
    }
}
