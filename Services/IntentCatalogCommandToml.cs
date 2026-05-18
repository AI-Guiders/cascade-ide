#nullable enable

namespace CascadeIDE.Services;

/// <remarks>TOML v2: <c>[[command]]</c> — якорь <c>command_id</c>; melody — поля на команде; slash — <c>[[command.form.slash]]</c>.</remarks>
internal sealed class CommandToml
{
    public string? CommandId { get; set; }
    public bool? Enabled { get; set; }
    public string? SlashGroup { get; set; }

    // Flat melody на [[command]] (предпочтительная раскладка; legacy — [command.melody])
    public string? MelodySlug { get; set; }
    public string? MelodyShape { get; set; }
    public bool? MelodyShowUsageHintIfBareSlug { get; set; }
    public string? MelodyTailSignature { get; set; }
    public string? MelodyWireClass { get; set; }
    public string? MelodyChordCommit { get; set; }
    public string? MelodyPaletteHintSlug { get; set; }
    public string? MelodyPaletteUsageHint { get; set; }
    public string? MelodyPaletteUsageCategory { get; set; }

    /// <summary>Legacy: <c>[command.melody]</c> / <c>[command.form.melody]</c>.</summary>
    public MelodyFormToml? Melody { get; set; }

    public CommandFormToml? Form { get; set; }

    /// <summary>Legacy: <c>[[command.slash]]</c>.</summary>
    public List<SlashFormToml>? Slash { get; set; }
}

internal sealed class CommandFormToml
{
    public MelodyFormToml? Melody { get; set; }
    public List<SlashFormToml>? Slash { get; set; }
}

/// <remarks>Форма <c>c:slug</c> (вложенная или плоские поля на <see cref="CommandToml"/>).</remarks>
internal sealed class MelodyFormToml
{
    public string? Slug { get; set; }
    public string? Shape { get; set; }
    public bool? ShowUsageHintIfBareSlug { get; set; }
    public string? TailSignature { get; set; }
    public string? WireClass { get; set; }
    public string? ChordCommit { get; set; }
    public string? PaletteHintSlug { get; set; }
    public string? PaletteUsageHint { get; set; }
    public string? PaletteUsageCategory { get; set; }
}

/// <remarks>Форма <c>/intent action</c> — <c>[[command.form.slash]]</c>.</remarks>
internal sealed class SlashFormToml
{
    public bool? Enabled { get; set; }
    public string? Path { get; set; }
    public string? Help { get; set; }
    public string? Group { get; set; }
    public string? Kind { get; set; }
    public SlashStaticArgsToml? Args { get; set; }
    public string? MfdPage { get; set; }
    public string? PrimarySurface { get; set; }
    /// <summary><c>workspace_files</c> — динамические подсказки файлов после пробела (ADR 0125).</summary>
    public string? Completion { get; set; }
    /// <summary>Обработчик локального отчёта при <c>kind=report</c> (см. <see cref="Chat.ChatSlashReportHandlers"/>).</summary>
    public string? ReportHandler { get; set; }
    /// <summary>Обработчик Intercom при <c>kind=intercom</c> (см. <see cref="Chat.ChatSlashIntercomHandlers"/>).</summary>
    public string? IntercomHandler { get; set; }
}

/// <remarks><c>[command.form.slash.args]</c> → JSON args исполнителя.</remarks>
internal sealed class SlashStaticArgsToml
{
    public string? Page { get; set; }
    public string? Surface { get; set; }
}
