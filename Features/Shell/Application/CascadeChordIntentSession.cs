#nullable enable
using System.Linq;
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
        /// <summary>После корня — набор буквенно-цифрового хвоста как у <c>c:</c>.</summary>
        AwaitMelodyTail
    }

    private readonly Action _notifyOverlayProperties;
    private readonly Func<string, Task> _executeCommandByIdAsync;

    private Phase _phase;
    /// <summary>Нормализованный хвост мелодии (нижний регистр), как после <c>c:</c>.</summary>
    private string _melodyTail = "";
    private DateTimeOffset _deadline = DateTimeOffset.MinValue;
    private DispatcherTimer? _timer;

    /// <summary>Фокус в полоске HUD над редактором: туннель не перехватывает буквы.</summary>
    private bool _hudTextHasFocus;

    /// <summary>Пользователь закрыл выпадающий список (light dismiss).</summary>
    private bool _dropdownUserDismissed;

    public CascadeChordIntentSession(
        Action notifyOverlayProperties,
        Func<string, Task> executeCommandByIdAsync)
    {
        _notifyOverlayProperties = notifyOverlayProperties;
        _executeCommandByIdAsync = executeCommandByIdAsync;
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
        && (CascadeChordPresentationProjection.FilterEligibleMatches(_melodyTail).Count > 0 || OverlayNoMatches);

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

    public void HudMelodyTextSet(string value)
    {
        if (_phase != Phase.AwaitMelodyTail)
            return;
        var n = CascadeChordPresentationProjection.NormalizeMelodyInput(value);
        if (string.Equals(n, _melodyTail, StringComparison.Ordinal))
            return;
        ApplyMelodyTail(n);
    }

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
        "Esc — отмена · таймаут " + (int)TimeoutSeconds + " с · Ctrl+Q — палитра";

    public bool OverlayNoMatches =>
        _phase == Phase.AwaitMelodyTail
        && !string.IsNullOrEmpty(_melodyTail)
        && CascadeChordPresentationProjection.FilterEligibleMatches(_melodyTail).Count == 0;

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

    public void SetHudTextHasFocus(bool hasFocus)
    {
        _hudTextHasFocus = hasFocus;
        if (hasFocus)
            _dropdownUserDismissed = false;
        _notifyOverlayProperties();
    }

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
        var cmdEnter = IntentMelodyAliases.TryResolveExactCommandId(_melodyTail);
        if (cmdEnter != null)
            _ = ExecuteCommandFireAndForgetAsync(cmdEnter);
        EndIdle();
    }

    private void ApplyMelodyTail(string newTail)
    {
        _dropdownUserDismissed = false;
        var matches = CascadeChordPresentationProjection.FilterEligibleMatches(newTail);
        var exact = matches.FirstOrDefault(m => string.Equals(m.Alias, newTail, StringComparison.Ordinal));
        var hasLonger = matches.Any(m => m.Alias.Length > newTail.Length);
        if (exact.CommandId != null && !hasLonger && newTail.Length > 0)
        {
            _melodyTail = newTail;
            _ = ExecuteCommandFireAndForgetAsync(exact.CommandId);
            EndIdle();
            return;
        }

        if (matches.Count == 0 && newTail.Length > 0)
        {
            EndIdle();
            return;
        }

        _melodyTail = newTail;
        _deadline = DateTimeOffset.UtcNow.AddSeconds(TimeoutSeconds);
        RestartTimer();
        _notifyOverlayProperties();
    }

    public void BeginRoot(Action? requestHudFocus)
    {
        _phase = Phase.AwaitMelodyTail;
        _melodyTail = "";
        _hudTextHasFocus = false;
        _dropdownUserDismissed = false;
        _deadline = DateTimeOffset.UtcNow.AddSeconds(TimeoutSeconds);
        RestartTimer();
        _notifyOverlayProperties();
        requestHudFocus?.Invoke();
    }

    /// <summary>
    /// Обрабатывает аккорд Cascade: возвращает <see langword="true"/>, если событие поглощено (в т.ч. корень).
    /// </summary>
    public bool TryConsumeKeyDown(KeyEventArgs e, Action? requestHudFocusOnChordRoot)
    {
        var map = MainWindowHotkeyService.GetMergedMap();
        var rootGesture = CascadeChordHotkey.ResolveRootGesture(map);

        if (CascadeChordHotkey.RootGestureMatches(rootGesture, e))
        {
            BeginRoot(requestHudFocusOnChordRoot);
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

        if (_hudTextHasFocus)
            return false;

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

        if (!CascadeChordMelodyKeyMap.TryMapToChar(e.Key, out var ch))
        {
            EndIdle();
            e.Handled = true;
            return true;
        }

        var newTail = _melodyTail + ch;
        ApplyMelodyTail(newTail);
        e.Handled = true;
        return true;
    }

    public void EndIdle()
    {
        _phase = Phase.Idle;
        _melodyTail = "";
        _hudTextHasFocus = false;
        _dropdownUserDismissed = false;
        StopTimer();
        _notifyOverlayProperties();
    }

    private async Task ExecuteCommandFireAndForgetAsync(string commandId)
    {
        try
        {
            await _executeCommandByIdAsync(commandId).ConfigureAwait(false);
        }
        catch
        {
            // Исключения из хендлеров команд логируются внутри исполнителя; аккорд уже сброшен.
        }
    }
}
