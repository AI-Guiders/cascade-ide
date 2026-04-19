using Avalonia.Input;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Аккордный слой ADR 0060: корень <c>cascade_chord</c> из hotkeys.toml (по умолчанию Ctrl+K), затем цепочка без Ctrl/Alt.
/// <list type="bullet">
/// <item><b>K → M → (M|P|F)</b> — зоны кокпита: MFD / PFD / Forward (фокус редактора).</item>
/// <item><b>K → S → (P|F|D)</b> — Semantic Map: вид / уровень / детализация.</item>
/// </list>
/// </summary>
public partial class MainWindowViewModel
{
    private const double CascadeChordTimeoutSeconds = 4;

    private enum CascadeChordPhase
    {
        Idle,
        /// <summary>После Ctrl+K.</summary>
        AwaitFirstKey,
        /// <summary>После K → M: выбор зоны MFD / PFD / Forward.</summary>
        AwaitZoneKey,
        /// <summary>После K → S: параметры Semantic Map.</summary>
        AwaitSemanticMapKey
    }

    private CascadeChordPhase _cascadeChordPhase;
    private DateTimeOffset _cascadeChordDeadline = DateTimeOffset.MinValue;
    private DispatcherTimer? _cascadeChordTimer;

    /// <summary>Показать оверлей с подсказками (ADR 0060 §6) пока активна машина аккорда.</summary>
    public bool IsCascadeChordOverlayVisible => _cascadeChordPhase != CascadeChordPhase.Idle;

    /// <summary>Текст подсказки для текущего шага машины аккорда.</summary>
    public string CascadeChordOverlayHintText => BuildCascadeChordOverlayHint();

    /// <summary>Сводка настроек Semantic Map (без ComboBox; смена через Ctrl+K → S → P/F/D или палитру).</summary>
    public string SemanticMapSettingsSummaryLine =>
        $"Вид: {SemanticMapPresentation} · уровень: {SemanticMapLevel} · детализация: {_settings.SemanticMap.DetailLevel.Trim()} · Ctrl+K → S → P / F / D";

    private string BuildCascadeChordOverlayHint()
    {
        var timeout = (int)CascadeChordTimeoutSeconds;
        return _cascadeChordPhase switch
        {
            CascadeChordPhase.AwaitFirstKey =>
                "CascadeChord · шаг 1 (без Ctrl/Alt):\n" +
                "  M — зоны кокпита → далее M / P / F (MFD / PFD / Forward)\n" +
                "  S — Semantic Map → далее P / F / D (вид / уровень / детализация)\n" +
                "  Esc — отмена · таймаут " + timeout + " с\n" +
                "Палитра Ctrl+Q — полный каталог.",
            CascadeChordPhase.AwaitZoneKey =>
                "CascadeChord · зона кокпита (после K → M):\n" +
                "  M — MFD (развернуть регион)\n" +
                "  P — PFD (развернуть регион)\n" +
                "  F — Forward (фокус в редактор)\n" +
                "  Esc — отмена · таймаут " + timeout + " с",
            CascadeChordPhase.AwaitSemanticMapKey =>
                "CascadeChord · Semantic Map (после K → S):\n" +
                "  P — вид (list → graph → both)\n" +
                "  F — уровень (file ↔ controlFlow)\n" +
                "  D — детализация (glance → normal → inspect)\n" +
                "  Esc — отмена · таймаут " + timeout + " с",
            _ =>
                "CascadeChord\n" +
                "  Esc — отмена · таймаут " + timeout + " с"
        };
    }

    private void EnsureCascadeChordTimer()
    {
        if (_cascadeChordTimer is not null)
            return;
        _cascadeChordTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(CascadeChordTimeoutSeconds)
        };
        _cascadeChordTimer.Tick += (_, _) =>
        {
            _cascadeChordTimer.Stop();
            if (_cascadeChordPhase == CascadeChordPhase.Idle)
                return;
            _cascadeChordPhase = CascadeChordPhase.Idle;
            NotifyCascadeChordOverlayProperties();
        };
    }

    private void RestartCascadeChordTimer()
    {
        EnsureCascadeChordTimer();
        _cascadeChordTimer!.Stop();
        _cascadeChordTimer.Start();
    }

    private void StopCascadeChordTimer()
    {
        _cascadeChordTimer?.Stop();
    }

    private void NotifyCascadeChordOverlayProperties()
    {
        OnPropertyChanged(nameof(IsCascadeChordOverlayVisible));
        OnPropertyChanged(nameof(CascadeChordOverlayHintText));
    }

    /// <summary>
    /// Обрабатывает аккорд Cascade: возвращает <see langword="true"/>, если событие поглощено (в т.ч. корень Ctrl+K).
    /// Вызывать из tunnel <see cref="Views.MainWindow"/> до <see cref="MainWindowHotkeyService.TryHandleTunnelShortcuts"/>.
    /// </summary>
    public bool TryConsumeCascadeChordKeyDown(KeyEventArgs e)
    {
        var map = MainWindowHotkeyService.GetMergedMap();
        var rootGesture = CascadeChordHotkey.ResolveRootGesture(map);

        if (CascadeChordHotkey.RootGestureMatches(rootGesture, e))
        {
            _cascadeChordPhase = CascadeChordPhase.AwaitFirstKey;
            _cascadeChordDeadline = DateTimeOffset.UtcNow.AddSeconds(CascadeChordTimeoutSeconds);
            RestartCascadeChordTimer();
            NotifyCascadeChordOverlayProperties();
            e.Handled = true;
            return true;
        }

        if (_cascadeChordPhase == CascadeChordPhase.Idle)
            return false;

        if (DateTimeOffset.UtcNow > _cascadeChordDeadline)
        {
            _cascadeChordPhase = CascadeChordPhase.Idle;
            StopCascadeChordTimer();
            NotifyCascadeChordOverlayProperties();
            return false;
        }

        if (e.Key == Key.Escape)
        {
            _cascadeChordPhase = CascadeChordPhase.Idle;
            StopCascadeChordTimer();
            NotifyCascadeChordOverlayProperties();
            e.Handled = true;
            return true;
        }

        var mods = e.KeyModifiers;
        if (mods.HasFlag(KeyModifiers.Control) || mods.HasFlag(KeyModifiers.Alt) || mods.HasFlag(KeyModifiers.Meta))
        {
            _cascadeChordPhase = CascadeChordPhase.Idle;
            StopCascadeChordTimer();
            NotifyCascadeChordOverlayProperties();
            return false;
        }

        switch (_cascadeChordPhase)
        {
            case CascadeChordPhase.AwaitFirstKey:
                switch (e.Key)
                {
                    case Key.M:
                        _cascadeChordPhase = CascadeChordPhase.AwaitZoneKey;
                        break;
                    case Key.S:
                        _cascadeChordPhase = CascadeChordPhase.AwaitSemanticMapKey;
                        break;
                    default:
                        _cascadeChordPhase = CascadeChordPhase.Idle;
                        StopCascadeChordTimer();
                        NotifyCascadeChordOverlayProperties();
                        return false;
                }

                _cascadeChordDeadline = DateTimeOffset.UtcNow.AddSeconds(CascadeChordTimeoutSeconds);
                RestartCascadeChordTimer();
                NotifyCascadeChordOverlayProperties();
                e.Handled = true;
                return true;

            case CascadeChordPhase.AwaitZoneKey:
                switch (e.Key)
                {
                    case Key.M:
                        ApplyMfdRegionExpanded(true);
                        break;
                    case Key.P:
                        ApplyPfdRegionExpanded(true);
                        break;
                    case Key.F:
                        ((IIdeMcpActions)this).FocusEditor();
                        break;
                    default:
                        _cascadeChordPhase = CascadeChordPhase.Idle;
                        StopCascadeChordTimer();
                        NotifyCascadeChordOverlayProperties();
                        return false;
                }

                _cascadeChordPhase = CascadeChordPhase.Idle;
                StopCascadeChordTimer();
                NotifyCascadeChordOverlayProperties();
                e.Handled = true;
                return true;

            case CascadeChordPhase.AwaitSemanticMapKey:
                switch (e.Key)
                {
                    case Key.P:
                        CycleSemanticMapPresentation();
                        break;
                    case Key.F:
                        CycleSemanticMapLevel();
                        break;
                    case Key.D:
                        CycleSemanticMapDetailLevel();
                        break;
                    default:
                        _cascadeChordPhase = CascadeChordPhase.Idle;
                        StopCascadeChordTimer();
                        NotifyCascadeChordOverlayProperties();
                        return false;
                }

                _cascadeChordPhase = CascadeChordPhase.Idle;
                StopCascadeChordTimer();
                NotifyCascadeChordOverlayProperties();
                e.Handled = true;
                return true;

            default:
                return false;
        }
    }

    /// <summary>Команда палитры / MCP: list → graph → both.</summary>
    public void CycleSemanticMapPresentation()
    {
        var order = new[]
        {
            SemanticMapPresentationKind.List,
            SemanticMapPresentationKind.Graph,
            SemanticMapPresentationKind.Both
        };
        var cur = SemanticMapPresentationKind.Normalize(SemanticMapPresentation);
        var i = Array.IndexOf(order, cur);
        if (i < 0)
            i = 0;
        SemanticMapPresentation = order[(i + 1) % order.Length];
    }

    /// <summary>Команда палитры / MCP: file ↔ controlFlow.</summary>
    public void CycleSemanticMapLevel()
    {
        SemanticMapLevel = string.Equals(SemanticMapLevel, SemanticMapLevelKind.File, StringComparison.OrdinalIgnoreCase)
            ? SemanticMapLevelKind.ControlFlow
            : SemanticMapLevelKind.File;
    }

    /// <summary>Команда палитры / MCP: glance → normal → inspect.</summary>
    public void CycleSemanticMapDetailLevel()
    {
        var cur = _settings.SemanticMap.NormalizedDetailLevel;
        var next = cur switch
        {
            SemanticMapDetailLevel.Glance => SemanticMapDetailLevel.Normal,
            SemanticMapDetailLevel.Normal => SemanticMapDetailLevel.Inspect,
            SemanticMapDetailLevel.Inspect => SemanticMapDetailLevel.Glance,
            _ => SemanticMapDetailLevel.Normal
        };
        _settings.SemanticMap.DetailLevel = next switch
        {
            SemanticMapDetailLevel.Glance => "glance",
            SemanticMapDetailLevel.Normal => "normal",
            SemanticMapDetailLevel.Inspect => "inspect",
            _ => "normal"
        };
        SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
        OnPropertyChanged(nameof(SemanticMapSettingsSummaryLine));
    }
}
