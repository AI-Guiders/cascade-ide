#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Тонкая обёртка над <see cref="ChatSurfaceEntityFactory"/>.</summary>
internal static class SkiaChatSceneBuilder
{
    public static IReadOnlyList<ISkiaChatEntity> Build(
        ChatSurfaceSnapshot snapshot,
        bool overviewMode,
        Guid detailThreadId,
        bool forwardHost = false,
        IntercomFontsSettings? intercomFonts = null,
        bool compactSideHost = false) =>
        ChatSurfaceEntityFactory.Build(snapshot, overviewMode, detailThreadId, forwardHost, intercomFonts, compactSideHost);
}
