#nullable enable

using System.IO;
using System.Text;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

internal static partial class GlassFdsGlance
{
    static void AppendIgniteWake(StringBuilder sb)
    {
        var path = CdpHabitatPaths.IgniteWakeLatchPath;
        if (!File.Exists(path))
        {
            AppendMark(sb, "wake", false);
            return;
        }

        try
        {
            var view = LatchPaint.PaintIgniteWake(File.ReadAllText(path));
            if (view is null)
            {
                AppendMark(sb, "wake", true);
                return;
            }

            sb.AppendLine($"│ WAKE  {Truncate(view.ChromeHint, 28)}");
            if (!string.IsNullOrWhiteSpace(view.Task))
                sb.AppendLine($"│   › {Truncate(view.Task, 26)}");
        }
        catch
        {
            AppendMark(sb, "wake", true);
        }
    }
}
