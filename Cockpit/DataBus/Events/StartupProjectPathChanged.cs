namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Текущий стартовый .csproj для F5/scope в IDE Health (null = не задан).</summary>
public readonly record struct StartupProjectPathChanged(string? ProjectPath);
