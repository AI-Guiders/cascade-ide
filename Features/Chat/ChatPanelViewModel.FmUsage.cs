#nullable enable
using CascadeIDE.Features.Agent.Harness;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Fm;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

/// <summary>FM token usage: catalog, capture, Intercom chrome subtitle (ADR 0166 P1 P0).</summary>
public partial class ChatPanelViewModel
{
    private readonly FmModelCatalog _fmModelCatalog = new();
    private FmTurnUsage? _fmSessionUsage;
    private FmTurnUsage? _fmLastTurnUsage;
    private int? _fmMaxModelLen;
    private Func<FmOpenAiCompatibleCredentials?>? _getFmOpenAiCredentials;

    [ObservableProperty]
    private string _fmUsageSubtitle = "";

    public void SetFmOpenAiCredentialsAccessor(Func<FmOpenAiCompatibleCredentials?> getCredentials) =>
        _getFmOpenAiCredentials = getCredentials;

    private async Task RecordFmTurnUsageAsync(FmTurnUsage? usage, CancellationToken cancellationToken = default)
    {
        if (usage is null)
            return;

        _fmLastTurnUsage = usage;
        _fmSessionUsage = _fmSessionUsage is null ? usage : _fmSessionUsage.Add(usage);

        await EnsureFmModelContextAsync(cancellationToken).ConfigureAwait(false);
        RefreshFmUsageSubtitle();

        if (_fmMaxModelLen is > 0 && usage.PromptTokens > 0)
        {
            HarnessContextPressureResult usagePressure = HarnessContextPressureResult.None;
            await UiScheduler.Default.InvokeAsync(() =>
            {
                usagePressure = Harness.OnContextUsagePct(usage.PromptTokens, _fmMaxModelLen.Value);
            }).ConfigureAwait(false);

            if (usagePressure.InjectPreCompact && !string.IsNullOrWhiteSpace(usagePressure.PreCompactUserMessage))
                await InjectHarnessUserMessageAsync(usagePressure.PreCompactUserMessage!).ConfigureAwait(false);
        }
    }

    private async Task EnsureFmModelContextAsync(CancellationToken cancellationToken)
    {
        if (_fmMaxModelLen is > 0)
            return;

        var creds = _getFmOpenAiCredentials?.Invoke();
        if (creds is null)
            return;

        var info = await _fmModelCatalog.TryResolveModelAsync(
            creds.BaseUrl,
            creds.ApiKey,
            creds.ModelId,
            cancellationToken).ConfigureAwait(false);
        if (info?.MaxModelLen is > 0)
            _fmMaxModelLen = info.MaxModelLen;
    }

    private void RefreshFmUsageSubtitle()
    {
        var harness = _getCascadeSettings?.Invoke()?.Agent.Harness;
        var warnPct = harness?.ContextWarnPct ?? 75;
        var text = FmUsagePresentation.FormatSubtitle(
            _fmLastTurnUsage,
            _fmSessionUsage,
            _fmMaxModelLen,
            warnPct);

        UiScheduler.Default.Post(() => FmUsageSubtitle = text);
    }
}

/// <summary>OpenAI-compatible FM endpoint + key для catalog/usage.</summary>
public sealed record FmOpenAiCompatibleCredentials(string BaseUrl, string ApiKey, string ModelId);
