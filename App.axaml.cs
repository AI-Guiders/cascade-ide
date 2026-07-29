using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;
using ModelContextProtocol.Server;

namespace CascadeIDE;

public partial class App : Application
{
    /// <summary>Запуск с MCP-сервером на stdio (агент/Cursor подключается к IDE по stdin/stdout).</summary>
    public static bool RunMcpStdio { get; set; }

    /// <summary>Agent land → GUI projector (land-LATEST.json).</summary>
    static Features.Cdp.CdpLandProjector? LandProjector { get; set; }

    /// <summary>Human GUI → agent focus latch (focus-LATEST.json).</summary>
    static Features.Cdp.CdpFocusLatchPublisher? FocusPublisher { get; set; }

    /// <summary>Agent Instant Save ↔ human Save shared dirty (disk-LATEST.json).</summary>
    static Features.Cdp.CdpDiskSyncProjector? DiskSyncProjector { get; set; }

    /// <summary>Agent desk → live presentation topology (presentation-LATEST.json).</summary>
    static Features.Cdp.CdpPresentationProjector? PresentationProjector { get; set; }

    /// <summary>Agent desk ↔ Intercom @PF/@PM voice (intercom-LATEST.json).</summary>
    static Features.Cdp.CdpIntercomVoiceProjector? IntercomVoiceProjector { get; set; }

    /// <summary>Agent/desk co-presence chrome (shared-LATEST.json).</summary>
    static Features.Cdp.CdpSharedFileProjector? SharedFileProjector { get; set; }

    /// <summary>Agent desk seats → cabin tool map (seats-LATEST.json).</summary>
    static Features.Cdp.CdpSeatsProjector? SeatsProjector { get; set; }

    /// <summary>Agent SA/alert → EICAS bar (alert-LATEST.json).</summary>
    static Features.Cdp.CdpAlertProjector? AlertProjector { get; set; }

    /// <summary>Agent eQRH suggest → EICAS advisory (qrh-LATEST.json).</summary>
    static Features.Cdp.CdpQrhProjector? QrhProjector { get; set; }

    /// <summary>Agent ECL checklist → EICAS advisory (ecl-LATEST.json).</summary>
    static Features.Cdp.CdpEclProjector? EclProjector { get; set; }

    /// <summary>Agent L1 pressure → quiet chrome (pressure-LATEST.json).</summary>
    static Features.Cdp.CdpPressureProjector? PressureProjector { get; set; }

    /// <summary>Agent AutoIgnition → quiet chrome (ignite-LATEST.json).</summary>
    static Features.Cdp.CdpIgniteProjector? IgniteProjector { get; set; }

    /// <summary>Agent Project Switch → quiet chrome (scope-LATEST.json).</summary>
    static Features.Cdp.CdpScopeProjector? ScopeProjector { get; set; }

    /// <summary>Agent sys ops → quiet chrome (sys-LATEST.json).</summary>
    static Features.Cdp.CdpSysProjector? SysProjector { get; set; }

    /// <summary>Agent onboard cold-start → quiet chrome (onboard-LATEST.json).</summary>
    static Features.Cdp.CdpOnboardProjector? OnboardProjector { get; set; }

    /// <summary>Agent arch board → quiet chrome (arch-LATEST.json).</summary>
    static Features.Cdp.CdpArchProjector? ArchProjector { get; set; }

    /// <summary>Agent MCP outlet → quiet chrome (mcp-LATEST.json).</summary>
    static Features.Cdp.CdpMcpProjector? McpProjector { get; set; }

    /// <summary>Agent Task Manager → quiet chrome (plan-LATEST.json).</summary>
    static Features.Cdp.CdpPlanProjector? PlanProjector { get; set; }

    /// <summary>Agent report board → quiet chrome (report-LATEST.json).</summary>
    static Features.Cdp.CdpReportProjector? ReportProjector { get; set; }

    /// <summary>Agent CRM callout → quiet chrome (crm-LATEST.json).</summary>
    static Features.Cdp.CdpCrmProjector? CrmProjector { get; set; }

    /// <summary>Agent webcam capture → quiet chrome (webcam-LATEST.json).</summary>
    static Features.Cdp.CdpWebcamProjector? WebcamProjector { get; set; }

    /// <summary>Agent toolchain health → quiet chrome (toolchain-LATEST.json).</summary>
    static Features.Cdp.CdpToolchainProjector? ToolchainProjector { get; set; }

    /// <summary>Agent plugins attention → quiet chrome (plugins-LATEST.json).</summary>
    static Features.Cdp.CdpPluginsProjector? PluginsProjector { get; set; }

    /// <summary><c>cide://</c> из argv при cold start (ADR 0157).</summary>
    public static string? PendingMagicLinkUri { get; set; }

    internal static IDisposable? MagicLinkPrimaryMutex { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        UiCulture.ApplyFromSettingsOrSystem();
        UiModeCatalog.Initialize();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel();
            vm.IsMcpServerMode = RunMcpStdio;
            desktop.MainWindow = new MainWindow { DataContext = vm };
            LandProjector = Features.Cdp.CdpLandProjector.Start(vm.IdeMcp);
            FocusPublisher = Features.Cdp.CdpFocusLatchPublisher.Start();
            DiskSyncProjector = Features.Cdp.CdpDiskSyncProjector.Start(vm.Documents);
            PresentationProjector = Features.Cdp.CdpPresentationProjector.Start(vm);
            IntercomVoiceProjector = Features.Cdp.CdpIntercomVoiceProjector.Start(vm);
            SharedFileProjector = Features.Cdp.CdpSharedFileProjector.Start(vm.Documents);
            SeatsProjector = Features.Cdp.CdpSeatsProjector.Start(vm);
            AlertProjector = Features.Cdp.CdpAlertProjector.Start(vm.EicasLatchFeed);
            QrhProjector = Features.Cdp.CdpQrhProjector.Start(vm.EicasLatchFeed);
            EclProjector = Features.Cdp.CdpEclProjector.Start(vm.EicasLatchFeed);
            PressureProjector = Features.Cdp.CdpPressureProjector.Start(vm);
            IgniteProjector = Features.Cdp.CdpIgniteProjector.Start(vm);
            ScopeProjector = Features.Cdp.CdpScopeProjector.Start(vm);
            SysProjector = Features.Cdp.CdpSysProjector.Start(vm);
            OnboardProjector = Features.Cdp.CdpOnboardProjector.Start(vm);
            ArchProjector = Features.Cdp.CdpArchProjector.Start(vm);
            McpProjector = Features.Cdp.CdpMcpProjector.Start(vm);
            PlanProjector = Features.Cdp.CdpPlanProjector.Start(vm);
            ReportProjector = Features.Cdp.CdpReportProjector.Start(vm);
            CrmProjector = Features.Cdp.CdpCrmProjector.Start(vm);
            WebcamProjector = Features.Cdp.CdpWebcamProjector.Start(vm);
            ToolchainProjector = Features.Cdp.CdpToolchainProjector.Start(vm);
            PluginsProjector = Features.Cdp.CdpPluginsProjector.Start(vm);
            if (RunMcpStdio)
                _ = RunMcpServerAsync(vm);
            if (!string.IsNullOrWhiteSpace(PendingMagicLinkUri))
            {
                vm.EnqueueMagicLink(PendingMagicLinkUri);
                PendingMagicLinkUri = null;
            }

            _ = vm.RefreshOllamaAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task RunMcpServerAsync(MainWindowViewModel vm)
    {
        try
        {
            var runtime = Services.IdeMcpServer.Create(vm.IdeMcp);
            await using var server = McpServer.Create(new StdioServerTransport("CascadeIDE"), runtime.Options);
            runtime.AttachServer(server);
            await server.RunAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MCP server error: {ex.Message}");
        }
    }

}
