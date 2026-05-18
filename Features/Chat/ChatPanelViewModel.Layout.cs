#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Intercom в якоре Forward (ADR 0120): компактный chrome и плотнее Skia-лента.</summary>
    [ObservableProperty]
    private bool _isForwardIntercomLayout;
}
