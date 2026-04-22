using CascadeIDE.Models;

namespace CascadeIDE.Models.Shell;

/// <summary>Дескриптор страницы Mfd для API, не заменяет enum в состоянии VM.</summary>
public readonly record struct MfdShellPageDescriptor(MfdShellPage Page) : IMfdShellPage
{
    public string ShellSurfaceId => "mfd." + Page.ToString("G", System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
}

public static class MfdShellPageShellExtensions
{
    public static IMfdShellPage AsShellPage(this MfdShellPage page) => new MfdShellPageDescriptor(page);
}
