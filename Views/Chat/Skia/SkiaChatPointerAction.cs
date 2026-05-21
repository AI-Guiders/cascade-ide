#nullable enable

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Действие pointer hit вне ленты сообщений (composer) или спец. зоны.</summary>
internal enum SkiaChatPointerAction
{
    None = 0,
    ComposerSend = 1,
    ComposerFocus = 2,
    SlashPopup = 3,
    OverviewToggle = 4,
    CommandLineFocus = 5,
}
