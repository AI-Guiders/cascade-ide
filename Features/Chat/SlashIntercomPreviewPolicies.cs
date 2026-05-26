#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Preview slash intercom по <c>intercom_handler</c> из каталога (не по hardcoded path).</summary>
internal static class SlashIntercomPreviewPolicies
{
    public static bool TryBuild(
        string intercomPath,
        in ChatSlashCommandParseResult parse,
        SlashCommandAnchorPreviewResolver? resolveAnchor,
        out SlashCommandPreviewResult result)
    {
        result = default;
        if (!SlashRouteCatalogIndex.TryGetIntercomHandler(intercomPath, out var handlerId))
            return false;

        switch (handlerId)
        {
            case ChatSlashIntercomHandlers.Ids.MessageSelect:
                result = SlashCommandPreviewRuleHelpers.BuildParametricPreview(parse.ArgsTail, "Сообщения", parse);
                return true;

            case ChatSlashIntercomHandlers.Ids.MessageSelectClear:
            {
                var tail = (parse.ArgsTail ?? "").Trim();
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
                    SlashPathAliases.ExtractPeekArgs(parse),
                    resolveAnchor);
                return true;
        }

        return false;
    }
}
