using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Contracts.Experimental.Capabilities;

namespace CascadeIDE.Services.Capabilities;

/// <summary>Простая in-memory реализация <see cref="ICapabilityRegistry"/> для v1.</summary>
public sealed class SimpleCapabilityRegistry : ICapabilityRegistry
{
    private readonly List<ServiceCapabilityDescriptor> _services = [];
    private readonly List<CommandCapabilityDescriptor> _commands = [];
    private readonly List<UiSurfaceCapabilityDescriptor> _uiSurfaces = [];

    public void RegisterService(ServiceCapabilityDescriptor descriptor) => _services.Add(descriptor);
    public void RegisterCommand(CommandCapabilityDescriptor descriptor) => _commands.Add(descriptor);
    public void RegisterUiSurface(UiSurfaceCapabilityDescriptor descriptor) => _uiSurfaces.Add(descriptor);

    public CapabilityMap BuildMap()
    {
        var map = new CapabilityMap
        {
            Services = _services.ToArray(),
            Commands = _commands.ToArray(),
            UiSurfaces = _uiSurfaces.ToArray()
        };

        return map with { Hash = ComputeHash(map) };
    }

    private static string ComputeHash(CapabilityMap map)
    {
        // Canonical-ish JSON to stabilize hash across runs.
        var json = JsonSerializer.Serialize(map, new JsonSerializerOptions
        {
            WriteIndented = false
        });
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}

