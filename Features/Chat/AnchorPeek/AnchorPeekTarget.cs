#nullable enable

namespace CascadeIDE.Features.Chat.AnchorPeek;

internal readonly record struct AnchorPeekTarget(AnchorPeekTargetKind Kind, int Ordinal, string HexId)
{
    public static AnchorPeekTarget FromOrdinal(int ordinal) =>
        new(AnchorPeekTargetKind.Ordinal, ordinal, "");

    public static AnchorPeekTarget FromHexId(string hexId) =>
        new(AnchorPeekTargetKind.HexId, 0, hexId);
}
