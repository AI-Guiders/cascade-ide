using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class WebAiPortalMfdPageView : UserControl
{
    private const int MaxResultChars = 2_000;

    public WebAiPortalMfdPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (WebView is null || DataContext is not MainWindowViewModel vm)
            return;
        TryNavigate(vm.WebAiPortalUrlText);
    }

    private void OnNavigateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        TryNavigate(vm.WebAiPortalUrlText);
    }

    private void TryNavigate(string? raw)
    {
        var s = raw?.Trim();
        if (string.IsNullOrEmpty(s))
            s = "about:blank";
        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            if (DataContext is MainWindowViewModel vm)
                vm.WebAiPortalLastBridgeResult = "Некорректный URL.";
            return;
        }

        WebView?.Navigate(uri);
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
            var text = await vm.WebAiPortalBridge.ExecuteFromWebJsonAsync(body).ConfigureAwait(true);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.WebAiPortalLastBridgeResult = TruncateForUi("← bridge: " + text);
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.WebAiPortalLastBridgeResult = "← bridge error: " + ex.Message;
            });
        }
    }

    private static string TruncateForUi(string s) =>
        s.Length <= MaxResultChars ? s : s[..MaxResultChars] + "…";
}
