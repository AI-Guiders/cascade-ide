using System.Reflection;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeCommandRegistryTests
{
    [Fact]
    public void WindowHotkeyByTomlKey_NoDuplicateEffectiveKeys()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in IdeCommandRegistry.AllEntries)
        {
            if (e.WindowHotkey is null)
                continue;
            var k = e.EffectiveHotkeysTomlKey;
            Assert.True(seen.Add(k), $"Дубликат ключа hotkeys.toml для окна: {k}");
        }
    }

    [Fact]
    public void TunnelShortcutKeyOrder_EveryKeyResolvesInWindowIndex()
    {
        foreach (var k in IdeCommandRegistry.TunnelShortcutKeyOrder)
        {
            Assert.True(
                IdeCommandRegistry.WindowHotkeyByTomlKey.ContainsKey(k),
                $"Tunnel order ссылается на неизвестный ключ: {k}");
        }
    }

    /// <summary>
    /// Обратная согласованность: tunnel не «забывает» ни одного окна-хоткея и не ссылается на лишние ключи.
    /// </summary>
    [Fact]
    public void TunnelShortcutKeyOrder_KeySet_Matches_WindowHotkeyByTomlKey()
    {
        var tunnel = IdeCommandRegistry.TunnelShortcutKeyOrder.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var window = IdeCommandRegistry.WindowHotkeyByTomlKey.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.True(
            tunnel.SetEquals(window),
            "Множество ключей tunnel ≠ множеству записей с WindowHotkey. " +
            $"Только в tunnel: {string.Join(", ", tunnel.Except(window))}. " +
            $"Только в окне: {string.Join(", ", window.Except(tunnel))}.");
    }

    [Fact]
    public void TunnelShortcutKeyOrder_HasNoDuplicateKeys()
    {
        var order = IdeCommandRegistry.TunnelShortcutKeyOrder;
        var distinct = order.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(order.Length, distinct.Count);
    }

    [Fact]
    public void PaletteEntries_HaveMcpCommandId()
    {
        foreach (var e in IdeCommandRegistry.AllEntries)
        {
            if (!e.IncludeInPalette)
                continue;
            Assert.False(string.IsNullOrEmpty(e.CommandId), $"Палитра без CommandId: {e.PaletteId}");
        }
    }

    /// <summary>
    /// Каждый непустой <see cref="IdeCommandRegistry.IdeCommandRegistryEntry.CommandId"/> — значение из констант <see cref="IdeCommands"/> (нет опечаток в строке).
    /// </summary>
    [Fact]
    public void Registry_CommandIds_AreIdeCommandsConstValues()
    {
        var validIds = GetIdeCommandsStringConstants();
        foreach (var e in IdeCommandRegistry.AllEntries)
        {
            if (string.IsNullOrEmpty(e.CommandId))
                continue;
            Assert.True(
                validIds.Contains(e.CommandId),
                $"CommandId не найден среди констант IdeCommands: «{e.CommandId}» (PaletteId={e.PaletteId}).");
        }
    }

    private static HashSet<string> GetIdeCommandsStringConstants()
    {
        var comparer = StringComparer.Ordinal;
        var set = new HashSet<string>(comparer);
        foreach (var fi in typeof(IdeCommands).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (fi.FieldType != typeof(string) || !fi.IsLiteral)
                continue;
            if (fi.GetRawConstantValue() is string s)
                set.Add(s);
        }

        return set;
    }
}
