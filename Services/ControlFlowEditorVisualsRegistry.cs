using System.Runtime.CompilerServices;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>Один генератор Virtual Spacing на <see cref="TextView"/>.</summary>
public static class ControlFlowEditorVisualsRegistry
{
    private static readonly ConditionalWeakTable<TextView, ControlFlowVirtualSpacingElementGenerator> SpacingByTextView = new();

    public static ControlFlowVirtualSpacingElementGenerator GetOrCreateSpacingGenerator(TextView textView)
    {
        if (!SpacingByTextView.TryGetValue(textView, out var gen))
        {
            gen = new ControlFlowVirtualSpacingElementGenerator();
            SpacingByTextView.Add(textView, gen);
        }

        return gen;
    }

    public static void InstallSpacingGenerator(TextView textView, ControlFlowVirtualSpacingElementGenerator generator)
    {
        var gens = textView.ElementGenerators;
        while (gens.Remove(generator)) { }

        int inlayIndex = -1;
        for (int i = 0; i < gens.Count; i++)
        {
            if (gens[i] is VarInlayHintElementGenerator)
            {
                inlayIndex = i;
                break;
            }
        }

        if (inlayIndex >= 0)
            gens.Insert(inlayIndex, generator);
        else
            gens.Insert(0, generator);
    }

    public static void RemoveSpacingGenerator(TextView textView, ControlFlowVirtualSpacingElementGenerator generator)
    {
        while (textView.ElementGenerators.Remove(generator)) { }
    }
}
