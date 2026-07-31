using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

/// <summary>IDE Health strip / EICAS adjacency / cockpit short lines / Skia mount contexts.</summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Дублирующая карточка IDE Health на вкладке «Терминал» в Power. Пока видна полоса <see cref="WorkspaceHealthStripView"/> под редактором —
    /// false, чтобы DockPanel не отдавал высоту дублю и не схлопывал область вывода консоли.
    /// </summary>
    public bool IdeHealthOnTerminalTab =>
        MainWindowPresentationCapabilitiesProjection.IdeHealthOnTerminalTab(Capabilities, ShowIdeHealthStrip);

    /// <summary>Куда вести полосу IDE Health: нижняя полоса или страница зоны — из capabilities (<c>ide_health_surface</c>).</summary>
    public IdeHealthUiSurface IdeHealthStripSurface => Capabilities.IdeHealthSurface;

    /// <summary>Форма представления канала IDE Health на оси <see cref="ContentRepresentation"/> (ADR 0063).</summary>
    public ContentRepresentation IdeHealthContentRepresentation => Capabilities.IdeHealthContentRepresentation;

    /// <summary>Полоска build/tests/debug/git — при <c>ide_health_strip</c> и <c>bottom_strip</c>; рисуется в <see cref="Views.WorkspaceChromeBandView"/> внутри MFD.</summary>
    public bool ShowIdeHealthStrip =>
        MainWindowPresentationCapabilitiesProjection.ShowIdeHealthStrip(Capabilities);

    /// <summary>IDE Health на странице оболочки Mfd (вместо нижней полосы) — при <c>ide_health_strip</c> и <c>ide_health_surface = dedicated_page</c> (v1 — колонка зоны Mfd).</summary>
    public bool ShowIdeHealthMfdPage =>
        MainWindowPresentationCapabilitiesProjection.ShowIdeHealthMfdPage(Capabilities);

    /// <summary>
    /// Полоса оповещений EICAS v1 (над полосой Workspace Health). Видно при <c>eicas_alerts_bar</c> и непустом списке (Dark Cockpit).
    /// Отдельный контур от build/tests/debug/git (ADR 0021 §5; словарь §1.1).
    /// </summary>
    public bool ShowEicasAlertsBar =>
        MainWindowPresentationCapabilitiesProjection.ShowEicasAlertsBar(Capabilities, EicasMessages.Count);

    /// <summary>Зона под чатом в MFD: полоса EICAS / IDE Health и/или док (терминал, сборка, Problems, Git, инструменты).</summary>
    public bool ShowWorkspaceBottomChrome =>
        MainWindowPresentationCapabilitiesProjection.ShowWorkspaceBottomChrome(
            ShowIdeHealthStrip,
            ShowEicasAlertsBar,
            IsMfdContourContentVisible);

    /// <summary>Строки из канала IDE Health (один снимок на <see cref="MainWindowViewModel.RebuildIdeHealth"/>, без повторного <c>Build()</c> в геттерах).</summary>
    public string IdeHealthBuildText =>
        IdeHealthStripPresentationProjection.SolutionBuildLineText(_lastIdeHealthInputSnapshot);

    /// <summary>Короткий статус для «кольца» сборки в Power cockpit.</summary>
    public string IdeHealthBuildCockpitShort =>
        IdeHealthStripPresentationProjection.SolutionBuildCockpitShort(_lastIdeHealthInputSnapshot);

    public string IdeHealthTestsText =>
        IdeHealthStripPresentationProjection.SolutionTestsLineText(_lastIdeHealthInputSnapshot);

    /// <summary>Компактная строка тестов для полосы Power.</summary>
    public string IdeHealthTestsCockpitShort =>
        IdeHealthStripPresentationProjection.SolutionTestsCockpitShort(_lastIdeHealthInputSnapshot);

    /// <summary>Есть активная DAP-сессия (режим отладки, как в VS).</summary>
    public bool HasDebugSession => _dapDebug.HasActiveSession;

    /// <summary>Выполнение остановлено — доступны шаги и просмотр стека.</summary>
    public bool IsDebugExecutionPaused =>
        MainWindowPresentationDapProjection.IsDebugExecutionPaused(
            _dapDebug.HasActiveSession,
            _dapDebug.IsExecutionStopped);

    /// <summary>Процесс запущен под отладчиком, выполнение идёт.</summary>
    public bool IsDebugExecutionRunning =>
        MainWindowPresentationDapProjection.IsDebugExecutionRunning(
            _dapDebug.HasActiveSession,
            _dapDebug.IsExecutionStopped);

    public string IdeHealthDebugText =>
        IdeHealthStripPresentationProjection.SolutionDebugLineText(_lastIdeHealthInputSnapshot);

    /// <summary>Короткий статус отладки для Power.</summary>
    public string IdeHealthDebugCockpitShort =>
        IdeHealthStripPresentationProjection.SolutionDebugCockpitShort(_lastIdeHealthInputSnapshot);

    /// <summary>Снимок для Skia mount — тот же тик, что <see cref="IdeHealthBuildCockpitShort"/>; обновляется в <see cref="MainWindowViewModel.RebuildIdeHealth"/>.</summary>
    public IdeHealthStatusMountPayload IdeHealthMountPayload =>
        _lastIdeHealthMountPayload ?? new IdeHealthStatusMountPayload("", "", "", SafetyLevel);

    public bool IsPfdIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleInDockedColumn(
            UseSkiaInstrumentMount,
            IsPfdColumnVisible);

    public bool IsMfdIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleInDockedColumn(
            UseSkiaInstrumentMount,
            IsMfdColumnVisible);

    public bool IsMfdHostWindowIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleForHostWindow(
            UseSkiaInstrumentMount,
            IsMfdHostWindowShellOpen);

    public bool IsPfdHostWindowIdeHealthMountVisible =>
        MainWindowPresentationSurfaceProjection.IsIdeHealthSkiaMountVisibleForHostWindow(
            UseSkiaInstrumentMount,
            IsPfdHostWindowShellOpen);

    public IdeHealthStatusMountContext? PfdIdeHealthMountContext =>
        MainWindowPresentationSurfaceProjection.ResolvePfdIdeHealthMountContext(
            UseSkiaInstrumentMount,
            IsPfdHostWindowShellOpen,
            IsPfdColumnVisible,
            _instrumentMountPolicyResolver,
            _settings.Display,
            MainWindowPresentationSurfaceProjection.MountPolicySurfaceId(ActiveAttentionLayoutSurface),
            IdeHealthMountPayload);

    public IdeHealthStatusMountContext? MfdIdeHealthMountContext =>
        MainWindowPresentationSurfaceProjection.ResolveMfdIdeHealthMountContext(
            UseSkiaInstrumentMount,
            IsMfdHostWindowShellOpen,
            IsMfdColumnVisible,
            _instrumentMountPolicyResolver,
            _settings.Display,
            MainWindowPresentationSurfaceProjection.MountPolicySurfaceId(ActiveAttentionLayoutSurface),
            IdeHealthMountPayload);
}
