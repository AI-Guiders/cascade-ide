using System.Globalization;

namespace CascadeIDE.ViewModels;

/// <summary>Structured trace step for Power-mode timeline cards.</summary>
public sealed class AgentTraceStepViewModel
{
    public AgentTraceStepViewModel(string kind, string text, string status, DateTimeOffset? at = null)
    {
        Kind = kind;
        Text = text;
        Status = status;
        var t = at ?? DateTimeOffset.Now;
        TimestampText = t.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }

    public string Kind { get; }
    public string Text { get; }
    public string Status { get; }
    public string TimestampText { get; }

    public bool IsPlan => string.Equals(Kind, "PLAN", StringComparison.OrdinalIgnoreCase);
    public bool IsAction => string.Equals(Kind, "ACTION", StringComparison.OrdinalIgnoreCase);
    public bool IsObservation => string.Equals(Kind, "OBSERVATION", StringComparison.OrdinalIgnoreCase);
    public bool IsNext => string.Equals(Kind, "NEXT", StringComparison.OrdinalIgnoreCase);

    public bool IsSuccess => string.Equals(Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
    public bool IsWarning => string.Equals(Status, "WARNING", StringComparison.OrdinalIgnoreCase);
    public bool IsPending => string.Equals(Status, "PENDING", StringComparison.OrdinalIgnoreCase);
}
