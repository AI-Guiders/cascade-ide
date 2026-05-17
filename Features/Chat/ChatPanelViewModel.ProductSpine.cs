#nullable enable
using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private bool _suppressProductSpinePersistence;

    [ObservableProperty]
    private string _productSpineLineTitle = "";

    [ObservableProperty]
    private string _productSpineCurrentFocus = "";

    [ObservableProperty]
    private string _productSpineMilestonesText = "";

    [ObservableProperty]
    private bool _includeProductSpineInAgentContext;

    partial void OnProductSpineLineTitleChanged(string value) => OnProductSpineFieldChanged();

    partial void OnProductSpineCurrentFocusChanged(string value) => OnProductSpineFieldChanged();

    partial void OnProductSpineMilestonesTextChanged(string value) => OnProductSpineFieldChanged();

    partial void OnIncludeProductSpineInAgentContextChanged(bool value) => OnProductSpineFieldChanged();

    public string ToggleProductSpineInAgentContext()
    {
        IncludeProductSpineInAgentContext = !IncludeProductSpineInAgentContext;
        return IncludeProductSpineInAgentContext ? "ProductSpineInAgentContext=on" : "ProductSpineInAgentContext=off";
    }

    public string GetProductSpineJson()
    {
        var spine = BuildProductSpine();
        return JsonSerializer.Serialize(new
        {
            line_title = spine.LineTitle,
            current_focus = spine.CurrentFocus,
            milestones = spine.Milestones,
            include_in_agent_context = spine.IncludeInAgentContext,
            has_content = spine.HasContent
        }, ChatPanelJson);
    }

    public string SetProductSpineFromMcp(IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || args.Count == 0)
            return "No spine fields provided";

        var hasAny = false;
        _suppressProductSpinePersistence = true;
        try
        {
            if (args.ContainsKey("line_title"))
            {
                ProductSpineLineTitle = McpCommandJsonArgs.String(args, "line_title") ?? "";
                hasAny = true;
            }

            if (args.ContainsKey("current_focus"))
            {
                ProductSpineCurrentFocus = McpCommandJsonArgs.String(args, "current_focus") ?? "";
                hasAny = true;
            }

            if (args.ContainsKey("milestones"))
            {
                ProductSpineMilestonesText = McpCommandJsonArgs.String(args, "milestones") ?? "";
                hasAny = true;
            }

            if (McpCommandJsonArgs.OptionalBool(args, "include_in_agent_context") is { } include)
            {
                IncludeProductSpineInAgentContext = include;
                hasAny = true;
            }
        }
        finally
        {
            _suppressProductSpinePersistence = false;
        }

        if (!hasAny)
            return "No spine fields provided";

        _ = PersistProductSpineMetadataAsync();
        RefreshChatSurfaceSnapshot();
        return "OK";
    }

    private ChatProductSpine BuildProductSpine() =>
        new(
            ProductSpineLineTitle.Trim(),
            ProductSpineCurrentFocus.Trim(),
            ChatProductSpine.ParseMilestonesText(ProductSpineMilestonesText),
            IncludeProductSpineInAgentContext);

    private string ApplyProductSpineToOutboundMessage(string input)
    {
        var prefix = BuildProductSpine().BuildAgentContextPrefix();
        return prefix is null ? input : prefix + Environment.NewLine + input;
    }

    private void OnProductSpineFieldChanged()
    {
        RefreshChatSurfaceSnapshot();
        if (!_suppressProductSpinePersistence)
            _ = PersistProductSpineMetadataAsync();
    }

    private void ApplyProductSpineFromMetadata(ChatSessionMetadata meta)
    {
        _suppressProductSpinePersistence = true;
        try
        {
            ProductSpineLineTitle = string.IsNullOrWhiteSpace(meta.ProductSpineLineTitle)
                ? ResolveDefaultProductSpineLineTitle()
                : meta.ProductSpineLineTitle.Trim();
            ProductSpineCurrentFocus = meta.ProductSpineCurrentFocus?.Trim() ?? "";
            ProductSpineMilestonesText = meta.ProductSpineMilestones?.Trim() ?? "";
            IncludeProductSpineInAgentContext = meta.ProductSpineIncludeInAgentContext;
        }
        finally
        {
            _suppressProductSpinePersistence = false;
        }
    }

    private string ResolveDefaultProductSpineLineTitle()
    {
        var root = _getWorkspaceRoot().Trim();
        if (string.IsNullOrEmpty(root))
            return ChatProductSpinePresentation.DefaultLineTitle;
        try
        {
            return new DirectoryInfo(root).Name;
        }
        catch
        {
            return ChatProductSpinePresentation.DefaultLineTitle;
        }
    }

    private async Task PersistProductSpineMetadataAsync()
    {
        try
        {
            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            var updated = meta with
            {
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                ProductSpineLineTitle = string.IsNullOrWhiteSpace(ProductSpineLineTitle) ? null : ProductSpineLineTitle.Trim(),
                ProductSpineCurrentFocus = string.IsNullOrWhiteSpace(ProductSpineCurrentFocus) ? null : ProductSpineCurrentFocus.Trim(),
                ProductSpineMilestones = string.IsNullOrWhiteSpace(ProductSpineMilestonesText) ? null : ProductSpineMilestonesText.Trim(),
                ProductSpineIncludeInAgentContext = IncludeProductSpineInAgentContext
            };
            await _sessionStore.SaveMetadataAsync(updated, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // best-effort
        }
    }
}
