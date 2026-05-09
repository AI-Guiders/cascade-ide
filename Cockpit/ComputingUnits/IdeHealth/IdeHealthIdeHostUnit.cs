namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;

/// <summary>
/// CCU «страт C / IDE host» (ADR 0097): из <see cref="IdeHostStateChanged"/> в <see cref="IdeHealthIdeHostInput"/> (краткая подсказка LSP).
/// </summary>
[ComputingUnit]
public sealed class IdeHealthIdeHostUnit : ICockpitComputeUnit
{
    public static IdeHealthIdeHostUnit Default { get; } = new();

    private IdeHealthIdeHostUnit()
    {
    }

    public IdeHealthIdeHostInput Compose(in IdeHostStateChanged state) =>
        Compose(state.CSharpLspProcessActive, state.MarkdownLspProcessActive);

    public IdeHealthIdeHostInput Compose(bool csharpLspActive, bool markdownLspActive)
    {
        var hint = (csharpLspActive, markdownLspActive) switch
        {
            (true, true) => "LSP · C# · MD",
            (true, false) => "LSP · C#",
            (false, true) => "LSP · MD",
            _ => (string?)null
        };
        return new IdeHealthIdeHostInput(LspStatusHint: hint);
    }
}
