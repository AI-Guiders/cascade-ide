using System.Text.Json;
using Avalonia.Input;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models.Shell;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Аккордный слой ADR 0060: корень <c>cascade_chord</c> из hotkeys.toml (по умолчанию Ctrl+K), затем тот же хвост мелодии, что после <c>c:</c>.
/// Однозначный обычный alias (например <c>so</c>) исполняется без Enter при отсутствии более длинного alias-префикса; параметрические (<c>wai:</c>, <c>els:</c>:…) — только по Enter или из палитры.
/// При конфликте префиксов (<c>gs</c> vs <c>gsu</c>) — точный хвост или Enter.
/// </summary>
public partial class MainWindowViewModel
{
    private CascadeChordIntentSession? _cascadeChordSession;

    private CascadeChordIntentSession ChordSession =>
        _cascadeChordSession ??= new CascadeChordIntentSession(
            NotifyCascadeChordOverlayProperties,
            () => CurrentFilePath,
            () => EditorText,
            async (commandId, argsJson) =>
            {
                try
                {
                    IReadOnlyDictionary<string, JsonElement>? args = IdeCommandRegistry.ParseArgs(argsJson);
                    await ((IIdeMcpActions)this).ExecuteCommandAsync(commandId, args, CancellationToken.None)
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
        nameof(IsCascadeChordHudMirrorPlaceholderVisible),
        nameof(IsCascadeChordHudMirrorMelodyVisible),
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

    /// <summary>Хвост мелодии в полоске HUD (зеркало; только чтение из сессии).</summary>
    public string CascadeChordHudMelodyText => ChordSession.HudMelodyTextGet();

    /// <summary>Плейсхолдер зеркала: покой (Ctrl+K…) или пустой armed до первой буквы.</summary>
    public bool IsCascadeChordHudMirrorPlaceholderVisible =>
        !IsCascadeChordOverlayVisible || string.IsNullOrEmpty(CascadeChordHudMelodyText);

    /// <summary>Текущий набранный хвост поверх placeholder.</summary>
    public bool IsCascadeChordHudMirrorMelodyVisible =>
        IsCascadeChordOverlayVisible && !string.IsNullOrEmpty(CascadeChordHudMelodyText);

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

    /// <summary>Popup закрыт кликом вне списка (Avalonia light dismiss).</summary>
    public void NotifyCascadeChordDropdownDismissed() => ChordSession.NotifyDropdownDismissed();

    /// <summary>Выбор строки из выпадающего списка.</summary>
    public void PickCascadeChordSuggestion(CascadeChordOverlaySuggestion? item) => ChordSession.PickSuggestion(item);

    /// <summary>Esc или явная отмена: сброс сессии аккорда.</summary>
    public void CancelCascadeChord() => ChordSession.Cancel();

    /// <summary>Явный Enter (программно): как из туннеля — точный alias или выход из фазы.</summary>
    public void OnCascadeChordHudEnter() => ChordSession.OnHudEnterFromView();

    /// <summary>Корень аккорда (hotkeys.toml <c>cascade_chord</c>): обрабатывается tunnel KeyDown главного окна (как палитра Ctrl+Q).</summary>
    [RelayCommand]
    private void CascadeChordRoot() => ChordSession.BeginRoot();

    /// <summary>
    /// Обрабатывает аккорд Cascade: возвращает <see langword="true"/>, если событие поглощено (в т.ч. корень Ctrl+K).
    /// Вызывать из tunnel <see cref="Views.MainWindow"/> до <see cref="MainWindowHotkeyService.TryHandleTunnelShortcuts"/>.
    /// </summary>
    public bool TryConsumeCascadeChordKeyDown(KeyEventArgs e) =>
        ChordSession.TryConsumeKeyDown(e);
}
