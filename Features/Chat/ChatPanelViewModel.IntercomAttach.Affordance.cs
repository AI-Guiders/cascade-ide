#nullable enable
using System.Text.Json;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

/// <summary>Composer attach affordances: selection/scope/diagnostic/problem + drag-drop.</summary>
public partial class ChatPanelViewModel
{
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
}
