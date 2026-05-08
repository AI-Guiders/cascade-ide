using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.ViewModels;

/// <summary>Связка с Workspace Health.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Свойства, читающие кэш после <see cref="RebuildIdeHealth"/>; держать в одном месте с циклом notify.</summary>
    private static readonly string[] IdeHealthRebuildPresentationNames =
    [
        nameof(IdeHealthMountPayload),
        nameof(PfdIdeHealthMountContext),
        nameof(MfdIdeHealthMountContext),
        nameof(IdeHealthBuildText),
        nameof(IdeHealthBuildCockpitShort),
        nameof(IdeHealthTestsText),
        nameof(IdeHealthTestsCockpitShort),
        nameof(IdeHealthDebugText),
        nameof(IdeHealthDebugCockpitShort),
    ];

    private bool _inIdeHealthRebuild;
    private IdeHealthInputSnapshot? _lastIdeHealthInputSnapshot;
    private IdeHealthStatusMountPayload? _lastIdeHealthMountPayload;

    /// <summary>Упорядоченные сегменты для <see cref="Views.WorkspaceHealthStripView"/> (поверхность); строит <see cref="IIdeHealthSurfaceCompositor"/> из снимка канала (ADR 0036 п.1→п.3).</summary>
    public ObservableCollection<IdeHealthSegment> IdeHealthSegments { get; } = new();

    /// <summary>Сид DataBus + отображаемых строк git до первого <see cref="RefreshGitSummaryAsync"/> (ADR 0099).</summary>
    private void SeedIdeHealthDataBus()
    {
        _ideDataBus.Publish(new StartupProjectPathChanged(StartupProjectCsprojFullPath));
        _ideDataBus.Publish(new GitStateChanged(Chrome.WorkspaceHealthGitText, Chrome.WorkspaceHealthGitCockpitShort));
        _ideDataBus.Publish(new IdeHostStateChanged(
            CSharpLspProcessActive: false,
            MarkdownLspProcessActive: false,
            CSharpLspHostPresent: false,
            MarkdownLspHostPresent: false));
    }

    /// <summary>Каноническое LSP-состояние C#/MD для DataBus, IDE Health (страт C) и ER; только с UI thread.</summary>
    private IdeHostStateChanged CaptureIdeHostLspState() =>
        new(
            CSharpLspProcessActive: _csharpLspHost is { IsActive: true },
            MarkdownLspProcessActive: _markdownLspHost is { IsActive: true },
            CSharpLspHostPresent: _csharpLspHost != null,
            MarkdownLspHostPresent: _markdownLspHost != null);

    /// <summary>Публикация в IDE DataBus и сразу <see cref="RebuildIdeHealth"/>; вызывать с UI thread (обработчики <see cref="IdeHealthSnapshotUnit"/> согласованы с <see cref="RebuildIdeHealth"/>, ADR 0099).</summary>
    private void PublishToIdeDataBusAndRebuild<T>(T evt)
    {
        _ideDataBus.Publish(evt);
        RebuildIdeHealth();
    }

    /// <summary>Состояние сборки на шину после await с <c>ConfigureAwait(false)</c>; гарантирует пересбор полосы IDE Health на UI-потоке.</summary>
    private Task PublishIdeBuildStateOnUiAsync(BuildStateChanged e) =>
        UiScheduler.Default.InvokeAsync(() => PublishToIdeDataBusAndRebuild(e));

    /// <summary>LSP → шина + пересбор полосы; вызывать с UI thread.</summary>
    private void PublishIdeHostLspToDataBusAndRebuild() =>
        PublishToIdeDataBusAndRebuild(CaptureIdeHostLspState());

    /// <summary>Один вход для git → шина + пересбор полосы (без дублирования по <see cref="UiChromeViewModel.WorkspaceHealthGitText"/>).</summary>
    private void PublishGitToIdeDataBusAndRebuildIdeHealth() =>
        PublishToIdeDataBusAndRebuild(
            new GitStateChanged(Chrome.WorkspaceHealthGitText, Chrome.WorkspaceHealthGitCockpitShort));

    /// <summary>Освободить подписки канала (при закрытии окна). Идемпотентно.</summary>
    public void ReleaseWorkspaceHealthChannel()
    {
        if (_workspaceHealth is IDisposable d)
            d.Dispose();
    }

    private void RebuildIdeHealth()
    {
        if (_inIdeHealthRebuild)
            return;
        _inIdeHealthRebuild = true;
        try
        {
            var snapshot = _workspaceHealth.Build(IdeHealthChannelContext.Default);
            _lastIdeHealthInputSnapshot = snapshot;
            _lastIdeHealthMountPayload = new IdeHealthStatusMountPayload(
                snapshot.Solution.Build.CockpitShort,
                snapshot.Solution.Tests.CockpitShort,
                snapshot.Solution.Debug.CockpitShort,
                SafetyLevel);
            _workspaceHealthSurfaceCompositor.Compose(
                IdeHealthSegments,
                snapshot,
                new IdeHealthSurfaceDecision(Enabled: true));
            foreach (var name in IdeHealthRebuildPresentationNames)
                OnPropertyChanged(name);
        }
        finally
        {
            _inIdeHealthRebuild = false;
        }
    }
}
