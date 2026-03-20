using System.Text;

namespace CascadeIDE.Features.AutonomousAgent;

/// <summary>
/// Состояние автономного прогона для resume: next-step и история,
/// чтобы продолжить без перезапуска с нуля.
/// </summary>
public sealed class AutonomousRunState
{
    public string Objective { get; set; } = "";
    public string SafetyLevel { get; set; } = "";
    public int MaxSteps { get; set; }

    public int NextStep { get; set; }

    /// <summary>История подсказок/наблюдений, на основе которой строится prompt.</summary>
    public List<string> History { get; } = [];

    public bool HasResumableSteps => NextStep > 0 && NextStep < MaxSteps;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Objective={Objective}");
        sb.AppendLine($"Safety={SafetyLevel}");
        sb.AppendLine($"NextStep={NextStep}/{MaxSteps}");
        sb.AppendLine($"HistoryLen={History.Count}");
        return sb.ToString();
    }
}

