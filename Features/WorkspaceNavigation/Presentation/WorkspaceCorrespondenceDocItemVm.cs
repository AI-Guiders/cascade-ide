#nullable enable

namespace CascadeIDE.Features.WorkspaceNavigation.Presentation;

public sealed class WorkspaceCorrespondenceDocItemVm
{
    public required string DocPath { get; init; }

    public required string DisplayTitle { get; init; }

    public string DisplayLine => $"{DisplayTitle} — {DocPath}";
}
