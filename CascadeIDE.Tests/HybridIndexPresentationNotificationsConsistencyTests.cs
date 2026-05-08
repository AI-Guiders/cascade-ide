using System.Collections.ObjectModel;
using System.Reflection;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// HSE: список <see cref="MainWindowViewModel"/> для <c>RaiseHybridIndexPresentationProperties</c> должен совпадать со
/// <see cref="NotifyPropertyChangedForAttribute"/> у поля снимка HCI — один источник правды без дрейфа.
/// </summary>
public sealed class HybridIndexPresentationNotificationsConsistencyTests
{
    [Fact]
    public void HybridIndexDependentPresentationNames_matches_observable_snapshot_notify_targets()
    {
        var observableField = typeof(MainWindowViewModel).GetField(
            "_hybridIndexLast",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(observableField);

        var dependencyArrayField = typeof(MainWindowViewModel).GetField(
            "HybridIndexDependentPresentationNames",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(dependencyArrayField);

        var fromArray = ((string[])dependencyArrayField!.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);

        var fromToolkit = CollectNotifyPropertyNames(observableField!);
        Assert.NotEmpty(fromToolkit);

        Assert.Equal(fromArray, fromToolkit);
    }

    private static HashSet<string> CollectNotifyPropertyNames(FieldInfo observableField)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var cad in observableField.GetCustomAttributesData())
        {
            if (cad.AttributeType != typeof(NotifyPropertyChangedForAttribute))
                continue;

            foreach (var arg in cad.ConstructorArguments)
                AddConstructorArgStrings(arg, set);
        }

        return set;
    }

    private static void AddConstructorArgStrings(CustomAttributeTypedArgument arg, HashSet<string> sink)
    {
        if (arg.Value is string s)
        {
            sink.Add(s);
            return;
        }

        if (arg.Value is ReadOnlyCollection<CustomAttributeTypedArgument> elems)
        {
            foreach (var item in elems)
                AddConstructorArgStrings(item, sink);
        }
    }
}
