#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Справка Intercom для <c>/help</c>. Текст — <see cref="BundledRelativePath"/> (диск под exe, затем EmbeddedResource).
/// </summary>
public static class IntercomHelpGuide
{
    /// <summary>Относительно <see cref="AppContext.BaseDirectory"/>; override без пересборки.</summary>
    public const string BundledRelativePath = "Intercom/intercom-help.ru.md";

    private static readonly Lazy<string> Cached = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string FormatFull() => Cached.Value;

    private static string Load()
    {
        if (BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var raw)
            && !string.IsNullOrWhiteSpace(raw))
        {
            return raw.TrimEnd();
        }

        throw new InvalidOperationException(
            $"Отсутствует или пустой {BundledRelativePath} (рядом с exe или EmbeddedResource сборки CascadeIDE).");
    }
}
