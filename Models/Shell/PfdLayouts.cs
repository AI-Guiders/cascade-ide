namespace CascadeIDE.Models.Shell;

/// <summary>Известные расклады Pfd; v1 — один primary.</summary>
public static class PfdLayouts
{
    /// <summary>Текущий v1: один host slot, один кабинный прибор в колонке (без deck на уровне хоста).</summary>
    public const string PrimaryV1 = "pfd.layout.primary_v1";

    public static IPfdLayout Default { get; } = new PrimaryV1PfdLayout();

    private sealed class PrimaryV1PfdLayout : IPfdLayout
    {
        public string LayoutId => PrimaryV1;
    }
}
