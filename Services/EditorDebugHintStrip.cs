namespace CascadeIDE.Services;

/// <summary>Подсказка отладки в конце логической строки редактора.</summary>
public readonly record struct EditorDebugHintStrip(int Line1, string Label);
