#nullable enable
using Avalonia.Input;
using Avalonia.Threading;
using CascadeIDE.Models.Shell;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>
/// Сессия аккорда ADR 0060: фаза, хвост мелодии, таймер и разбор клавиш до вызова исполнения команды.
/// </summary>
public sealed class CascadeChordIntentSession
{
    /// <summary>Таймаут ожидания следующей клавиши без Ctrl/Alt (секунды; лампа и оверлей).</summary>
    public const double TimeoutSeconds = 8;

    private enum Phase
    {
        Idle,
        /// <summary>После корня — набор хвоста как у <c>c:</c> в палитре (включая параметрические <c>wai:</c>, <c>els:</c>…).</summary>
        AwaitMelodyTail
    }

    private readonly Action _notifyOverlayProperties;
    private readonly Func<string?> _getCurrentFilePath;
    private readonly Func<string?> _getEditorText;
    private readonly Func<string, string?, Task> _executeChordCommandAsync;

    private Phase _phase;
    /// <summary>Нормализованный хвост мелодии (нижний регистр), как после <c>c:</c>.</summary>
    private string _melodyTail = "";
    private DateTimeOffset _deadline = DateTimeOffset.MinValue;
    private DispatcherTimer? _timer;

    /// <summary>Пользователь закрыл выпадающий список (light dismiss).</summary>
    private bool _dropdownUserDismissed;

    public CascadeChordIntentSession(
        Action notifyOverlayProperties,
        Func<string?> getCurrentFilePath,
        Func<string?> getEditorText,
        Func<string, string?, Task> executeChordCommandAsync)
    {
        _notifyOverlayProperties = notifyOverlayProperties;
        _getCurrentFilePath = getCurrentFilePath;
        _getEditorText = getEditorText;
        _executeChordCommandAsync = executeChordCommandAsync;
    }

    private const int MaxDropdownItems = 25;

    public bool IsOverlayVisible => _phase != Phase.Idle;

    public string OverlayHintText => BuildOverlayHint();

    public IReadOnlyList<CascadeChordOverlaySuggestion> OverlaySuggestions =>
        CascadeChordPresentationProjection.BuildSuggestionRows(
            _phase == Phase.AwaitMelodyTail,
            _melodyTail,
            MaxDropdownItems);

    public bool IsDropdownOpen =>
        _phase == Phase.AwaitMelodyTail
        && !_dropdownUserDismissed
        && !string.IsNullOrEmpty(_melodyTail)
        && (CascadeChordPresentationProjection.FilterEligibleMatches(_melodyTail).Count > 0
            || OverlayNoMatches
            || ParametricIntentMelody.IsParametricChordTailPrefix(_melodyTail));

    public bool HasDropdownItems =>
        _phase == Phase.AwaitMelodyTail && OverlaySuggestions.Count > 0;

    public string HudWatermark =>
        IsOverlayVisible
            ? "мелодия (как после c:)"
            : "Ctrl+K — команда по мелодии";

    public string OverlayInputText =>
        _phase == Phase.AwaitMelodyTail
            ? _melodyTail
            : "";

    public string HudMelodyTextGet() =>
        _phase == Phase.AwaitMelodyTail ? _melodyTail : "";

    public string OverlayFindBarLine =>
        _phase != Phase.AwaitMelodyTail
            ? ""
            : string.IsNullOrEmpty(_melodyTail)
                ? "мелодия (как после c:) — полоска под тулбаром"
                : _melodyTail;

    public double OverlayFindBarOpacity =>
        _phase == Phase.AwaitMelodyTail && string.IsNullOrEmpty(_melodyTail)
            ? 0.72
            : 1.0;

    public string OverlayCompactFooter =>
        "Esc — отмена · / — Command Line · таймаут " + (int)TimeoutSeconds + " с · Ctrl+Q — палитра";

    public bool OverlayNoMatches =>
        _phase == Phase.AwaitMelodyTail
        && !string.IsNullOrEmpty(_melodyTail)
        && CascadeChordPresentationProjection.FilterEligibleMatches(_melodyTail).Count == 0
        && !ParametricIntentMelody.IsParametricChordTailPrefix(_melodyTail);

    public string CommandArmedLampToolTip =>
        !IsOverlayVisible
            ? "Command: в покое. Ctrl+K — режим armed (CascadeChord)."
            : "Command (armed) — ввод по аккорду; транспорт CascadeChord (Ctrl+K).\n\n" + BuildOverlayHint();

    private string BuildOverlayHint()
    {
        var matches = CascadeChordPresentationProjection.FilterEligibleMatches(_melodyTail);
        return CascadeChordPresentationProjection.BuildOverlayHint(
            _phase == Phase.AwaitMelodyTail,
            _melodyTail,
            (int)TimeoutSeconds,
            matches);
    }

    private void EnsureTimer()
    {
        if (_timer is not null)
            return;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(TimeoutSeconds)
        };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            if (_phase == Phase.Idle)
                return;
            EndIdle();
        };
    }

    private void RestartTimer()
    {
        EnsureTimer();
        _timer!.Stop();
        _timer.Start();
    }

    private void StopTimer() => _timer?.Stop();

    public void NotifyDropdownDismissed()
    {
        _dropdownUserDismissed = true;
        _notifyOverlayProperties();
    }

    public void PickSuggestion(CascadeChordOverlaySuggestion? item)
    {
        if (item is null || _phase != Phase.AwaitMelodyTail)
            return;
        ApplyMelodyTail(item.Alias);
    }

    public void Cancel() => EndIdle();

    /// <summary>Enter из поля HUD (view).</summary>
    public void OnHudEnterFromView() => CommitEnterFromMelodyInput();

    private void CommitEnterFromMelodyInput()
    {
        if (_phase != Phase.AwaitMelodyTail)
            return;

        var melody = _melodyTail;

        if (ParametricIntentMelody.TryResolveParametricExecution(
                melody,
                _getCurrentFilePath(),
                _getEditorText() ?? "",
                out var paramCmdId,
                out var paramArgsJson,
                out _))
            _ = ExecuteCommandFireAndForgetAsync(paramCmdId, paramArgsJson);
        else if (IntentMelodyAliases.TryResolveExactCommandId(melody) is { } plain &&
                 !ParametricIntentMelody.IsParametricMelodyBaseAlias(melody))
            _ = ExecuteCommandFireAndForgetAsync(plain, null);

        EndIdle();
    }

    private void ApplyMelodyTail(string newTail)
    {
        _dropdownUserDismissed = false;
        var matches = CascadeChordPresentationProjection.FilterEligibleMatches(newTail);
        var exact = matches.FirstOrDefault(m => string.Equals(m.Alias, newTail, StringComparison.Ordinal));
        var hasLongerAlias = IntentMelodyAliases.HasStrictLongerAliasPrefix(newTail);
        if (exact.CommandId != null &&
            !hasLongerAlias &&
            newTail.Length > 0 &&
            !ParametricIntentMelody.ChordDefersInstantExecuteForExactAlias(exact.Alias))
        {
            _melodyTail = newTail;
            _ = ExecuteCommandFireAndForgetAsync(exact.CommandId, null);
            EndIdle();
            return;
        }

        if (matches.Count == 0 && newTail.Length > 0)
        {
            if (!ParametricIntentMelody.IsParametricChordTailPrefix(newTail))
            {
                EndIdle();
                return;
            }

            _melodyTail = newTail;
            _deadline = DateTimeOffset.UtcNow.AddSeconds(TimeoutSeconds);
            RestartTimer();
            _notifyOverlayProperties();
            return;
        }

        _melodyTail = newTail;
        _deadline = DateTimeOffset.UtcNow.AddSeconds(TimeoutSeconds);
        RestartTimer();
        _notifyOverlayProperties();
    }

    /// <summary>Корень аккорда: фаза ожидания мелодии; ввод — через tunnel окна (зеркало HUD не забирает фокус).</summary>
    public void BeginRoot()
    {
        _phase = Phase.AwaitMelodyTail;
        _melodyTail = "";
        _dropdownUserDismissed = false;
        _deadline = DateTimeOffset.UtcNow.AddSeconds(TimeoutSeconds);
        RestartTimer();
        _notifyOverlayProperties();
    }

    /// <summary>
    /// Обрабатывает аккорд Cascade: возвращает <see langword="true"/>, если событие поглощено (в т.ч. корень).
    /// </summary>
    public bool TryConsumeKeyDown(KeyEventArgs e)
    {
        var map = MainWindowHotkeyService.GetMergedMap();
        var rootGesture = CascadeChordHotkey.ResolveRootGesture(map);

        if (CascadeChordHotkey.RootGestureMatches(rootGesture, e))
        {
            BeginRoot();
            e.Handled = true;
            return true;
        }

        if (_phase == Phase.Idle)
            return false;

        if (DateTimeOffset.UtcNow > _deadline)
        {
            EndIdle();
            return false;
        }

        if (e.Key == Key.Escape)
        {
            EndIdle();
            e.Handled = true;
            return true;
        }

        var mods = e.KeyModifiers;
        if (mods.HasFlag(KeyModifiers.Control) || mods.HasFlag(KeyModifiers.Alt) || mods.HasFlag(KeyModifiers.Meta))
        {
            EndIdle();
            return false;
        }

        if (_phase == Phase.AwaitMelodyTail)
            return HandleMelodyKeyDown(e);

        return false;
    }

    private bool HandleMelodyKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitEnterFromMelodyInput();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Back)
        {
            if (_melodyTail.Length > 0)
                ApplyMelodyTail(_melodyTail[..^1]);
            else
                EndIdle();
            e.Handled = true;
            return true;
        }

        if (!CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(e.Key, e.KeyModifiers, e.PhysicalKey, out var ch))
        {
            EndIdle();
            e.Handled = true;
            return true;
        }

        if (_melodyTail.Length == 0 && ch == '/')
        {
            _ = ExecuteCommandFireAndForgetAsync(IdeCommands.CockpitOpenCommandLine, """{"initial_text":"/"}""");
            EndIdle();
            e.Handled = true;
            return true;
        }

        var newTail = _melodyTail + char.ToLowerInvariant(ch);
        ApplyMelodyTail(newTail);
        e.Handled = true;
        return true;
    }

    public void EndIdle()
    {
        _phase = Phase.Idle;
        _melodyTail = "";
        _dropdownUserDismissed = false;
        StopTimer();
        _notifyOverlayProperties();
    }

    private async Task ExecuteCommandFireAndForgetAsync(string commandId, string? argsJson)
    {
        try
        {
            await _executeChordCommandAsync(commandId, argsJson).ConfigureAwait(false);
        }
        catch
        {
            // Исключения из хендлеров команд логируются внутри исполнителя; аккорд уже сброшен.
        }
    }
}
