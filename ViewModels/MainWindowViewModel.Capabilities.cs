using CascadeIDE.Services.Capabilities;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Реестр capabilities.</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task DumpCapabilitiesMapAsync()
    {
        try
        {
            var map = _capabilities.BuildMap();
            var path = CapabilityDump.WriteCapabilityMapToFile(map);
            if (RequestShowInfoAsync is not null)
                await RequestShowInfoAsync("Capabilities dump", $"Saved: {path}");
        }
        catch (Exception ex)
        {
            if (RequestShowInfoAsync is not null)
                await RequestShowInfoAsync("Capabilities dump failed", ex.Message);
        }
    }
}

