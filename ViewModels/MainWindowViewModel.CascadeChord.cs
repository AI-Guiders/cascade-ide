using System.Text;
using Avalonia.Input;
using Avalonia.Threading;
using CascadeIDE.Models;
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
    /// <summary>Фокус в полоске HUD над редактором: туннель не перехватывает буквы — ввод идёт в <see cref="CascadeChordHudMelodyText"/>.</summary>
    private bool _cascadeChordHudTextHasFocus;

    /// <summary>Пользователь закрыл выпадающий список (light dismiss) — не открывать снова, пока хвост не изменится или не вернётся фокус.</summary>
    private bool _cascadeChordDropdownUserDismissed;

    /// <summary>Показать оверлей с подсказками (ADR 0060 §6) пока активна машина аккорда.</summary>
    public bool IsCascadeChordOverlayVisible => _cascadeChordPhase != CascadeChordPhase.Idle;

    /// <summary>Текст подсказки для текущего шага машины аккорда.</summary>
    public string CascadeChordOverlayHintText => BuildCascadeChordOverlayHint();

    /// <summary>Команды, подходящие под префикс (выпадающий список, до 25).</summary>
    public IReadOnlyList<CascadeChordOverlaySuggestion> CascadeChordOverlaySuggestions =>
        BuildCascadeChordSuggestionRows(_cascadeChordPhase, _cascadeChordMelodyTail, MaxChordDropdownItems);

    private const int MaxChordDropdownItems = 25;

    /// <summary>Выпадающий список под полем ввода: пока armed, есть префикс и совпадения или явное «нет совпадений».</summary>
    public bool IsCascadeChordDropdownOpen =>
        _cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail
        && !_cascadeChordDropdownUserDismissed
        && !string.IsNullOrEmpty(_cascadeChordMelodyTail)
        && (FilterCascadeChordEligibleMatches(_cascadeChordMelodyTail).Count > 0 || CascadeChordOverlayNoMatches);

    /// <summary>Есть строки в выпадающем списке (для «нет совпадений» — отдельный текст).</summary>
    public bool HasChordDropdownItems =>
        _cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail && CascadeChordOverlaySuggestions.Count > 0;

    /// <summary>Подпись поля ввода рядом с CMD: в покое и при armed.</summary>
    public string CascadeChordHudWatermark =>
        IsCascadeChordOverlayVisible
            ? "мелодия (как после c:)"
            : "Ctrl+K — команда по мелодии";

    /// <summary>Набранный хвост для визуального поля ввода в оверлее (find-bar style).</summary>
    public string CascadeChordOverlayInputText =>
        _cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail
            ? _cascadeChordMelodyTail
            : "";

    /// <summary>Двусторонняя привязка к полю ввода в полоске над редактором (только фаза armed).</summary>
    public string CascadeChordHudMelodyText
    {
        get => _cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail ? _cascadeChordMelodyTail : "";
        set
        {
            if (_cascadeChordPhase != CascadeChordPhase.AwaitMelodyTail)
                return;
            var n = NormalizeCascadeChordMelodyInput(value);
            if (string.Equals(n, _cascadeChordMelodyTail, StringComparison.Ordinal))
                return;
            ApplyCascadeChordMelodyTail(n);
        }
    }

    /// <summary>После Ctrl+K — сфокусировать поле в полоске task cockpit (см. <see cref="Views.CascadeChordHudStripView"/>).</summary>
    public event Action? CascadeChordHudFocusRequested;

    /// <summary>Одна строка для полоски: плейсхолдер при пустом хвосте или набранный хвост (без двух <c>IsVisible</c>).</summary>
    public string CascadeChordOverlayFindBarLine =>
        _cascadeChordPhase != CascadeChordPhase.AwaitMelodyTail
            ? ""
            : string.IsNullOrEmpty(_cascadeChordMelodyTail)
                ? "мелодия (как после c:) — полоска под тулбаром"
                : _cascadeChordMelodyTail;

    /// <summary>Плейсхолдер в полоске — чуть приглушить через прозрачность текста.</summary>
    public double CascadeChordOverlayFindBarOpacity =>
        _cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail && string.IsNullOrEmpty(_cascadeChordMelodyTail)
            ? 0.72
            : 1.0;

    /// <summary>Нижняя строка подсказки в полосе.</summary>
    public string CascadeChordOverlayCompactFooter =>
        "Esc — отмена · таймаут " + (int)CascadeChordTimeoutSeconds + " с · Ctrl+Q — палитра";

    /// <summary>При вводе неверного префикса до сброса (редко видно — машина часто сразу выходит).</summary>
    public bool CascadeChordOverlayNoMatches =>
        _cascadeChordPhase == CascadeChordPhase.AwaitMelodyTail
        && !string.IsNullOrEmpty(_cascadeChordMelodyTail)
        && FilterCascadeChordEligibleMatches(_cascadeChordMelodyTail).Count == 0;

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

        var matches = FilterCascadeChordEligibleMatches(buf);
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
        string tailNormalized,
        int maxItems)
    {
        if (phase != CascadeChordPhase.AwaitMelodyTail)
            return [];

        var matches = FilterCascadeChordEligibleMatches(tailNormalized);
        return matches
            .Take(maxItems)
            .Select(m => new CascadeChordOverlaySuggestion(
                m.Alias,
                TruncateChordTitle(IdeCommandDocDisplay.ShortTitleForCommandId(m.CommandId))))
            .ToList();
    }

    private static IReadOnlyList<(string Alias, string CommandId)> FilterCascadeChordEligibleMatches(string tailNormalized) =>
        IntentMelodyAliases.FilterByTailPrefix(tailNormalized)
            .Where(m => ParametricIntentMelody.IsChordEligibleAlias(m.Alias))
            .ToList();

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
        OnPropertyChanged(nameof(CascadeChordOverlayInputText));
        OnPropertyChanged(nameof(CascadeChordOverlayFindBarLine));
        OnPropertyChanged(nameof(CascadeChordOverlayFindBarOpacity));
        OnPropertyChanged(nameof(CascadeChordOverlayNoMatches));
        OnPropertyChanged(nameof(CascadeChordOverlayCompactFooter));
        OnPropertyChanged(nameof(CascadeChordHudMelodyText));
        OnPropertyChanged(nameof(IsCascadeChordDropdownOpen));
        OnPropertyChanged(nameof(HasChordDropdownItems));
        OnPropertyChanged(nameof(CascadeChordHudWatermark));
        OnPropertyChanged(nameof(CommandArmedLampToolTip));
    }

    /// <summary>Вызывается из полоски HUD: фокус ввода — туннель не должен съедать буквы до TextBox.</summary>
    public void SetCascadeChordHudTextHasFocus(bool hasFocus)
    {
        _cascadeChordHudTextHasFocus = hasFocus;
        if (hasFocus)
            _cascadeChordDropdownUserDismissed = false;
        OnPropertyChanged(nameof(IsCascadeChordDropdownOpen));
    }

    /// <summary>Popup закрыт кликом вне списка (Avalonia light dismiss).</summary>
    public void NotifyCascadeChordDropdownDismissed()
    {
        _cascadeChordDropdownUserDismissed = true;
        OnPropertyChanged(nameof(IsCascadeChordDropdownOpen));
    }

    /// <summary>Выбор строки из выпадающего списка.</summary>
    public void PickCascadeChordSuggestion(CascadeChordOverlaySuggestion? item)
    {
        if (item is null || _cascadeChordPhase != CascadeChordPhase.AwaitMelodyTail)
            return;
        ApplyCascadeChordMelodyTail(item.Alias);
    }


    /// <summary>Esc из поля ввода или снаружи (после снятия фокуса с HUD).</summary>
    public void CancelCascadeChord() => EndCascadeChordIdle();

    /// <summary>Enter в поле HUD: как в туннеле — точный alias или сброс.</summary>
    public void OnCascadeChordHudEnter()
    {
        if (_cascadeChordPhase != CascadeChordPhase.AwaitMelodyTail)
            return;
        var cmdEnter = IntentMelodyAliases.TryResolveExactCommandId(_cascadeChordMelodyTail);
        if (cmdEnter != null)
            _ = ExecuteCascadeChordCommandAsync(cmdEnter);
        EndCascadeChordIdle();
    }

    private static string NormalizeCascadeChordMelodyInput(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Общая логика хвоста: однозначное совпадение → выполнить; ноль префиксов при непустом → выход.</summary>
    private void ApplyCascadeChordMelodyTail(string newTail)
    {
        _cascadeChordDropdownUserDismissed = false;
        var matches = FilterCascadeChordEligibleMatches(newTail);
        var exact = matches.FirstOrDefault(m => string.Equals(m.Alias, newTail, StringComparison.Ordinal));
        var hasLonger = matches.Any(m => m.Alias.Length > newTail.Length);
        if (exact.CommandId != null && !hasLonger && newTail.Length > 0)
        {
            _cascadeChordMelodyTail = newTail;
            _ = ExecuteCascadeChordCommandAsync(exact.CommandId);
            EndCascadeChordIdle();
            return;
        }

        if (matches.Count == 0 && newTail.Length > 0)
        {
            EndCascadeChordIdle();
            return;
        }

        _cascadeChordMelodyTail = newTail;
        _cascadeChordDeadline = DateTimeOffset.UtcNow.AddSeconds(CascadeChordTimeoutSeconds);
        RestartCascadeChordTimer();
        NotifyCascadeChordOverlayProperties();
    }

    private void BeginCascadeChordRoot()
    {
        _cascadeChordPhase = CascadeChordPhase.AwaitMelodyTail;
        _cascadeChordMelodyTail = "";
        _cascadeChordHudTextHasFocus = false;
        _cascadeChordDropdownUserDismissed = false;
        _cascadeChordDeadline = DateTimeOffset.UtcNow.AddSeconds(CascadeChordTimeoutSeconds);
        RestartCascadeChordTimer();
        NotifyCascadeChordOverlayProperties();
        CascadeChordHudFocusRequested?.Invoke();
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

        if (_cascadeChordHudTextHasFocus)
            return false;

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
            OnCascadeChordHudEnter();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Back)
        {
            if (_cascadeChordMelodyTail.Length > 0)
                ApplyCascadeChordMelodyTail(_cascadeChordMelodyTail[..^1]);
            else
                EndCascadeChordIdle();
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
        ApplyCascadeChordMelodyTail(newTail);
        e.Handled = true;
        return true;
    }

    private void EndCascadeChordIdle()
    {
        _cascadeChordPhase = CascadeChordPhase.Idle;
        _cascadeChordMelodyTail = "";
        _cascadeChordHudTextHasFocus = false;
        _cascadeChordDropdownUserDismissed = false;
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
