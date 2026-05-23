#nullable enable
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Intercom в лобовом Forward (ADR 0120): <c>primary_work_surface = intercom</c>, на всю зону.</summary>
    [ObservableProperty]
    private bool _isForwardIntercomLayout;

    /// <summary>Skia chrome: toolbar, spine, вкладки (лобовой Forward на весь экран).</summary>
    public bool IntercomForwardChrome => IsForwardIntercomLayout;

    /// <summary>Лента и composer: <c>comfortable</c> vs <c>compact</c> из <c>[intercom] feed_metrics</c>.</summary>
    [ObservableProperty]
    private bool _intercomComfortableFeed;

    /// <summary>Устаревшее имя привязки: только chrome, не плотность ленты.</summary>
    public bool IntercomForwardHost => IntercomForwardChrome;

    /// <summary>Topic Navigator: при comfortable — как на MFD; иначе toggle на Forward.</summary>
    [ObservableProperty]
    private bool _isTopicNavigatorVisible;

    public bool IntercomTopicNavigatorVisible =>
        IsTopicNavigatorVisible || !IntercomForwardChrome;

    [ObservableProperty]
    private string _topicNavigatorSearchQuery = "";

    /// <summary>Шрифты Skia-ленты и MFD-панели из <c>[fonts.intercom]</c>.</summary>
    [ObservableProperty]
    private IntercomFontsSettings _intercomFonts = new();

    public event EventHandler<IntercomFontsSettings>? IntercomPanelFontsChanged;

    partial void OnIsForwardIntercomLayoutChanged(bool value)
    {
        OnPropertyChanged(nameof(IntercomForwardChrome));
        OnPropertyChanged(nameof(IntercomForwardHost));
        OnPropertyChanged(nameof(IntercomTopicNavigatorVisible));
        if (value && IntercomComfortableFeed)
            IsTopicNavigatorVisible = true;
    }

    public void SetIntercomFontsSettings(IntercomFontsSettings fonts) =>
        IntercomFonts = fonts;

    partial void OnIntercomFontsChanged(IntercomFontsSettings value) =>
        IntercomPanelFontsChanged?.Invoke(this, value);

    partial void OnIsTopicNavigatorVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(IntercomTopicNavigatorVisible));

    public void ToggleIntercomTopicNavigator() =>
        IsTopicNavigatorVisible = !IsTopicNavigatorVisible;

    /// <summary>TOML <c>[intercom] tci_validation_icon</c> (left | right | highlight_only).</summary>
    public string IntercomTciValidationIcon { get; private set; } = TciValidationIconModes.Right;

    /// <summary>Сбросить Skia TCI chrome (pill placement) после load/save settings.</summary>
    public event EventHandler? IntercomTciChromeChanged;

    /// <summary>Применить <c>[intercom]</c> (плотность ленты, TCI icon) после load/save settings.</summary>
    public void ApplyIntercomPresentationSettings(IntercomSettings intercom)
    {
        IntercomComfortableFeed = intercom.UseComfortableFeedMetrics();
        IntercomTciValidationIcon = string.IsNullOrWhiteSpace(intercom.TciValidationIcon)
            ? TciValidationIconModes.Right
            : intercom.TciValidationIcon.Trim();
        IntercomTciChromeChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIntercomComfortableFeedChanged(bool value) =>
        OnPropertyChanged(nameof(IntercomTopicNavigatorVisible));
}
