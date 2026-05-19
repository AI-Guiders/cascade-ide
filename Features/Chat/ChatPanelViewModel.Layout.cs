#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Intercom в якоре Forward (ADR 0120): компактный chrome и плотнее Skia-лента.</summary>
    [ObservableProperty]
    private bool _isForwardIntercomLayout;

    /// <summary>MFD Chat и др.: Skia-only Intercom без legacy UiKit-оболочки (ADR 0123 comfortable).</summary>
    [ObservableProperty]
    private bool _useComfortableSkiaIntercomHost = true;

    public bool IsSkiaIntercomHostVisible => IsForwardIntercomLayout || UseComfortableSkiaIntercomHost;

    public bool IntercomSkiaCompactLayout => IsForwardIntercomLayout;

    partial void OnIsForwardIntercomLayoutChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSkiaIntercomHostVisible));
        OnPropertyChanged(nameof(IntercomSkiaCompactLayout));
    }

    partial void OnUseComfortableSkiaIntercomHostChanged(bool value) =>
        OnPropertyChanged(nameof(IsSkiaIntercomHostVisible));
}
