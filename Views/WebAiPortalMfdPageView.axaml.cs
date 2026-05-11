using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CascadeIDE.Features.WebAiPortal.Application;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class WebAiPortalMfdPageView : UserControl
{
    private const int MaxResultChars = 2_000;
    private const int AutoDomBridgePollMs = 1_100;

    private readonly DispatcherTimer _autoDomBridgeTimer =
        new() { Interval = TimeSpan.FromMilliseconds(AutoDomBridgePollMs) };

    private int _autoDomBridgeInFlight;
    private string? _lastAutoDomBridgeDedupKey;

    public WebAiPortalMfdPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _autoDomBridgeTimer.Tick += (_, _) => OnAutoDomBridgeTimerTick();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (WebView is null || DataContext is not MainWindowViewModel vm)
            return;
        TryNavigate(vm.WebAiPortalUrlText);
        _autoDomBridgeTimer.Start();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e) => _autoDomBridgeTimer.Stop();

    private void OnNavigateClick(object? sender, RoutedEventArgs e) => NavigateFromUrlBar();

    private void OnWebAiPortalUrlKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        NavigateFromUrlBar();
    }

    private void NavigateFromUrlBar()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        TryNavigate(vm.WebAiPortalUrlText);
    }

    private void TryNavigate(string? raw)
    {
        if (!WebAiPortalUrlNormalize.TryBuildNavigationUri(raw, out var uri, out var normalized))
        {
            if (DataContext is MainWindowViewModel vm)
                vm.WebAiPortalLastBridgeResult = "Некорректный URL.";
            return;
        }

        if (DataContext is MainWindowViewModel vmNavigate)
            vmNavigate.WebAiPortalUrlText = normalized;

        if (WebView is null)
            return;
        ArgumentNullException.ThrowIfNull(uri);
        WebView.Navigate(uri);
    }

    private async void OnExecuteJsonCascadeFromClipboardClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        string? jsonPayload = null;
        var sourceLabel = "";

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard is { } clip)
        {
            try
            {
                var raw = await clip.TryGetTextAsync().ConfigureAwait(true);
                if (!string.IsNullOrWhiteSpace(raw) &&
                    WebAiPortalBridgePayloadResolution.TryResolvePayload(raw, out var j, out var src))
                {
                    jsonPayload = j;
                    sourceLabel = src == WebAiPortalBridgePayloadResolution.PayloadSourceHint.FencedMarkdown
                        ? "буфер (json-cascade)"
                        : "буфер (JSON)";
                }
            }
            catch (Exception ex)
            {
                vm.WebAiPortalLastBridgeResult = "Буфер обмена: " + ex.Message;
                return;
            }
        }

        string? domError = null;
        if (jsonPayload is null && WebView is not null)
        {
            try
            {
                var sr = await WebView
                    .InvokeScript(WebAiPortalLastCommandDomProbe.LastPayloadProbeScriptJavaScript)
                    .ConfigureAwait(true);
                var unwrapped = WebAiPortalLastCommandDomProbe.UnwrapWrappedJsonString(sr);
                if (!string.IsNullOrWhiteSpace(unwrapped) &&
                    WebAiPortalBridgePayloadResolution.TryResolvePayload(unwrapped, out var jd, out _))
                {
                    jsonPayload = jd;
                    sourceLabel = "страница (последний блок)";
                }
            }
            catch (Exception ex)
            {
                domError = ex.Message;
            }
        }

        if (jsonPayload is null)
        {
            var parts = new List<string>
            {
                "Нет команды для моста: в буфере нет fenced json-cascade и не распознан голый JSON с command_id;",
                "на странице в pre/code не найден последний такой объект.",
            };
            if (domError is not null)
                parts.Add("Ошибка скрипта DOM: " + domError);
            vm.WebAiPortalLastBridgeResult = string.Join(" ", parts);
            return;
        }

        try
        {
            await ExecuteBridgeFromResolvedJsonAsync(vm, jsonPayload, sourceLabel).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.WebAiPortalLastBridgeResult = "← " + sourceLabel + " error: " + ex.Message;
            });
        }
    }

    private async void OnAutoDomBridgeTimerTick()
    {
        try
        {
            await TryAutoDomBridgeFromPageOnceAsync().ConfigureAwait(true);
        }
        catch
        {
            // не бомбим таймер
        }
    }

    /// <summary>
    /// Режим «hands-free» ADR 0108: не ждать кнопки и буфера — только poll DOM (дедуп по каноническому JSON).
    /// </summary>
    private async Task TryAutoDomBridgeFromPageOnceAsync()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        if (!vm.WebAiPortalAutoDomBridgePolling)
            return;
        if (WebView is null)
            return;
        if (!vm.WebAiPortalBridge.BridgeConsented || !vm.WebAiPortalBridge.BridgeArmed)
            return;
        if (Interlocked.CompareExchange(ref _autoDomBridgeInFlight, 1, 0) != 0)
            return;

        try
        {
            string? jsonPayload = null;
            try
            {
                var sr = await WebView
                    .InvokeScript(WebAiPortalLastCommandDomProbe.LastPayloadProbeScriptJavaScript)
                    .ConfigureAwait(true);
                var unwrapped = WebAiPortalLastCommandDomProbe.UnwrapWrappedJsonString(sr);
                if (!string.IsNullOrWhiteSpace(unwrapped) &&
                    WebAiPortalBridgePayloadResolution.TryResolvePayload(unwrapped, out var jd, out _))
                    jsonPayload = jd;
            }
            catch
            {
                return;
            }

            if (string.IsNullOrEmpty(jsonPayload))
                return;

            if (!WebAiPortalBridgePayloadDedup.TryCanonicalKey(jsonPayload, out var dedupKey) ||
                string.IsNullOrEmpty(dedupKey))
                return;
            if (dedupKey == _lastAutoDomBridgeDedupKey)
                return;

            _ = WebAiPortalBridgePayloadResolution.TryGetCommandId(jsonPayload, out var invokedCmdId);
            var text = await vm.WebAiPortalBridge.ExecuteFromWebJsonAsync(jsonPayload).ConfigureAwait(true);
            _lastAutoDomBridgeDedupKey = dedupKey;
            await ApplyPostBridgeDispositionAsync(vm, "← страница (авто DOM): ", invokedCmdId, text).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.WebAiPortalLastBridgeResult = "← авто DOM error: " + ex.Message;
            });
        }
        finally
        {
            Interlocked.Exchange(ref _autoDomBridgeInFlight, 0);
        }
    }

    private async Task ExecuteBridgeFromResolvedJsonAsync(MainWindowViewModel vm, string jsonPayload, string sourceLabel)
    {
        _ = WebAiPortalBridgePayloadResolution.TryGetCommandId(jsonPayload, out var invokedCmdId);
        var text = await vm.WebAiPortalBridge.ExecuteFromWebJsonAsync(jsonPayload).ConfigureAwait(true);
        await ApplyPostBridgeDispositionAsync(vm, "← " + sourceLabel + ": ", invokedCmdId, text).ConfigureAwait(true);
    }

    private void OnRevokeBridgeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.WebAiPortalBridge.RevokeBridge();
            vm.WebAiPortalLastBridgeResult = "Мост отозван.";
        }
    }

    private async void OnNavigationCompletedWeb(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        _lastAutoDomBridgeDedupKey = null;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.WebAiPortalLastBridgeResult = e.IsSuccess
                    ? "Navigation completed."
                    : "Navigation failed.";
            }
        });
    }

    private async void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var body = e.Body ?? "";
        try
        {
            _ = WebAiPortalBridgePayloadResolution.TryGetCommandId(body, out var invokedCmdId);
            var text = await vm.WebAiPortalBridge.ExecuteFromWebJsonAsync(body).ConfigureAwait(true);
            await ApplyPostBridgeDispositionAsync(vm, "← bridge: ", invokedCmdId, text).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.WebAiPortalLastBridgeResult = "← bridge error: " + ex.Message;
            });
        }
    }

    /// <summary>После успешного ответа моста: текст для композитор/буфер с учётом лимита чата; опционально вставка в фокус на странице.</summary>
    private async Task ApplyPostBridgeDispositionAsync(
        MainWindowViewModel vm,
        string statusPrefix,
        string? executedCommandId,
        string fullResponseText)
    {
        var mixin = WebAiPortalChatMixInFormatter.BuildForComposer(
            vm.WebAiPortalPreferCompactComposerForChat,
            WebAiPortalChatMixInFormatter.DefaultMaxChatCharacters,
            executedCommandId,
            fullResponseText);

        string? injectHint = null;
        if (vm.WebAiPortalTryInjectResultIntoFocusedComposer && WebView is not null)
        {
            try
            {
                var script = WebAiPortalComposerInjectScript.Build(mixin.TextForComposer);
                var raw = await WebView.InvokeScript(script).ConfigureAwait(true);
                injectHint = SummarizeComposerInject(
                    WebAiPortalLastCommandDomProbe.UnwrapWrappedJsonString(raw) ?? "");
            }
            catch (Exception ex)
            {
                injectHint = ex.Message;
            }
        }

        var top = TopLevel.GetTopLevel(this);
        if (vm.WebAiPortalCopyFullBridgeResultToClipboard && top?.Clipboard is { } clip)
            await clip.SetTextAsync(mixin.TextForComposer).ConfigureAwait(true);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var summary = mixin.UsedCompactMixer
                ? $"{fullResponseText.Length} символов IDE → ниже текст для чата компактный (буфер/композитор); сырой JSON не ставили."
                : fullResponseText;
            var line = TruncateForUi(statusPrefix + summary);
            if (injectHint is not null)
                line += " · композитор: " + injectHint;
            if (vm.WebAiPortalCopyFullBridgeResultToClipboard)
                line += mixin.UsedCompactMixer ? " · буфер (компакт)" : " · буфер";
            vm.WebAiPortalLastBridgeResult = line;
        });
    }

    private static string SummarizeComposerInject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "?";
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True && root.TryGetProperty("tag", out var tag))
                return "ок " + (tag.GetString() ?? "");

            if (root.TryGetProperty("reason", out var reason))
            {
                var r = reason.GetString();
                root.TryGetProperty("tag", out var tag2);
                var t = tag2.ValueKind != JsonValueKind.Undefined ? tag2.ToString() : "";
                return string.IsNullOrEmpty(t) ? (r ?? "?") : (r ?? "?") + " " + t;
            }
        }
        catch
        {
            // строка уже обрезается статусной полосой
        }

        return raw.Length <= 64 ? raw : raw[..64] + "…";
    }

    private static string TruncateForUi(string s) =>
        s.Length <= MaxResultChars ? s : s[..MaxResultChars] + "…";
}
