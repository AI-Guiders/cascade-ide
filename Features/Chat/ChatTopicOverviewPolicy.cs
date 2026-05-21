namespace CascadeIDE.Features.Chat;

/// <summary>Правила adaptive default для overview/detail ([ADR 0072 §3](docs/adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md)).</summary>
internal static class ChatTopicOverviewPolicy
{
    public static bool ResolveNextOverviewMode(int threadCount, int lastOverviewThreadCount, bool currentOverviewMode)
    {
        if (threadCount == lastOverviewThreadCount)
            return currentOverviewMode;

        // ADR 0127: вкладки тем — daily path в detail; overview остаётся для картотеки.
        if (lastOverviewThreadCount < 0)
            return false;

        if (threadCount <= 1)
            return false;

        if (lastOverviewThreadCount <= 1)
            return true;

        return currentOverviewMode;
    }

    public static void ApplyAdaptiveDefault(int threadCount, ref int lastOverviewThreadCount, Action<bool> setOverviewMode, Func<bool> getOverviewMode)
    {
        if (threadCount == lastOverviewThreadCount)
            return;

        var next = ResolveNextOverviewMode(threadCount, lastOverviewThreadCount, getOverviewMode());
        lastOverviewThreadCount = threadCount;
        if (next != getOverviewMode())
            setOverviewMode(next);
    }
}
