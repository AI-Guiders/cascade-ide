using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;
public sealed partial class ProblemsPanelViewModel : ObservableObject
{
    public int ErrorCount
    {
        get
        {
            var n = 0;
            foreach (var i in Items)
            {
                if (string.Equals(i.Severity, "error", StringComparison.OrdinalIgnoreCase))
                    n++;
            }

            return n;
        }
    }

    public int WarningCount
    {
        get
        {
            var n = 0;
            foreach (var i in Items)
            {
                if (string.Equals(i.Severity, "warning", StringComparison.OrdinalIgnoreCase))
                    n++;
            }

            return n;
        }
    }
}