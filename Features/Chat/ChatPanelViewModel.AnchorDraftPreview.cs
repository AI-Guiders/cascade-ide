#nullable enable

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private void scheduleAnchorDraftPreview(string text, int caret) =>
        _anchorDraftPreview?.Schedule(text, caret);

    private void clearAnchorDraftPreview() =>
        _anchorDraftPreview?.Clear();
}
