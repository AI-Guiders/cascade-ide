using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SettingsLegacyStringDefaultsTests
{
    [Fact]
    public void Apply_fills_empty_intercom_transport_and_hybrid_index()
    {
        var settings = new CascadeIdeSettings
        {
            Intercom = new IntercomSettings
            {
                Transport = new IntercomTransportSettings
                {
                    BaseUrl = "",
                    LocalServerPath = "",
                },
            },
            HybridIndex = new HybridIndexSettings { IndexDir = "" },
        };

        SettingsLegacyStringDefaults.Apply(settings);

        Assert.Equal(IntercomTransportSettings.DefaultBaseUrl, settings.Intercom.Transport.BaseUrl);
        Assert.Equal(IntercomTransportSettings.DefaultLocalServerRelativePath, settings.Intercom.Transport.LocalServerPath);
        Assert.Equal(".hybrid-codebase-index", settings.HybridIndex.IndexDir);
    }

    [Fact]
    public void Embedded_defaults_toml_includes_agent_environment_and_local_server_path()
    {
        var text = SettingsDefaultsLoader.GetEmbeddedDefaultsToml();
        Assert.Contains("[agent.environment]", text, StringComparison.Ordinal);
        Assert.Contains("local_server_path = \"tools/intercom-service/IntercomService.exe\"", text, StringComparison.Ordinal);
        Assert.Contains("executable_env = \"PATH\"", text, StringComparison.Ordinal);
        Assert.Contains("base_url = \"http://127.0.0.1:5080\"", text, StringComparison.Ordinal);
    }
}
