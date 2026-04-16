using Xunit;

// Параллельный прогон отключён: статика InstrumentPlacementRuntime / UiModeCatalog и Avalonia headless ([AvaloniaFact]).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
