#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Preview slash intercom по <c>intercom_handler</c> из каталога (не по hardcoded path).</summary>
internal static class SlashIntercomPreviewPolicies
{
    public static bool TryBuild(
        string slashPath,
        string? argTail,
        SlashCommandAnchorPreviewResolver? resolveAnchor,
        out SlashCommandPreviewResult result)
    {
        result = default;
        if (!SlashRouteCatalogIndex.TryGetIntercomHandler(slashPath, out var handlerId))
            return false;

        switch (handlerId)
        {
            case ChatSlashIntercomHandlers.Ids.MessageSelect:
                result = SlashCommandPreviewRuleHelpers.BuildParametricPreview(
                    argTail ?? "",
                    "Сообщения",
                    slashPath);
                return true;

            case ChatSlashIntercomHandlers.Ids.MessageSelectClear:
            {
                var tail = (argTail ?? "").Trim();
                result = tail.Length > 0
                    ? new("Ожидается «/intercom message select clear» без аргументов.", SlashCommandPreviewKind.Error)
                    : new("Сбросить подсветку сообщений в detail-ленте.", SlashCommandPreviewKind.Ok);
                return true;
            }

            case ChatSlashIntercomHandlers.Ids.MessageAnchorsList:
                result = new("Готово: список якорей сообщения и черновика.", SlashCommandPreviewKind.Ok);
                return true;

            case ChatSlashIntercomHandlers.Ids.AnchorPeek:
                result = SlashCommandPreviewRuleHelpers.BuildAnchorPeek(
                    SlashPathAliases.ExtractPeekArgs(slashPath, argTail),
                    resolveAnchor);
                return true;
        }

        return false;
    }
}
