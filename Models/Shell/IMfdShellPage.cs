using CascadeIDE.Models;

namespace CascadeIDE.Models.Shell;

/// <summary>Страница оболочки Mfd (<c>MfdShellView</c>); маппится на <see cref="MfdShellPage"/>.</summary>
public interface IMfdShellPage : IShellPage
{
    MfdShellPage Page { get; }
}
