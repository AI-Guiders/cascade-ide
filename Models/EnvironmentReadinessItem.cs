namespace CascadeIDE.Models;

/// <summary>Уровень строки на экране «готовность окружения» (ADR 0023).</summary>
public enum EnvironmentReadinessLevel
{
    /// <summary>Условия выполнены.</summary>
    Ok,

    /// <summary>Стоит обратить внимание (нет решения, LSP не поднялся и т.д.).</summary>
    Warning,

    /// <summary>Информативно (например режим без отдельного LSP).</summary>
    Info,

    /// <summary>Инструмент недоступен (dotnet не найден и т.п.).</summary>
    Unavailable
}

/// <summary>Одна строка снимка готовности окружения (immutable).</summary>
public sealed record EnvironmentReadinessItem(
    string Title,
    string Detail,
    EnvironmentReadinessLevel Level);
