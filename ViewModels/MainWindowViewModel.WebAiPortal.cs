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

    /// <summary>После ответа моста копировать полный текст в системный буфер (удобная вставка в веб-чат).</summary>
    [ObservableProperty]
    private bool _webAiPortalCopyFullBridgeResultToClipboard = true;

    /// <summary>Пробовать дописать ответ IDE в активное поле страницы (textarea/input/contenteditable; эвристика).</summary>
    [ObservableProperty]
    private bool _webAiPortalTryInjectResultIntoFocusedComposer;

    /// <summary>Если ответ моста длинный — подмешивать компакт с HCI/<c>json-cascade</c>, а не сырой JSON (лимиты веб-чата).</summary>
    [ObservableProperty]
    private bool _webAiPortalPreferCompactComposerForChat = true;

    /// <summary>
    /// Авто-цикл ADR 0108: периодически искать последнюю <c>json-cascade</c> в DOM и выполнять мост без кнопки и без буфера обмена.
    /// Требует «Принял политику» и «Мост включён». Отправка сообщения на стороне сайта — по-прежнему задача веб-UI (IDE не жмёт Send).
    /// </summary>
    [ObservableProperty]
    private bool _webAiPortalAutoDomBridgePolling;
}
