namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Событие hi-freq контура ADR 0103: не публикуется в <c>IDataBus</c> без сжатия.
/// </summary>
public enum EditorInputDeltaKind
{
    /// <summary>Каретка или выделение.</summary>
    CaretOrSelection = 0,

    /// <summary>Изменён текст документа (нужен для согласованного смещения каретки / будущей семантики).</summary>
    DocumentText = 1,
}

/// <summary>Единица потока «редактор → стабилизация → CCU/VM»; capacity 1 + drop-oldest на границе.</summary>
public readonly record struct EditorInputDelta(
    string? FilePath,
    int CaretOffset,
    int SelectionStart,
    int SelectionLength,
    EditorInputDeltaKind Kind);
