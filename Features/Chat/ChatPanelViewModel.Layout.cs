#nullable enable
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Intercom в лобовом Forward (ADR 0120): <c>primary_work_surface = intercom</c>, на всю зону.</summary>
    [ObservableProperty]
    private bool _isForwardIntercomLayout;

    /// <summary>MFD Chat: Skia Intercom на странице Chat (ADR 0123).</summary>
    [ObservableProperty]
    private bool _useComfortableSkiaIntercomHost = true;

    public bool IsSkiaIntercomHostVisible => IsForwardIntercomLayout || UseComfortableSkiaIntercomHost;

    /// <summary>Метрики ленты для Forward-хоста (плотнее). MFD Chat — false. Не «узкая колонка».</summary>
    public bool IntercomForwardHost => IsForwardIntercomLayout;

    /// <summary>Forward: toggle Navigator; MFD Skia — панель всегда при наличии тем (ADR 0127-E).</summary>
    [ObservableProperty]
    private bool _isTopicNavigatorVisible;

    public bool IntercomTopicNavigatorVisible => !IntercomForwardHost || IsTopicNavigatorVisible;

    [ObservableProperty]
    private string _topicNavigatorSearchQuery = "";

    /// <summary>Шрифты Skia-ленты и MFD-панели из <c>[fonts.intercom]</c>.</summary>
    [ObservableProperty]
    private IntercomFontsSettings _intercomFonts = new();

    public event EventHandler<IntercomFontsSettings>? IntercomPanelFontsChanged;

    partial void OnIsForwardIntercomLayoutChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSkiaIntercomHostVisible));
        OnPropertyChanged(nameof(IntercomForwardHost));
    }

    public void SetIntercomFontsSettings(IntercomFontsSettings fonts) =>
        IntercomFonts = fonts;

    partial void OnIntercomFontsChanged(IntercomFontsSettings value) =>
        IntercomPanelFontsChanged?.Invoke(this, value);

    partial void OnUseComfortableSkiaIntercomHostChanged(bool value) =>
        OnPropertyChanged(nameof(IsSkiaIntercomHostVisible));

    partial void OnIsTopicNavigatorVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(IntercomTopicNavigatorVisible));

    public void ToggleIntercomTopicNavigator() =>
        IsTopicNavigatorVisible = !IsTopicNavigatorVisible;
}
