#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseWork
{
    public required string RawLine { get; init; }

    public string Body { get; set; } = "";

    public string HeadToken { get; set; } = "";

    public string Head { get; set; } = "";

    public string? InlineAction { get; set; }

    public string Tail { get; set; } = "";

    public string Action { get; set; } = "";

    public string Args { get; set; } = "";

    public string TopicArgsBeforeNormalize { get; set; } = "";
}
