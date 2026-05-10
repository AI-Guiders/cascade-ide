using CascadeIDE.Features.WebAiPortal.Application;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Страница MFD «веб-портал» (ADR 0108): URL, результат последнего вызова моста, <see cref="WebAiPortalCommandBridge"/>.</summary>
public partial class MainWindowViewModel
{
    private readonly WebAiPortalCommandBridge _webAiPortalBridge;

    /// <summary>Мост <c>invokeCSharpAction</c> → IdeCommands (whitelist).</summary>
    public WebAiPortalCommandBridge WebAiPortalBridge => _webAiPortalBridge;

    [ObservableProperty]
    private string _webAiPortalUrlText = "about:blank";

    [ObservableProperty]
    private string _webAiPortalLastBridgeResult = "";
}
