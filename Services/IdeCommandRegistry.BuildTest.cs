using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: сборка (см. <c>IdeCommands.BuildTest.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterBuildPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Сборка
        AddPalette(b, "build_solution_ui", IdeCommands.BuildSolutionUi, "Собрать решение (UI)", "Сборка");
        AddPalette(b, "build_structured", IdeCommands.BuildStructured, "Сборка (JSON/structured)", "Сборка");
        AddPalette(b, "run_tests", IdeCommands.RunTests, "Запустить тесты", "Сборка");
    }
}
