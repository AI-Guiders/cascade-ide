#nullable enable

using CascadeIDE.Features.Chat.AnchorPeek;
using CascadeIDE.Features.Cockpit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>CCL на Skia Intercom (не оверлей редактора).</summary>
    public bool ShowIntercomCockpitCommandLine =>
        IsCockpitCommandLineOpen && CommandLineSession.ActiveHost == CockpitCommandLineHostKind.Intercom;

    internal void NotifyCockpitCommandLinePresentationChanged() =>
        OnPropertyChanged(nameof(ShowIntercomCockpitCommandLine));
    private readonly SlashCommandPreviewService _slashCommandPreviewService;
    private readonly ICockpitCommandLineSession _cockpitCommandLineSession;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntercomSlashPreviewToolTip))]
    private bool _isCockpitCommandLineOpen;

    [ObservableProperty]
    private string _cockpitCommandLineText = "/";

    [ObservableProperty]
    private string _commandLineSlashPreview = "";

    [ObservableProperty]
    private SlashCommandPreviewKind _commandLineSlashPreviewKind;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntercomSlashPreviewToolTip))]
    private string? _commandLineSlashPreviewToolTip;

    [ObservableProperty]
    private string _composerSlashPreview = "";

    [ObservableProperty]
    private SlashCommandPreviewKind _composerSlashPreviewKind;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntercomSlashPreviewToolTip))]
    private string? _composerSlashPreviewToolTip;

    [ObservableProperty]
    private int _cockpitCommandLineCaretIndex;

    /// <summary>ToolTip на Intercom surface: CCL только при host=Intercom.</summary>
    public string? IntercomSlashPreviewToolTip =>
        ShowIntercomCockpitCommandLine ? CommandLineSlashPreviewToolTip : ComposerSlashPreviewToolTip;

    public ICockpitCommandLineSession CommandLineSession => _cockpitCommandLineSession;

    [RelayCommand]
    private void OpenCockpitCommandLineUi() => OpenCockpitCommandLine("/");

    public void OpenCockpitCommandLine(string initialText = "/")
    {
        var text = string.IsNullOrWhiteSpace(initialText) ? "/" : initialText;
        IsCockpitCommandLineOpen = true;
        CockpitCommandLineText = text;
        CockpitCommandLineCaretIndex = text.Length;
        UpdateSlashPreview(TciInputSurface.CockpitCommandLine);
        UpdateSlashPreview(TciInputSurface.Composer);
        RefreshCockpitCommandLineAutocomplete();
    }

    partial void OnIsCockpitCommandLineOpenChanged(bool value) =>
        NotifyCockpitCommandLinePresentationChanged();

    public void CloseCockpitCommandLine()
    {
        IsCockpitCommandLineOpen = false;
        applySlashPreview(SlashCommandPreviewResult.Empty, TciInputSurface.CockpitCommandLine);
        DismissChatSlashAutocomplete();
        RefreshComposerAutocomplete();
        UpdateSlashPreview(TciInputSurface.Composer);
    }

    partial void OnCockpitCommandLineTextChanged(string value)
    {
        UpdateSlashPreview(TciInputSurface.CockpitCommandLine);
        if (IsCockpitCommandLineOpen)
            RefreshCockpitCommandLineAutocomplete(value);
    }

    partial void OnCockpitCommandLineCaretIndexChanged(int value)
    {
        if (IsCockpitCommandLineOpen)
            RefreshCockpitCommandLineAutocomplete();
    }

    /// <summary>Единая точка обновления slash-preview (P2).</summary>
    public void UpdateSlashPreview(TciInputSurface surface, string? textOverride = null, int? caretOverride = null)
    {
        if (surface == TciInputSurface.Composer && IsCockpitCommandLineOpen)
        {
            applySlashPreview(SlashCommandPreviewResult.Empty, TciInputSurface.Composer);
            return;
        }

        var preview = surface switch
        {
            TciInputSurface.CockpitCommandLine => _slashCommandPreviewService.Evaluate(
                textOverride ?? CockpitCommandLineText),
            TciInputSurface.Composer =>
                _slashCommandPreviewService.EvaluateComposerAtCaret(
                    textOverride ?? ChatInput,
                    caretOverride ?? ChatComposerCaretIndex),
            _ => SlashCommandPreviewResult.Empty,
        };

        applySlashPreview(preview, surface);
    }

    /// <summary>Slash preview под composer (линия по каретке).</summary>
    public void RefreshComposerSlashPreview(string? inputOverride = null, int? caretOverride = null) =>
        UpdateSlashPreview(TciInputSurface.Composer, inputOverride, caretOverride);

    private void applySlashPreview(SlashCommandPreviewResult preview, TciInputSurface surface)
    {
        var text = preview.Text ?? "";
        var tip = SlashCommandPreviewAccessibility.FormatToolTip(preview);
        switch (surface)
        {
            case TciInputSurface.CockpitCommandLine:
                CommandLineSlashPreview = text;
                CommandLineSlashPreviewKind = preview.Kind;
                CommandLineSlashPreviewToolTip = tip;
                break;
            case TciInputSurface.Composer:
                ComposerSlashPreview = text;
                ComposerSlashPreviewKind = preview.Kind;
                ComposerSlashPreviewToolTip = tip;
                break;
        }
    }

    private bool tryBuildAnchorSlashPreview(string argsTail, out SlashCommandPreviewResult result)
    {
        if (TryResolveAnchorByShortId(argsTail, out var anchor, out _, out var error))
        {
            var kind = SlashCommandPreviewService.MapResolveOutcome(anchor.ResolveOutcome);
            var label = anchor.DisplayLabel ?? anchor.MemberKey ?? anchor.File ?? "—";
            var status = IntercomAnchorSlash.FormatOutcomeShort(anchor.ResolveOutcome);
            var id = string.IsNullOrWhiteSpace(anchor.Id) ? "?" : anchor.Id;
            var ordinalPrefix = AnchorPeekResolver.TryResolve(
                argsTail,
                BuildAnchorPeekResolveContext(),
                out _,
                out _,
                out var ordinal,
                out _)
                ? $"#{ordinal} "
                : "";
            result = new($"{ordinalPrefix}a:{id}  {label}  {status}", kind);
            return true;
        }

        result = new(error, SlashCommandPreviewKind.Error);
        return true;
    }

    public async Task<bool> TryCommitCockpitCommandLineAsync(CancellationToken cancellationToken = default)
    {
        var raw = CockpitCommandLineText.Trim();
        if (raw.Length == 0)
        {
            CloseCockpitCommandLine();
            return true;
        }

        var result = await _slashCommandRunner.TryRunAsync(raw, cancellationToken).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!string.IsNullOrWhiteSpace(result.DetailText))
                ClarificationStatusText = result.DetailText.Trim();
        });

        CloseCockpitCommandLine();
        return result.Success;
    }
}
