#nullable enable

using CascadeIDE.Features.Cockpit;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Cockpit Command Line на Forward host: <c>primary=intercom</c> — Skia в ChatPanel; <c>primary=editor</c> — оверлей <see cref="Views.CockpitCommandLineOverlayView"/> (ADR 0120).
/// </summary>
public partial class MainWindowViewModel
{
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

