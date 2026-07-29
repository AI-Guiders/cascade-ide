using CascadeIDE.Features.Cdp;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Schema/fingerprint contract for seats-LATEST (CDP writer / CIDE reader).</summary>
public class CabinSeatsLatchProjectionTests
{
    [Fact]
    public void Schema_matches_cdp_writer()
    {
        Assert.Equal("cide_seats_latch/v1", CdpSeatsProjector.Schema);
        Assert.Equal("agent", CdpSeatsProjector.OriginAgent);
        Assert.EndsWith("seats-LATEST.json", CdpSeatsProjector.LatchPath);
    }
}
