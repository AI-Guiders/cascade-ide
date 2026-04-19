namespace CascadeIDE.Models;

/// <summary>Запись key-value в хранилище данных приложения (EF Core).</summary>
public sealed class AppDataItem
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}
