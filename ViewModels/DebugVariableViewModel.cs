namespace CascadeIDE.ViewModels;

/// <summary>Переменная для панели отладки.</summary>
public sealed class DebugVariableViewModel(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; } = value;

    public string DisplayText => $"{Name} = {Value}";
}
