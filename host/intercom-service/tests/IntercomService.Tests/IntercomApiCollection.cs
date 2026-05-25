using IntercomService.Tests;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IntercomService.Tests;

[CollectionDefinition("IntercomApi")]
public sealed class IntercomApiCollection : ICollectionFixture<IntercomWebApplicationFactory>;
