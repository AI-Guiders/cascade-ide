#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Серьёзность debounced slash-preview в TCI (CCL, composer). Отдельный контур от EICAS W/C/A (ADR 0021).
/// </summary>
public enum SlashCommandPreviewKind
{
    None = 0,
    /// <summary>Ready — команда и args готовы (зелёный).</summary>
    Ok = 1,
    /// <summary>Hint — мягкая подсказка (серый).</summary>
    Hint = 2,
    /// <summary>Incomplete — не хватает args / id допечатывается (янтарь).</summary>
    Incomplete = 3,
    /// <summary>Invalid — нет команды, синтаксис (красный).</summary>
    Error = 4,
}
