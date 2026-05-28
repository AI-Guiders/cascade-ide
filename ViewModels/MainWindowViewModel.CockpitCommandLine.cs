#nullable enable

using CascadeIDE.Features.Cockpit;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>
    /// Открыть Cockpit Command Line на активном Forward host.
    /// <c>primary=intercom</c> — inline Skia в ChatPanel.
    /// <c>primary=editor</c> — только оверлей <see cref="Views.CockpitCommandLineOverlayView"/> (как Ctrl+Q); inline в редакторе не используется.
    /// </summary>
    internal async Task OpenCockpitCommandLineOnActiveForwardHostAsync(string initialText = "/")
    {
        var text = string.IsNullOrWhiteSpace(initialText) ? "/" : initialText.Trim();

        if (PrimaryWorkSurface == PrimaryWorkSurfaceKind.Intercom)
        {
            // UI: Intercom Skia surface.
            await UiScheduler.Default.InvokeAsync(() =>
                ChatPanel.CommandLineSession.Open(CockpitCommandLineHostKind.Intercom, text));
            return;
        }

        await UiScheduler.Default.InvokeAsync(() =>
            ChatPanel.CommandLineSession.Open(CockpitCommandLineHostKind.Editor, text));
    }
}

