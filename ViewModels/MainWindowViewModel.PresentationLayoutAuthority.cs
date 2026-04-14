using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>Применение <see cref="PresentationLayoutAuthority"/> к свойствам главного VM.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Согласовать желаемую видимость обозревателя с пресетом <c>presentation</c> (первый экран).</summary>
    public void ApplySolutionExplorerVisible(bool desired) =>
        IsSolutionExplorerVisible = PresentationLayoutAuthority.CoerceSolutionExplorerVisible(_presentationParse, desired);

    /// <summary>Согласовать разворот колонки Mfd с пресетом <c>presentation</c> (первый экран).</summary>
    public void ApplyChatPanelExpanded(bool desired) =>
        IsChatPanelExpanded = PresentationLayoutAuthority.CoerceChatPanelExpanded(_presentationParse, desired);
}
