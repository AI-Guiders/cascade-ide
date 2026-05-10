using System.Collections.Frozen;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>Мост веб-портала ADR 0108: JSON с <c>command_id</c> + <c>args</c> → whitelist → <see cref="IIdeMcpActions"/>.</summary>
public sealed class WebAiPortalCommandBridge : ObservableObject
{
    private readonly IIdeMcpActions _actions;

    private bool _bridgeConsented;
    private bool _bridgeArmed;
    private bool _allowWritesForSession;

    /// <summary>Согласие с политикой моста (ADR 0108 §2.6).</summary>
    public bool BridgeConsented
    {
        get => _bridgeConsented;
        set => SetProperty(ref _bridgeConsented, value);
    }

    /// <summary>Включить приём сообщений от страницы (<c>invokeCSharpAction</c>).</summary>
    public bool BridgeArmed
    {
        get => _bridgeArmed;
        set => SetProperty(ref _bridgeArmed, value);
    }

    /// <summary>Разрешить write без модалки на каждый вызов (ADR 0108 §2.3 п.8).</summary>
    public bool AllowWritesForSession
    {
        get => _allowWritesForSession;
        set => SetProperty(ref _allowWritesForSession, value);
    }

    /// <summary>PoC-allowlist ADR 0108 §2.2 (канонические <c>IdeCommands</c>).</summary>
    internal static readonly FrozenSet<string> Whitelist = FrozenSet.ToFrozenSet(
        new[]
        {
            IdeCommands.GetEditorContentRange,
            IdeCommands.GetEditorState,
            IdeCommands.CodebaseIndexSearch,
            IdeCommands.GetCurrentFileDiagnostics,
        },
        StringComparer.Ordinal);

    /// <summary>Команды, считающиеся write и требующие подтверждения (пока пусто — весь whitelist чтение).</summary>
    internal static readonly FrozenSet<string> WriteCommands = FrozenSet.ToFrozenSet(
        Array.Empty<string>(),
        StringComparer.Ordinal);

    public WebAiPortalCommandBridge(IIdeMcpActions actions) => _actions = actions;

    public void RevokeBridge()
    {
        BridgeArmed = false;
        BridgeConsented = false;
        AllowWritesForSession = false;
    }

    /// <summary>Тело сообщения от <c>invokeCSharpAction</c>: JSON одной строкой или объект с полями MCP execute_command.</summary>
    public async Task<string> ExecuteFromWebJsonAsync(string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "Empty body";
        if (!BridgeConsented || !BridgeArmed)
            return "Web AI bridge is not armed (consent and enable required in IDE).";

        Dictionary<string, JsonElement>? dict;
        try
        {
            dict = ParseToArgDictionary(body);
        }
        catch (JsonException ex)
        {
            return "Invalid JSON: " + ex.Message;
        }

        if (dict is null)
            return "Invalid JSON: expected object.";

        var merged = IdeExecuteCommandArgs.MergeNestedArgs(dict);
        var commandId = merged is not null && merged.TryGetValue("command_id", out var cid) ? cid.GetString() : null;
        if (string.IsNullOrEmpty(commandId))
            return "Missing command_id";

        if (!Whitelist.Contains(commandId))
            return $"Command not allowed by web portal whitelist: {commandId}";

        if (WriteCommands.Contains(commandId))
        {
            if (!AllowWritesForSession)
            {
                var ok = await _actions
                    .RequestConfirmationAsync(
                        $"Веб-портал запрашивает команду (изменение): {commandId}. Разрешить один раз?",
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!string.Equals(ok, ConfirmationResponses.Ok, StringComparison.OrdinalIgnoreCase))
                    return "Denied by user";
            }
        }

        return await _actions.ExecuteCommandAsync(commandId, merged, cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, JsonElement>? ParseToArgDictionary(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return null;
        var dict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var p in doc.RootElement.EnumerateObject())
            dict[p.Name] = p.Value.Clone();
        return dict;
    }
}
