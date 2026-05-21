#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    [ObservableProperty]
    private bool _isCockpitCommandLineOpen;

    [ObservableProperty]
    private string _cockpitCommandLineText = "/";

    [ObservableProperty]
    private string _cockpitCommandLinePreview = "";

    [ObservableProperty]
    private int _cockpitCommandLineCaretIndex;

    [RelayCommand]
    private void OpenCockpitCommandLineUi() => OpenCockpitCommandLine("/");

    public void OpenCockpitCommandLine(string initialText = "/")
    {
        var text = string.IsNullOrWhiteSpace(initialText) ? "/" : initialText;
        IsCockpitCommandLineOpen = true;
        CockpitCommandLineText = text;
        CockpitCommandLineCaretIndex = text.Length;
        refreshCockpitCommandLinePreview();
        RefreshCockpitCommandLineAutocomplete();
    }

    public void CloseCockpitCommandLine()
    {
        IsCockpitCommandLineOpen = false;
        CockpitCommandLinePreview = "";
        DismissChatSlashAutocomplete();
        RefreshComposerAutocomplete();
    }

    partial void OnCockpitCommandLineTextChanged(string value)
    {
        refreshCockpitCommandLinePreview();
        if (IsCockpitCommandLineOpen)
            RefreshCockpitCommandLineAutocomplete(value);
    }

    partial void OnCockpitCommandLineCaretIndexChanged(int value)
    {
        if (IsCockpitCommandLineOpen)
            RefreshCockpitCommandLineAutocomplete();
    }

    private void refreshCockpitCommandLinePreview()
    {
        CockpitCommandLinePreview = CockpitCommandLinePreviewBuilder.TryBuild(CockpitCommandLineText, out var summary)
            ? summary ?? ""
            : "";
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
