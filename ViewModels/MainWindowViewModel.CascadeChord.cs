using Avalonia.Input;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models.Shell;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Аккордный слой ADR 0060: корень <c>cascade_chord</c> из hotkeys.toml (по умолчанию Ctrl+K), затем <b>тот же хвост мелодии</b>, что после <c>c:</c> в палитре (см. <see cref="IntentMelodyAliases"/>), без префикса <c>c:</c> и без Enter — если alias однозначен (например <c>so</c>).
/// При конфликте префиксов (например <c>gs</c> vs <c>gsu</c>) точное совпадение после полного ввода или по клавише Enter.
/// </summary>
public partial class MainWindowViewModel
{
    private CascadeChordIntentSession? _cascadeChordSession;

    private CascadeChordIntentSession ChordSession =>
        _cascadeChordSession ??= new CascadeChordIntentSession(
            NotifyCascadeChordOverlayProperties,
            async commandId =>
            {
                try
                {
                    await ((Services.IIdeMcpActions)this).ExecuteCommandAsync(commandId, null, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // Исключения из хендлеров команд логируются внутри исполнителя; аккорд уже сброшен.
                }
            });

    /// <summary>Связанные проекции оверлея/лампы Command при смене фазы или хвоста мелодии (ADR 0060).</summary>
    private static readonly string[] CascadeChordOverlayPresentationNames =
    [
        nameof(IsCascadeChordOverlayVisible),
        nameof(CascadeChordOverlayHintText),
        nameof(CascadeChordOverlaySuggestions),
        nameof(CascadeChordOverlayInputText),
        nameof(CascadeChordOverlayFindBarLine),
        nameof(CascadeChordOverlayFindBarOpacity),
        nameof(CascadeChordOverlayNoMatches),
        nameof(CascadeChordOverlayCompactFooter),
        nameof(CascadeChordHudMelodyText),
        nameof(IsCascadeChordDropdownOpen),
        nameof(HasChordDropdownItems),
        nameof(CascadeChordHudWatermark),
        nameof(CommandArmedLampToolTip),
    ];

    /// <summary>Показать оверлей с подсказками (ADR 0060 §6) пока активна машина аккорда.</summary>
    public bool IsCascadeChordOverlayVisible => ChordSession.IsOverlayVisible;

    /// <summary>Текст подсказки для текущего шага машины аккорда.</summary>
    public string CascadeChordOverlayHintText => ChordSession.OverlayHintText;

    /// <summary>Команды, подходящие под префикс (выпадающий список, до 25).</summary>
    public IReadOnlyList<CascadeChordOverlaySuggestion> CascadeChordOverlaySuggestions =>
        ChordSession.OverlaySuggestions;

    /// <summary>Выпадающий список под полем ввода: пока armed, есть префикс и совпадения или явное «нет совпадений».</summary>
    public bool IsCascadeChordDropdownOpen => ChordSession.IsDropdownOpen;

    /// <summary>Есть строки в выпадающем списке (для «нет совпадений» — отдельный текст).</summary>
    public bool HasChordDropdownItems => ChordSession.HasDropdownItems;

    /// <summary>Подпись поля ввода рядом с CMD: в покое и при armed.</summary>
    public string CascadeChordHudWatermark => ChordSession.HudWatermark;

    /// <summary>Набранный хвост для визуального поля ввода в оверлее (find-bar style).</summary>
    public string CascadeChordOverlayInputText => ChordSession.OverlayInputText;

    /// <summary>Двусторонняя привязка к полю ввода в полоске над редактором (только фаза armed).</summary>
    public string CascadeChordHudMelodyText
    {
        get => ChordSession.HudMelodyTextGet();
        set => ChordSession.HudMelodyTextSet(value);
    }

    /// <summary>После Ctrl+K — сфокусировать поле в полоске task cockpit (см. <see cref="Views.CascadeChordHudStripView"/>).</summary>
    public event Action? CascadeChordHudFocusRequested;

    /// <summary>Одна строка для полоски: плейсхолдер при пустом хвосте или набранный хвост (без двух <c>IsVisible</c>).</summary>
    public string CascadeChordOverlayFindBarLine => ChordSession.OverlayFindBarLine;

    /// <summary>Плейсхолдер в полоске — чуть приглушить через прозрачность текста.</summary>
    public double CascadeChordOverlayFindBarOpacity => ChordSession.OverlayFindBarOpacity;

    /// <summary>Нижняя строка подсказки в полосе.</summary>
    public string CascadeChordOverlayCompactFooter => ChordSession.OverlayCompactFooter;

    /// <summary>При вводе неверного префикса до сброса (редко видно — машина часто сразу выходит).</summary>
    public bool CascadeChordOverlayNoMatches => ChordSession.OverlayNoMatches;

    /// <summary>Подсказка для лампы Command на тулбаре: в покое — кратко; при armed — полный контекст аккорда.</summary>
    public string CommandArmedLampToolTip => ChordSession.CommandArmedLampToolTip;

    private void NotifyCascadeChordOverlayProperties()
    {
        foreach (var name in CascadeChordOverlayPresentationNames)
            OnPropertyChanged(name);
    }

    /// <summary>Вызывается из полоски HUD: фокус ввода — туннель не должен съедать буквы до TextBox.</summary>
    public void SetCascadeChordHudTextHasFocus(bool hasFocus) => ChordSession.SetHudTextHasFocus(hasFocus);

    /// <summary>Popup закрыт кликом вне списка (Avalonia light dismiss).</summary>
    public void NotifyCascadeChordDropdownDismissed() => ChordSession.NotifyDropdownDismissed();

    /// <summary>Выбор строки из выпадающего списка.</summary>
    public void PickCascadeChordSuggestion(CascadeChordOverlaySuggestion? item) => ChordSession.PickSuggestion(item);

    /// <summary>Esc из поля ввода или снаружи (после снятия фокуса с HUD).</summary>
    public void CancelCascadeChord() => ChordSession.Cancel();

    /// <summary>Enter в поле HUD: как в туннеле — точный alias или сброс.</summary>
    public void OnCascadeChordHudEnter() => ChordSession.OnHudEnterFromView();

    /// <summary>Корень аккорда (hotkeys.toml <c>cascade_chord</c>): обрабатывается tunnel KeyDown главного окна (как палитра Ctrl+Q).</summary>
    [RelayCommand]
    private void CascadeChordRoot() =>
        ChordSession.BeginRoot(() => CascadeChordHudFocusRequested?.Invoke());

    /// <summary>
    /// Обрабатывает аккорд Cascade: возвращает <see langword="true"/>, если событие поглощено (в т.ч. корень Ctrl+K).
    /// Вызывать из tunnel <see cref="Views.MainWindow"/> до <see cref="MainWindowHotkeyService.TryHandleTunnelShortcuts"/>.
    /// </summary>
    public bool TryConsumeCascadeChordKeyDown(KeyEventArgs e) =>
        ChordSession.TryConsumeKeyDown(e, () => CascadeChordHudFocusRequested?.Invoke());
}
