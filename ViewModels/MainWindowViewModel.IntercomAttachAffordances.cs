#nullable enable

using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Views;

namespace CascadeIDE.ViewModels;

/// <summary>Intercom attach affordances: selection hotkey, drag-to-composer (ADR 0128).</summary>
public partial class MainWindowViewModel
{
    internal void WireIntercomAttachAffordances()
    {
        ChatPanel.SetDiagnosticStripsAccessor(() =>
            _workspaceDiagnostics.GetStripsForFile(CurrentFilePath));
        ChatPanel.SetFocusIntercomComposerAction(FocusIntercomComposer);
    }

    internal void FocusIntercomComposer()
    {
        if (PrimaryWorkSurface != PrimaryWorkSurfaceKind.Intercom
            && TogglePrimaryWorkSurfaceCommand.CanExecute(null))
        {
            TogglePrimaryWorkSurfaceCommand.Execute(null);
        }
    }

    internal bool TryCompleteIntercomAttachDragAtScreen(double screenX, double screenY, string kind)
    {
        if (!TryFindIntercomComposerAtScreen(screenX, screenY, out var chatPanel))
            return false;

        chatPanel.ClarificationStatusText = chatPanel.AttachDragKindToComposer(kind);
        FocusIntercomComposer();
        return true;
    }

    internal void AttachSelectedProblemToIntercom(ProblemListItem item)
    {
        var message = ChatPanel.AttachProblemToComposer(item);
        ChatPanel.ClarificationStatusText = message;
        FocusIntercomComposer();
    }

    private static bool TryFindIntercomComposerAtScreen(double screenX, double screenY, out ChatPanelViewModel chatPanel)
    {
        chatPanel = null!;
        var point = new PixelPoint((int)Math.Round(screenX), (int)Math.Round(screenY));
        if (Avalonia.Application.Current?.ApplicationLifetime
            is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return false;
        }

        foreach (var window in desktop.Windows)
        {
            if (window is null)
                continue;

            var local = window.PointToClient(point);
            var hit = window.GetVisualAt(local);
            while (hit is not null)
            {
                if (hit is ChatPanelView chatView
                    && chatView.DataContext is ChatPanelViewModel vm
                    && chatView.TryHitComposerAt(local))
                {
                    chatPanel = vm;
                    return true;
                }

                hit = hit.GetVisualParent();
            }
        }

        return false;
    }
}
