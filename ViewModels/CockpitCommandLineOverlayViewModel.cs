#nullable enable

using System.ComponentModel;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Cockpit.Application;
using CascadeIDE.IdeDisplay.CockpitCommandLine;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Presentation Cockpit Command Line overlay (editor host, ADR 0120 / IDS 0079).</summary>
public sealed partial class CockpitCommandLineOverlayViewModel : ObservableObject
{
    private readonly CockpitCommandLineSurfaceCompositor _surfaceCompositor = new();
    private readonly Func<PrimaryWorkSurfaceKind> _getPrimaryWorkSurface;
    private readonly Func<CommandPaletteHost> _getCommandPaletteHost;

    public CockpitCommandLineOverlayViewModel(
        ChatPanelViewModel chatPanel,
        Func<PrimaryWorkSurfaceKind> getPrimaryWorkSurface,
        Func<CommandPaletteHost> getCommandPaletteHost)
    {
        ChatPanel = chatPanel;
        _getPrimaryWorkSurface = getPrimaryWorkSurface;
        _getCommandPaletteHost = getCommandPaletteHost;
        ChatPanel.PropertyChanged += OnChatPanelPropertyChanged;
        RefreshCockpitCommandLineSurfaceSnapshot();
    }

    public ChatPanelViewModel ChatPanel { get; }

    public CockpitCommandLineSurfaceSnapshot CockpitCommandLineSurfaceSnapshot { get; private set; } =
        CockpitCommandLineSurfaceSnapshot.Empty;

    public bool IsOverlayVisible(CommandPaletteHost currentTopLevelHost) =>
        CockpitCommandLineHostPolicy.ShouldShowEditorOverlay(
            ChatPanel.IsCockpitCommandLineOpen,
            ChatPanel.CommandLineSession.ActiveHost,
            _getPrimaryWorkSurface(),
            _getCommandPaletteHost(),
            currentTopLevelHost);

    private void OnChatPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatPanelViewModel.IsCockpitCommandLineOpen)
            or nameof(ChatPanelViewModel.CockpitCommandLineText)
            or nameof(ChatPanelViewModel.CockpitCommandLineCaretIndex)
            or nameof(ChatPanelViewModel.CommandLineSlashPreview)
            or nameof(ChatPanelViewModel.IsChatSlashAutocompleteVisible)
            or nameof(ChatPanelViewModel.SelectedChatSlashSuggestionIndex)
            or nameof(ChatPanelViewModel.ChatSlashBreadcrumb)
            or nameof(ChatPanelViewModel.ComposerPopupSuggestions))
        {
            RefreshCockpitCommandLineSurfaceSnapshot();
            OnPropertyChanged(string.Empty);
        }
    }

    public void NotifyShellPresentationChanged() => OnPropertyChanged(nameof(IsOverlayVisible));

    internal void RefreshCockpitCommandLineSurfaceSnapshot()
    {
        if (!ChatPanel.IsCockpitCommandLineOpen)
        {
            CockpitCommandLineSurfaceSnapshot = CockpitCommandLineSurfaceSnapshot.Empty;
            OnPropertyChanged(nameof(CockpitCommandLineSurfaceSnapshot));
            return;
        }

        var suggestions = ChatPanel.ComposerPopupSuggestions
            .Select(s => new CockpitCommandLineSuggestionRow(s.ListTitle, s.ListSubtitle))
            .ToArray();

        var intent = new CockpitCommandLineSurfaceIntent(
            ChatPanel.CockpitCommandLineText,
            ChatPanel.CockpitCommandLineCaretIndex,
            ChatPanel.SelectedChatSlashSuggestionIndex,
            ChatPanel.ChatSlashBreadcrumb,
            ChatPanel.IsChatSlashAutocompleteVisible,
            suggestions);

        CockpitCommandLineSurfaceSnapshot = _surfaceCompositor.Compose(intent);
        OnPropertyChanged(nameof(CockpitCommandLineSurfaceSnapshot));
    }
}
