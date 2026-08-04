#nullable enable
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace CDP.GlassCockpit.Windows;

/// <summary>Global CFG UI scale steps (0.85 / 1.0 / 1.15 / 1.3). Zone scale later.</summary>
public static class GlassUiScale
{
    public static readonly double[] Steps = [0.85, 1.0, 1.15, 1.3];

    public static double Current { get; private set; } = 1.0;

    static string PrefPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CascadeIDE",
            "glass-ui-scale.txt");

    public static double Load()
    {
        try
        {
            if (File.Exists(PrefPath)
                && double.TryParse(
                    File.ReadAllText(PrefPath).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var v))
            {
                Current = Nearest(v);
                return Current;
            }
        }
        catch
        {
            /* ignore */
        }

        Current = 1.0;
        return Current;
    }

    public static double CycleNext()
    {
        var i = Array.FindIndex(Steps, s => Math.Abs(s - Current) < 0.001);
        if (i < 0)
            i = 1;
        Current = Steps[(i + 1) % Steps.Length];
        Save(Current);
        return Current;
    }

    public static void Apply(FrameworkElement? root, double scale)
    {
        if (root is null)
            return;
        Current = Nearest(scale);
        root.LayoutTransform = Math.Abs(Current - 1.0) < 0.001
            ? Transform.Identity
            : new ScaleTransform(Current, Current);
    }

    public static string ChipLabel(double scale) =>
        "CFG · " + scale.ToString("0.##", CultureInfo.InvariantCulture) + "×";

    static void Save(double scale)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!);
            File.WriteAllText(PrefPath, scale.ToString("0.##", CultureInfo.InvariantCulture));
        }
        catch
        {
            /* ignore */
        }
    }

    static double Nearest(double v)
    {
        var best = Steps[0];
        var bestD = Math.Abs(v - best);
        foreach (var s in Steps)
        {
            var d = Math.Abs(v - s);
            if (d < bestD)
            {
                best = s;
                bestD = d;
            }
        }

        return best;
    }
}
