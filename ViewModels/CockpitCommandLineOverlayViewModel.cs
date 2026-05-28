#nullable enable

using System.ComponentModel;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Cockpit;
using CascadeIDE.Features.Cockpit.Application;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Presentation Cockpit Command Line overlay (editor host, ADR 0120).</summary>
public sealed partial class CockpitCommandLineOverlayViewModel : ObservableObject
{
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
    }

    public ChatPanelViewModel ChatPanel { get; }

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
            or nameof(ChatPanelViewModel.ChatSlashBreadcrumb))
            OnPropertyChanged(string.Empty);
    }

    public void NotifyShellPresentationChanged() => OnPropertyChanged(nameof(IsOverlayVisible));
}
