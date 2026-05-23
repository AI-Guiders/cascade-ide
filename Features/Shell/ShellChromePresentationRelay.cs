namespace CascadeIDE.Features.Shell;

/// <summary>
/// Зависимые presentation-свойства на <see cref="ViewModels.MainWindowViewModel"/> при смене полей <see cref="ShellChromeViewModel"/>.
/// </summary>
internal static class ShellChromePresentationRelay
{
    public static ReadOnlySpan<string> GetDependents(string sourcePropertyName) =>
        ShellChromePresentationNotifyMap.GetDependents(sourcePropertyName);
}
