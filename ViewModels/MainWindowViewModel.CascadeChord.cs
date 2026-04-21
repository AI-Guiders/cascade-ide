using System.Linq;
using Avalonia.Input;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Аккордный слой ADR 0060: корень <c>cascade_chord</c> из hotkeys.toml (по умолчанию Ctrl+K), затем <b>тот же хвост мелодии</b>, что после <c>c:</c> в палитре (см. <see cref="IntentMelodyAliases"/>), без префикса <c>c:</c> и без Enter — если alias однозначен (например <c>so</c>).
/// При конфликте префиксов (например <c>gs</c> vs <c>gsu</c>) точное совпадение после полного ввода или по клавише Enter.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>Таймаут ожидания следующей клавиши без Ctrl/Alt (секунды; лампа и оверлей).</summary>
    private const double CascadeChordTimeoutSeconds = 8;

    private enum CascadeChordPhase
    {
        Idle,
        /// <summary>После Ctrl+K — набор буквенно-цифрового хвоста как у <c>c:</c>.</summary>
        AwaitMelodyTail
    }

    private CascadeChordPhase _cascadeChordPhase;
    /// <summary>Нормализованный хвост мелодии (нижний регистр), как после <c>c:</c>.</summary>
    private string _cascadeChordMelodyTail = "";
    private DateTimeOffset _cascadeChordDeadline = DateTimeOffset.MinValue;
    private DispatcherTimer? _cascadeChordTimer;

    /// <summary>Показать оверлей с подсказками (ADR 0060 §6) пока активна машина аккорда.</summary>
    public bool IsCascadeChordOverlayVisible => _cascadeChordPhase != CascadeChordPhase.Idle;

    /// <summary>Текст подсказки для текущего шага машины аккорда.</summary>
    public string CascadeChordOverlayHintText => BuildCascadeChordOverlayHint();

    /// <summary>До двух команд, подходящих под текущий префикс мелодии (компактная полоса сверху).</summary>
    public IReadOnlyList<CascadeChordOverlaySuggestion> CascadeChordOverlaySuggestions =>
        BuildCascadeChordSuggestionRows(_cascadeChordPhase, _cascadeChordMelodyTail);

    /// <summary>Набранный хвост (пусто — «…»).</summary>
    public string CascadeChordOverlayBufferLine =>
        _cascadeChordPhase != CascadeChordPhase.AwaitMelodyTail
            ? ""
            : string.IsNullOrEmpty(_cascadeChordMelodyTail)
                ? "…"
                : "«" + _cascadeChordMelodyTail + "»";

    /// <summary>Нижняя строка подсказки в полосе.</summary>
    public string CascadeChordOverlayCompactFooter =>
        "Esc — отмена · таймаут " + (int)CascadeChordTimeoutSeconds + " с · Ctrl+Q — палитра";

    /// <summary>При вводе неверного префикса до сброса (редко видно — машина часто сразу выходит).</summary>
    public bool CascadeChordOverlayNoMatches =>
        _cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail
        && !string.IsNullOrEmpty(_cascadeChordMelodyTail)
        && IntentMelodyAliases.FilterByTailPrefix(_cascadeChordMelodyTail).Count == 0;

    /// <summary>Подсказка для лампы Command на тулбаре: в покое — кратко; при armed — полный контекст аккорда.</summary>
    public string CommandArmedLampToolTip =>
        IsCascadeChordOverlayVisible
            ? "Command (armed) — ввод по аккорду; транспорт CascadeChord (Ctrl+K).\n\n" + BuildCascadeChordOverlayHint()
            : "Command: в покое. Ctrl+K — режим armed (CascadeChord).";

    /// <summary>Сводка настроек Semantic Map (без ComboBox; смена через палитру или MCP).</summary>
    public string SemanticMapSettingsSummaryLine =>
        $"Вид: {SemanticMapPresentation} · уровень: {SemanticMapLevel} · детализация: {_settings.SemanticMap.DetailLevel.Trim()} · палитра / MCP";

    private string BuildCascadeChordOverlayHint()
    {
        var timeout = (int)CascadeChordTimeoutSeconds;
        var buf = _cascadeChordMelodyTail;
        var bufLine = string.IsNullOrEmpty(buf)
            ? "Набрано: (пусто) — тот же хвост, что после c: в палитре (например cps, cs, so)."
            : $"Набрано: «{buf}»";

        if (_cascadeChordPhase != CascadeChordPhase.AwaitMelodyTail)
        {
            return "CascadeChord\n" +
                   "  Esc — отмена · таймаут " + timeout + " с";
        }

        var matches = IntentMelodyAliases.FilterByTailPrefix(buf);
        var matchLine = matches.Count == 0
            ? "Нет alias с таким префиксом."
            : "Совпадения: " + string.Join(", ", matches.Select(m => m.Alias));

        return "CascadeChord · мелодия (как c:… без префикса и без Enter, если alias однозначен)\n" +
               bufLine + "\n" +
               matchLine + "\n" +
               "  Enter — выполнить, если хвост — точный alias (нужно при gs vs gsu)\n" +
               "  Backspace — стереть символ · Esc — отмена · таймаут " + timeout + " с\n" +
               "Палитра Ctrl+Q, c: — тот же каталог.";
    }

    private static IReadOnlyList<CascadeChordOverlaySuggestion> BuildCascadeChordSuggestionRows(
        CascadeChordPhase phase,
        string tailNormalized)
    {
        if (phase != CascadeChordPhase.AwaitMelodyTail)
            return [];

        var matches = IntentMelodyAliases.FilterByTailPrefix(tailNormalized);
        return matches
            .Take(2)
            .Select(m => new CascadeChordOverlaySuggestion(
                m.Alias,
                TruncateChordTitle(IdeCommandDocDisplay.ShortTitleForCommandId(m.CommandId))))
            .ToList();
    }

    private static string TruncateChordTitle(string s, int maxChars = 52)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        s = s.Trim();
        return s.Length <= maxChars ? s : s[..(maxChars - 1)] + "…";
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
            EndCascadeChordIdle();
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
        OnPropertyChanged(nameof(CascadeChordOverlaySuggestions));
        OnPropertyChanged(nameof(CascadeChordOverlayBufferLine));
        OnPropertyChanged(nameof(CascadeChordOverlayNoMatches));
        OnPropertyChanged(nameof(CascadeChordOverlayCompactFooter));
        OnPropertyChanged(nameof(CommandArmedLampToolTip));
    }

    private void BeginCascadeChordRoot()
    {
        _cascadeChordPhase = CascadeChordPhase.AwaitMelodyTail;
        _cascadeChordMelodyTail = "";
        _cascadeChordDeadline = DateTimeOffset.UtcNow.AddSeconds(CascadeChordTimeoutSeconds);
        RestartCascadeChordTimer();
        NotifyCascadeChordOverlayProperties();
    }

    /// <summary>Корень аккорда (hotkeys.toml <c>cascade_chord</c>): обрабатывается tunnel KeyDown главного окна (как палитра Ctrl+Q).</summary>
    [RelayCommand]
    private void CascadeChordRoot() => BeginCascadeChordRoot();

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
            BeginCascadeChordRoot();
            e.Handled = true;
            return true;
        }

        if (_cascadeChordPhase == CascadeChordPhase.Idle)
            return false;

        if (DateTimeOffset.UtcNow > _cascadeChordDeadline)
        {
            EndCascadeChordIdle();
            return false;
        }

        if (e.Key == Key.Escape)
        {
            EndCascadeChordIdle();
            e.Handled = true;
            return true;
        }

        var mods = e.KeyModifiers;
        if (mods.HasFlag(KeyModifiers.Control) || mods.HasFlag(KeyModifiers.Alt) || mods.HasFlag(KeyModifiers.Meta))
        {
            EndCascadeChordIdle();
            return false;
        }

        if (_cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail)
            return HandleCascadeChordMelodyKeyDown(e);

        return false;
    }

    private bool HandleCascadeChordMelodyKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var cmdEnter = IntentMelodyAliases.TryResolveExactCommandId(_cascadeChordMelodyTail);
            if (cmdEnter != null)
                _ = ExecuteCascadeChordCommandAsync(cmdEnter);
            EndCascadeChordIdle();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Back)
        {
            if (_cascadeChordMelodyTail.Length > 0)
                _cascadeChordMelodyTail = _cascadeChordMelodyTail[..^1];
            else
                EndCascadeChordIdle();
            _cascadeChordDeadline = DateTimeOffset.UtcNow.AddSeconds(CascadeChordTimeoutSeconds);
            RestartCascadeChordTimer();
            NotifyCascadeChordOverlayProperties();
            e.Handled = true;
            return true;
        }

        if (!TryMapMelodyKeyToChar(e.Key, out var ch))
        {
            EndCascadeChordIdle();
            e.Handled = true;
            return true;
        }

        var newTail = _cascadeChordMelodyTail + ch;
        var exact = IntentMelodyAliases.TryResolveExactCommandId(newTail);
        var hasLonger = IntentMelodyAliases.HasStrictLongerAliasPrefix(newTail);
        if (exact != null && !hasLonger)
        {
            _ = ExecuteCascadeChordCommandAsync(exact);
            EndCascadeChordIdle();
            e.Handled = true;
            return true;
        }

        if (IntentMelodyAliases.FilterByTailPrefix(newTail).Count == 0)
        {
            EndCascadeChordIdle();
            e.Handled = true;
            return true;
        }

        _cascadeChordMelodyTail = newTail;
        _cascadeChordDeadline = DateTimeOffset.UtcNow.AddSeconds(CascadeChordTimeoutSeconds);
        RestartCascadeChordTimer();
        NotifyCascadeChordOverlayProperties();
        e.Handled = true;
        return true;
    }

    private void EndCascadeChordIdle()
    {
        _cascadeChordPhase = CascadeChordPhase.Idle;
        _cascadeChordMelodyTail = "";
        StopCascadeChordTimer();
        NotifyCascadeChordOverlayProperties();
    }

    private static bool TryMapMelodyKeyToChar(Key key, out char ch)
    {
        ch = default;
        if (key >= Key.A && key <= Key.Z)
        {
            ch = (char)('a' + (key - Key.A));
            return true;
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            ch = (char)('0' + (key - Key.D0));
            return true;
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            ch = (char)('0' + (key - Key.NumPad0));
            return true;
        }

        return false;
    }

    private async Task ExecuteCascadeChordCommandAsync(string commandId)
    {
        try
        {
            await ((Services.IIdeMcpActions)this).ExecuteCommandAsync(commandId, null, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Исключения из хендлеров команд логируются внутри исполнителя; аккорд уже сброшен.
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
