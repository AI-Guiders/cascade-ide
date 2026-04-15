using System.Diagnostics;
using Avalonia.Controls;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private string? _lastMainGridGeometrySnapshot;

    /// <summary>Применяет строку колонок из <see cref="ViewModels.MainWindowViewModel.MainGridColumnDefinitions"/> (веса из <c>presentation</c>, ADR 0017).</summary>
    private void ApplyMainGridColumnDefinitions(ViewModels.MainWindowViewModel vm)
    {
        try
        {
            MainGrid.ColumnDefinitions = ColumnDefinitions.Parse(vm.MainGridColumnDefinitions);
            LogMainGridGeometrySnapshot(vm);
        }
        catch
        {
            MainGrid.ColumnDefinitions = ColumnDefinitions.Parse(PresentationMainGridColumnDefinitions.Default);
        }
    }

    [Conditional("DEBUG")]
    private void LogMainGridGeometrySnapshot(ViewModels.MainWindowViewModel vm)
    {
        var frame = vm.MainGridLayoutFrame;
        var weights = frame.NormalizedZoneWeights.Count == 0
            ? "-"
            : string.Join(",", frame.NormalizedZoneWeights.Select(static w => w.ToString("0.###")));
        var bounds = frame.ZoneBounds.Count == 0
            ? "-"
            : string.Join(
                ";",
                frame.ZoneBounds.Select(static b =>
                    $"{b.Zone}:{b.StartNormalized:0.###}+{b.WidthNormalized:0.###}"));

        var snapshot = $"presentation='{vm.EffectivePresentationLine}' cols='{frame.ColumnDefinitions}' zones={frame.ContentZoneCount} weighted={frame.HasExplicitWeights} weights=[{weights}] bounds=[{bounds}]";
        if (string.Equals(snapshot, _lastMainGridGeometrySnapshot, StringComparison.Ordinal))
            return;

        _lastMainGridGeometrySnapshot = snapshot;
        Debug.WriteLine($"[MainGridGeometry] {snapshot}");
    }
}
