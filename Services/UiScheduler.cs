namespace CascadeIDE.Services;

/// <summary>Доступ к планировщику UI-потока по умолчанию (до внедрения полноценного DI).</summary>
public static class UiScheduler
{
    public static IUiScheduler Default { get; } = new AvaloniaUiScheduler();
}
