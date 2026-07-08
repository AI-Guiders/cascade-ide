#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private Func<IReadOnlyList<EditorDiagnosticStrip>>? _getDiagnosticStripsForCurrentFile;
    private Action? _focusIntercomComposer;

    private readonly Dictionary<string, AttachmentAnchor> _pendingAttachDrafts = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private string _composerAttachHint = "";

    [ObservableProperty]
    private int _chatComposerCaretIndex;

    public static bool IsComposerAttachSlash(string? slashPath) =>
        !string.IsNullOrWhiteSpace(slashPath)
        && slashPath.StartsWith("/attach", StringComparison.OrdinalIgnoreCase);

    public void SetDiagnosticStripsAccessor(Func<IReadOnlyList<EditorDiagnosticStrip>>? getStripsForCurrentFile) =>
        _getDiagnosticStripsForCurrentFile = getStripsForCurrentFile;

    public void SetFocusIntercomComposerAction(Action? focusComposer) =>
        _focusIntercomComposer = focusComposer;

    public string AttachSelectionToComposer() =>
        runAttachAffordance(ChatSlashIntercomHandlers.Ids.AttachSelection);

    public string AttachScopeToComposer() =>
        runAttachAffordance(ChatSlashIntercomHandlers.Ids.AttachScope);

    public string AttachDiagnosticAtCaretToComposer()
    {
        var editor = BuildAttachEditorSnapshot();
        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();
        var strips = _getDiagnosticStripsForCurrentFile?.Invoke() ?? Array.Empty<EditorDiagnosticStrip>();
        if (!IntercomAttachmentResolveAtSend.TryResolveDiagnosticAtCaret(
                editor,
                strips,
                workspace,
                solution,
                out var draft,
                out var error))
        {
            return error;
        }

        return completeAttachAffordance(draft);
    }

    public string AttachProblemToComposer(ProblemListItem problem)
    {
        if (problem is null)
            return "Нет выбранной диагностики.";

        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();
        if (!IntercomAttachmentResolveAtSend.TryResolveFile(
                problem.FilePath,
                problem.Line,
                problem.Line,
                workspace,
                solution,
                out var draft,
                out var error))
        {
            return error;
        }

        draft = draft with
        {
            DisplayLabel = $"{problem.FileName}:{problem.Line} {problem.Id}",
        };
        return completeAttachAffordance(draft);
    }

    public string AttachDragKindToComposer(string kind) => kind switch
    {
        IntercomAttachDragFormats.KindSelection => AttachSelectionToComposer(),
        IntercomAttachDragFormats.KindScope => AttachScopeToComposer(),
        _ => $"Неизвестный attach: {kind}",
    };

    public string ApplyAttachDropPayload(string payload)
    {
        if (payload.StartsWith(IntercomAttachDragFormats.TextPrefix, StringComparison.Ordinal))
            payload = payload[IntercomAttachDragFormats.TextPrefix.Length..];

        if (payload.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var kind = root.TryGetProperty("kind", out var kindEl) && kindEl.ValueKind == JsonValueKind.String
                    ? kindEl.GetString()
                    : null;
                if (string.Equals(kind, IntercomAttachDragFormats.KindProblem, StringComparison.OrdinalIgnoreCase)
                    && root.TryGetProperty("filePath", out var pathEl)
                    && pathEl.ValueKind == JsonValueKind.String
                    && root.TryGetProperty("line", out var lineEl)
                    && lineEl.TryGetInt32(out var line))
                {
                    var id = root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                        ? idEl.GetString() ?? ""
                        : "";
                    var severity = root.TryGetProperty("severity", out var sevEl) && sevEl.ValueKind == JsonValueKind.String
                        ? sevEl.GetString() ?? "error"
                        : "error";
                    var msg = root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                        ? msgEl.GetString() ?? ""
                        : "";
                    return AttachProblemToComposer(new ProblemListItem(
                        pathEl.GetString()!,
                        line,
                        1,
                        severity,
                        id,
                        msg));
                }

                return AttachDragKindToComposer(kind ?? "");
            }
            catch
            {
                return AttachDragKindToComposer(payload);
            }
        }

        return AttachDragKindToComposer(payload);
    }

    public ChatSlashIntercomResult TryExecuteAttachSlash(string handlerId, string? argsTail)
    {
        var editor = BuildAttachEditorSnapshot();
        var workspace = ResolveAttachWorkspaceRoot();
        var solution = ResolveAttachSolutionPath();

        AttachmentAnchor draft;
        string error;
        switch (handlerId)
        {
            case ChatSlashIntercomHandlers.Ids.AttachSelection:
                if (!IntercomAttachmentResolveAtSend.TryResolveSelection(editor, workspace, solution, out draft, out error))
                    return ChatSlashIntercomResult.Fail(error);
                break;
            case ChatSlashIntercomHandlers.Ids.AttachScope:
                if (!IntercomAttachmentResolveAtSend.TryResolveScope(editor, workspace, solution, out draft, out error))
                    return ChatSlashIntercomResult.Fail(error);
                break;
            case ChatSlashIntercomHandlers.Ids.AttachFile:
                if (!tryResolveAttachFileArgs(argsTail, workspace, solution, out draft, out error))
                    return ChatSlashIntercomResult.Fail(error);
                break;
            default:
                return ChatSlashIntercomResult.Fail($"Неизвестный attach: {handlerId}");
        }

        return insertPendingAttach(draft);
    }

    private string runAttachAffordance(string handlerId)
    {
        var result = TryExecuteAttachSlash(handlerId, null);
        if (!result.Success)
            return result.Message;

        return completeAttachAffordanceMessage(result.Message);
    }

    private string completeAttachAffordance(AttachmentAnchor draft)
    {
        var result = insertPendingAttach(draft);
        return result.Success
            ? completeAttachAffordanceMessage(result.Message)
            : result.Message;
    }

    private string completeAttachAffordanceMessage(string? detail)
    {
        _focusIntercomComposer?.Invoke();
        if (!string.IsNullOrWhiteSpace(detail))
            ClarificationStatusText = detail;
        return detail ?? "OK";
    }

    private ChatSlashIntercomResult insertPendingAttach(AttachmentAnchor draft)
    {
        var shortId = IntercomAttachmentMarkers.NewShortId();
        _pendingAttachDrafts[shortId] = draft with { Id = shortId };

        var marker = IntercomAttachmentMarkers.FormatMarker(shortId);
        var caret = Math.Clamp(ChatComposerCaretIndex, 0, ChatInput.Length);
        var prefix = caret > 0 && !char.IsWhiteSpace(ChatInput[caret - 1]) ? " " : "";
        var suffix = caret < ChatInput.Length && !char.IsWhiteSpace(ChatInput[caret]) ? " " : "";
        ChatInput = ChatInput.Insert(caret, prefix + marker + suffix);
        ChatComposerCaretIndex = caret + prefix.Length + marker.Length + suffix.Length;
        refreshComposerAttachHint();
        RefreshComposerAutocomplete();

        var label = draft.DisplayLabel ?? shortId;
        return ChatSlashIntercomResult.Ok($"Прикреплено: {label}");
    }

    private void refreshComposerAttachHint()
    {
        if (_pendingAttachDrafts.Count == 0)
        {
            ComposerAttachHint = "";
            return;
        }

        var labels = _pendingAttachDrafts.Values
            .Select(a => a.DisplayLabel)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Take(4)
            .ToList();
        var more = _pendingAttachDrafts.Count - labels.Count;
        ComposerAttachHint = labels.Count == 0
            ? $"Прикреплено: {_pendingAttachDrafts.Count}"
            : more > 0
                ? $"Прикреплено: {string.Join(", ", labels)} +{more}"
                : $"Прикреплено: {string.Join(", ", labels)}";
    }

    /// <summary>Корень для resolve вложений: workspace VM, иначе каталог .sln.</summary>
    private string ResolveAttachWorkspaceRoot()
    {
        var ws = _getWorkspaceRoot()?.Trim() ?? "";
        if (ws.Length > 0)
            return ws;
        return WorkspaceDirectoryFromSolutionPath.Resolve(_getSolutionPath?.Invoke() ?? "");
    }

    /// <summary>Абсолютный путь решения: открытое в UI, иначе <c>session.meta.solution_path</c> (.slnx и др.).</summary>
    public string? ResolveAttachSolutionPath() =>
        IntercomAttachScope.ResolveSolutionPath(
            ResolveAttachWorkspaceRoot(),
            _getSolutionPath?.Invoke(),
            _sessionSolutionPathRelative);

    private string? ResolveAttachIndexDirectoryRelative() =>
        CascadeIDE.Features.HybridIndex.Application.HybridIndexIndexDirectoryRelative.ResolveOrDefault(
            _getCascadeSettings?.Invoke().HybridIndex.IndexDir);

    private IntercomAttachmentResolveAtSend.EditorSnapshot BuildAttachEditorSnapshot() =>
        new(
            _getCurrentFilePath?.Invoke(),
            _getEditorText?.Invoke(),
            _getEditorSelectionStart?.Invoke(),
            _getEditorSelectionLength?.Invoke(),
            _getEditorCaretOffset?.Invoke());

    private static bool tryResolveAttachFileArgs(
        string? argsTail,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";
        var parts = (argsTail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            error = "Укажи путь: /attach file <path> [start] [end].";
            return false;
        }

        int? lineStart = null;
        int? lineEnd = null;
        if (parts.Length >= 2 && int.TryParse(parts[^2], out var s) && int.TryParse(parts[^1], out var e))
        {
            lineStart = s;
            lineEnd = e;
            parts = parts[..^2];
        }
        else if (parts.Length >= 2 && int.TryParse(parts[^1], out var single))
        {
            lineStart = single;
            lineEnd = single;
            parts = parts[..^1];
        }

        var path = string.Join(' ', parts);
        return IntercomAttachmentResolveAtSend.TryResolveFile(path, lineStart, lineEnd, workspaceRoot, solutionPath, out anchor, out error);
    }

    public async Task RevealAttachmentFromFeedAsync(
        AttachmentAnchor anchor,
        bool select,
        int? messageIndex = null,
        CancellationToken cancellationToken = default)
    {
        anchor = resolveFeedAttachmentAnchor(anchor, messageIndex);
        if (string.IsNullOrWhiteSpace(anchor.File))
        {
            await UiScheduler.Default.InvokeAsync(() =>
                ClarificationStatusText = "Не удалось перейти: у вложения нет пути к файлу.");
            return;
        }

        try
        {
            string result;
            if (_revealIntercomAttachmentInIde is { } revealInIde)
            {
                result = await revealInIde(anchor, select, cancellationToken).ConfigureAwait(true);
            }
            else if (_executeIdeCommandForMafAgent is { } exec)
            {
                var anchorJson = JsonSerializer.SerializeToElement(anchor, ChatPanelJson);
                var args = new Dictionary<string, JsonElement>
                {
                    ["anchor_json"] = anchorJson,
                    ["select"] = JsonSerializer.SerializeToElement(select),
                };
                result = await exec(IdeCommands.IntercomRevealAttachment, args, cancellationToken).ConfigureAwait(true);
            }
            else
            {
                await UiScheduler.Default.InvokeAsync(() =>
                    ClarificationStatusText = "Не удалось перейти: IDE bridge недоступен.");
                return;
            }

            await UiScheduler.Default.InvokeAsync(() =>
                ClarificationStatusText = string.IsNullOrWhiteSpace(result) ? "OK" : result.Trim());
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() => ClarificationStatusText = ex.Message);
        }
    }

    private AttachmentAnchor resolveFeedAttachmentAnchor(AttachmentAnchor anchor, int? messageIndex)
    {
        if (!string.IsNullOrWhiteSpace(anchor.File))
            return anchor;

        if (messageIndex is not >= 0 || messageIndex >= ChatMessages.Count)
            return anchor;

        var attachments = ChatMessages[messageIndex.Value].Attachments;
        if (attachments is null || attachments.Count == 0)
            return anchor;

        if (!string.IsNullOrWhiteSpace(anchor.Id))
        {
            foreach (var candidate in attachments)
            {
                if (string.IsNullOrWhiteSpace(candidate.Id)
                    || !string.Equals(candidate.Id, anchor.Id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return candidate;
            }
        }

        return attachments[0];
    }
}
