using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: сборка (см. <c>IdeCommands.BuildTest.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterBuildPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Сборка
        AddPalette(b, "build_solution_ui", IdeCommands.BuildSolutionUi, "Собрать решение (UI)", "Сборка");
        AddPalette(b, "build", IdeCommands.Build, "Сборка (structured)", "Сборка");
    }
}
