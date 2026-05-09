#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Features.Os.DataAcquisition;

namespace CascadeIDE.Features.Git.DataAcquisition;

/// <summary>DAL: открытие пути в оболочке ОС (например папка в Explorer).</summary>
public static class ShellOpenPathLauncher
{
    [Obsolete("Use CascadeIDE.Features.Os.DataAcquisition.OsShellLauncher instead.")]
    public static void TryOpenInDefaultShell(string fullPath, Action<string>? onError = null)
    {
        OsShell.Default.TryOpen(fullPath, onError);
    }
}
