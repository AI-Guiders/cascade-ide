#nullable enable
using System.Text.Json;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Reveal Intercom attachment from chat feed into the IDE.</summary>
public partial class ChatPanelViewModel
{
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
