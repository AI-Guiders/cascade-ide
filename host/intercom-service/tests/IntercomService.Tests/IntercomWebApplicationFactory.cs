using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntercomService.Tests;

public sealed class IntercomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "intercom-service-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Intercom:DataDirectory"] = _dataDir,
                ["Intercom:RecreateDatabaseOnStart"] = "true",
                ["DevAuth:TeamToken"] = "dev-intercom-local-change-me",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_dataDir))
        {
            try { Directory.Delete(_dataDir, recursive: true); }
            catch { /* best effort */ }
        }

        base.Dispose(disposing);
    }
}
