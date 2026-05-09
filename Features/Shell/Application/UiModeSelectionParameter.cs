using System.Globalization;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Shell.Application;

[ComputingUnit]
public static class UiModeSelectionParameter
{
    public static int ParseIndex(object? parameter) =>
        parameter switch
        {
            int i => i,
            long l => l > int.MaxValue ? -1 : (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var j) => j,
            _ => -1,
        };
}
