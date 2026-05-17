#nullable enable
using CascadeIDE.Features.Chat;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Тонкая обёртка над <see cref="ChatSurfaceEntityFactory"/>.</summary>
internal static class SkiaChatSceneBuilder
{
    public static IReadOnlyList<ISkiaChatEntity> Build(
        ChatSurfaceSnapshot snapshot,
        bool overviewMode,
        Guid detailThreadId) =>
        ChatSurfaceEntityFactory.Build(snapshot, overviewMode, detailThreadId);
}
