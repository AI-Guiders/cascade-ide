#nullable enable

namespace CascadeIDE.Models;

/// <summary>
/// Fluent-сборка списка <see cref="AnnunciatorLampItem"/> для полосы ламп (сахар над конструктором record).
/// </summary>
/// <example>
/// <code>
/// var items = AnnunciatorLampStripBuilder.Create()
///     .AddLamp(EnvironmentReadinessCellIds.MarkdownLsp, "MD", AnnunciatorLampLevel.Ok, "Markdown LSP", "")
///     .AddLamp(EnvironmentReadinessCellIds.CSharpLsp, "C#", AnnunciatorLampLevel.Caution, "C# LSP", "Не поднят")
///     .Build();
/// </code>
/// </example>
public sealed class AnnunciatorLampStripBuilder
{
    private readonly List<AnnunciatorLampItem> _items = [];

    public static AnnunciatorLampStripBuilder Create() => new();

    /// <param name="id">Стабильный id (<see cref="EnvironmentReadinessCellIds"/> или свой для прототипа).</param>
    /// <param name="lampShortLabel">Подпись на лампе.</param>
    /// <param name="title">Заголовок тултипа; по умолчанию = <paramref name="lampShortLabel"/>.</param>
    /// <param name="detail">Текст тултипа; по умолчанию пусто.</param>
    public AnnunciatorLampStripBuilder AddLamp(
        string id,
        string lampShortLabel,
        AnnunciatorLampLevel level,
        string? title = null,
        string? detail = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(lampShortLabel);

        _items.Add(new AnnunciatorLampItem(
            id,
            title ?? lampShortLabel,
            detail ?? "",
            level,
            lampShortLabel));
        return this;
    }

    public IReadOnlyList<AnnunciatorLampItem> Build() => _items;
}
