#nullable enable
using System.Text;

namespace CascadeIDE.Services;

internal enum MafRoutingGoal
{
    Unknown = 0,
    Coding,
    Debug,
    Review,
    Planning,
}

internal enum MafRoutingLevel
{
    Unknown = 0,
    System,
    Container,
    Component,
    Code,
}

internal sealed record MafPromptRoutingState(
    MafRoutingGoal Goal,
    MafRoutingLevel Level,
    int Uncertainty,
    int Risk,
    double Confidence,
    bool ToolHeavy,
    bool ErrorHeavy,
    bool PlanningHeavy,
    IReadOnlyList<string> Evidence);

internal sealed record MafPromptPackSelection(
    string Key,
    string Text,
    int Score,
    IReadOnlyList<string> Reasons);

internal sealed record MafPromptPackRouteResult(
    MafPromptRoutingState State,
    IReadOnlyList<MafPromptPackSelection> Selections);

internal static class MafPromptPackRouter
{
    private const int BaseSelectionThreshold = 48;
    private const double LowConfidenceFallbackThreshold = 0.28;
    private const double SessionHistoryDecay = 0.85;
    private static readonly object SessionHistoryGate = new();
    private static readonly Dictionary<string, double> SessionSuccessScores = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] CodingWords = ["implement", "feature", "refactor", "добав", "сделай", "реализ", "напис"];
    private static readonly string[] DebugWords = ["bug", "debug", "error", "stack", "fix", "падает", "ошибка", "почини", "не работает"];
    private static readonly string[] ReviewWords = ["review", "audit", "risk", "проверь", "ревью", "регресс"];
    private static readonly string[] PlanWords = ["plan", "strategy", "design", "архитект", "подход", "спланируй"];

    private static readonly string[] SystemWords = ["system", "архитектур", "end-to-end", "domain", "границ", "c4"];
    private static readonly string[] ContainerWords = ["service", "module", "subsystem", "слой", "контейнер"];
    private static readonly string[] ComponentWords = ["class", "component", "viewmodel", "handler", "метод", "класс"];
    private static readonly string[] CodeWords = [".cs", ".axaml", "line", "method", "property", "файл", "код"];

    private static readonly string[] UncertaintyWords = ["?", "maybe", "думаю", "наверное", "не увер", "кажется"];
    private static readonly string[] RiskWords = ["prod", "production", "migration", "security", "data loss", "critical", "боев", "безопас"];

    private sealed record PackProfile(
        string Key,
        MafRoutingGoal Goal,
        MafRoutingLevel MinLevel,
        string[] EvidenceWords);

    private static readonly IReadOnlyList<PackProfile> Profiles =
    [
        new("pack_mode_coding", MafRoutingGoal.Coding, MafRoutingLevel.Component, ["change", "implement", "реализ", "добав"]),
        new("pack_mode_debug", MafRoutingGoal.Debug, MafRoutingLevel.Component, ["error", "debug", "ошибка", "падает"]),
        new("pack_mode_review", MafRoutingGoal.Review, MafRoutingLevel.Component, ["review", "risk", "ревью", "регресс"]),
        new("pack_domain_secret_full", MafRoutingGoal.Planning, MafRoutingLevel.System, ["secret mode", "гипотеза", "доказ"]),
        new("pack_domain_csharp_roslyn", MafRoutingGoal.Coding, MafRoutingLevel.Code, [".cs", "roslyn", "diagnostic", "analyzer", "dotnet"]),
        new("pack_domain_git", MafRoutingGoal.Coding, MafRoutingLevel.Component, ["git", "commit", "push", "merge", "rebase"]),
    ];

    internal static MafPromptPackRouteResult Route(
        MafIdeAgentPrompts.PromptPack prompts,
        IReadOnlyList<ChatMessage> cascadeConversation,
        string? minimizedContextBlock,
        int budgetChars)
    {
        var query = (GetLastUserMessage(cascadeConversation) ?? "").Trim();
        var normalized = query.ToLowerInvariant();
        var context = (minimizedContextBlock ?? "").ToLowerInvariant();

        var state = InferState(cascadeConversation, normalized, context);
        var selections = SelectPacks(prompts, state, normalized, context, budgetChars);
        return new MafPromptPackRouteResult(state, selections);
    }

    internal static string FormatRoutingStateForPrompt(MafPromptRoutingState state)
    {
        var sb = new StringBuilder();
        sb.Append("Top-Down: goal=").Append(state.Goal)
            .Append(", level=").Append(state.Level)
            .Append(", uncertainty=").Append(state.Uncertainty)
            .Append(", risk=").Append(state.Risk)
            .Append(", toolHeavy=").Append(state.ToolHeavy ? "yes" : "no")
            .Append(", errorHeavy=").Append(state.ErrorHeavy ? "yes" : "no")
            .Append(", planningHeavy=").Append(state.PlanningHeavy ? "yes" : "no")
            .Append(", confidence=").Append(state.Confidence.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
            .AppendLine(".");
        if (state.Evidence.Count > 0)
            sb.Append("Bottom-Up evidence: ").Append(string.Join("; ", state.Evidence)).Append('.');
        return sb.ToString().TrimEnd();
    }

    private static MafPromptRoutingState InferState(IReadOnlyList<ChatMessage> cascadeConversation, string query, string context)
    {
        var evidence = new List<string>();
        var (toolHeavy, errorHeavy, planningHeavy) = AnalyzeRecentConversation(cascadeConversation, evidence);

        var goalScores = new Dictionary<MafRoutingGoal, int>
        {
            [MafRoutingGoal.Coding] = ScoreWords(query, CodingWords, evidence, "goal:coding"),
            [MafRoutingGoal.Debug] = ScoreWords(query, DebugWords, evidence, "goal:debug"),
            [MafRoutingGoal.Review] = ScoreWords(query, ReviewWords, evidence, "goal:review"),
            [MafRoutingGoal.Planning] = ScoreWords(query, PlanWords, evidence, "goal:planning"),
        };

        if (errorHeavy)
            goalScores[MafRoutingGoal.Debug] += 3;
        if (planningHeavy)
            goalScores[MafRoutingGoal.Planning] += 2;
        if (toolHeavy && !errorHeavy)
            goalScores[MafRoutingGoal.Coding] += 1;

        var levelScores = new Dictionary<MafRoutingLevel, int>
        {
            [MafRoutingLevel.System] = ScoreWords(query, SystemWords, evidence, "level:system"),
            [MafRoutingLevel.Container] = ScoreWords(query, ContainerWords, evidence, "level:container"),
            [MafRoutingLevel.Component] = ScoreWords(query, ComponentWords, evidence, "level:component"),
            [MafRoutingLevel.Code] = ScoreWords(query, CodeWords, evidence, "level:code"),
        };

        if (context.Contains(".cs", StringComparison.Ordinal) || context.Contains("using ", StringComparison.Ordinal))
        {
            levelScores[MafRoutingLevel.Code] += 2;
            evidence.Add("context:code-snippets");
        }

        var (goal, goalScore, goalSecond) = PickTop(goalScores);
        var (level, levelScore, levelSecond) = PickTop(levelScores);
        var uncertainty = Math.Min(100, ScoreWords(query, UncertaintyWords, evidence, "uncertainty") * 12 + (query.Contains('?') ? 15 : 0));
        var risk = Math.Min(100, ScoreWords(query, RiskWords, evidence, "risk") * 15);

        var confidenceRaw = ((goalScore - goalSecond) * 0.45) + ((levelScore - levelSecond) * 0.35) + (Math.Max(0, 40 - uncertainty) * 0.2);
        var confidence = Math.Clamp(confidenceRaw / 40.0, 0.05, 0.99);

        return new MafPromptRoutingState(goal, level, uncertainty, risk, confidence, toolHeavy, errorHeavy, planningHeavy, evidence);
    }

    private static List<MafPromptPackSelection> SelectPacks(
        MafIdeAgentPrompts.PromptPack prompts,
        MafPromptRoutingState state,
        string query,
        string context,
        int budgetChars)
    {
        if (state.Confidence < LowConfidenceFallbackThreshold)
            return [];

        var candidates = new List<MafPromptPackSelection>();
        foreach (var profile in Profiles)
        {
            if (!prompts.TryGetOptionalSection(profile.Key, out var sectionText))
                continue;

            var reasons = new List<string>();
            var score = 0;

            if (profile.Goal == state.Goal)
            {
                score += 40;
                reasons.Add("goal-match");
            }

            if (state.Level >= profile.MinLevel)
            {
                score += 22;
                reasons.Add("level-fit");
            }

            if (state.ErrorHeavy && profile.Key == "pack_mode_debug")
            {
                score += 15;
                reasons.Add("recent-error-heavy");
            }

            if (state.ToolHeavy && profile.Key is "pack_mode_coding" or "pack_domain_csharp_roslyn")
            {
                score += 8;
                reasons.Add("recent-tool-heavy");
            }

            if (state.PlanningHeavy && profile.Key == "pack_domain_secret_full")
            {
                score += 10;
                reasons.Add("recent-planning-heavy");
            }

            var evHits = CountHits(query, profile.EvidenceWords) + CountHits(context, profile.EvidenceWords);
            if (evHits > 0)
            {
                score += evHits * 9;
                reasons.Add("evidence-hits=" + evHits);
            }

            if (profile.Key == "pack_domain_secret_full" && state.Confidence < 0.55 && state.Uncertainty >= 20)
            {
                score += 12;
                reasons.Add("low-confidence-guardrail");
            }

            if (profile.Key == "pack_domain_csharp_roslyn" &&
                (query.Contains("c#", StringComparison.Ordinal) || query.Contains(".cs", StringComparison.Ordinal) ||
                 context.Contains(".cs", StringComparison.Ordinal)))
            {
                score += 18;
                reasons.Add("csharp-context");
            }

            var sessionBoost = GetSessionHistoryBoost(profile.Key);
            if (sessionBoost > 0)
            {
                score += sessionBoost;
                reasons.Add("session-history+" + sessionBoost);
            }

            if (profile.Key == "pack_mode_debug" && state.Goal == MafRoutingGoal.Planning)
            {
                score -= 8;
                reasons.Add("goal-mismatch-penalty");
            }

            if (score < BaseSelectionThreshold)
                continue;

            candidates.Add(new MafPromptPackSelection(profile.Key, sectionText.Trim(), score, reasons));
        }

        var ordered = candidates
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .ToList();

        var selected = new List<MafPromptPackSelection>();
        var used = 0;
        foreach (var candidate in ordered)
        {
            if (state.Confidence < 0.35 && candidate.Score < 55)
                continue;

            var projected = used + candidate.Text.Length;
            if (projected > budgetChars)
                continue;

            selected.Add(candidate);
            used = projected;
        }

        RegisterSelections(selected);
        return selected;
    }

    private static (bool ToolHeavy, bool ErrorHeavy, bool PlanningHeavy) AnalyzeRecentConversation(
        IReadOnlyList<ChatMessage> cascadeConversation,
        List<string> evidence)
    {
        var toolCount = 0;
        var errorCount = 0;
        var planningCount = 0;
        var considered = 0;

        for (var i = cascadeConversation.Count - 1; i >= 0 && considered < 8; i--)
        {
            var message = cascadeConversation[i];
            considered++;
            var role = (message.Role ?? "").Trim();
            var content = (message.Content ?? "").ToLowerInvariant();

            if (string.Equals(role, "tool", StringComparison.OrdinalIgnoreCase))
                toolCount++;
            if (content.Contains("error", StringComparison.Ordinal) || content.Contains("exception", StringComparison.Ordinal) ||
                content.Contains("ошибка", StringComparison.Ordinal) || content.Contains("failed", StringComparison.Ordinal))
                errorCount++;
            if (content.Contains("plan", StringComparison.Ordinal) || content.Contains("шаг", StringComparison.Ordinal) ||
                content.Contains("подход", StringComparison.Ordinal) || content.Contains("стратег", StringComparison.Ordinal))
                planningCount++;
        }

        var toolHeavy = considered > 0 && toolCount >= Math.Max(2, considered / 3);
        var errorHeavy = considered > 0 && errorCount >= Math.Max(1, considered / 4);
        var planningHeavy = considered > 0 && planningCount >= Math.Max(1, considered / 4);

        if (toolHeavy)
            evidence.Add($"recent:tool-heavy({toolCount}/{considered})");
        if (errorHeavy)
            evidence.Add($"recent:error-heavy({errorCount}/{considered})");
        if (planningHeavy)
            evidence.Add($"recent:planning-heavy({planningCount}/{considered})");

        return (toolHeavy, errorHeavy, planningHeavy);
    }

    private static int GetSessionHistoryBoost(string packKey)
    {
        lock (SessionHistoryGate)
        {
            foreach (var key in SessionSuccessScores.Keys.ToArray())
                SessionSuccessScores[key] *= SessionHistoryDecay;

            if (!SessionSuccessScores.TryGetValue(packKey, out var value))
                return 0;
            return (int)Math.Clamp(Math.Round(value), 0, 12);
        }
    }

    private static void RegisterSelections(IReadOnlyList<MafPromptPackSelection> selections)
    {
        if (selections.Count == 0)
            return;

        lock (SessionHistoryGate)
        {
            foreach (var selection in selections)
            {
                SessionSuccessScores.TryGetValue(selection.Key, out var current);
                SessionSuccessScores[selection.Key] = Math.Clamp(current + 3.0, 0.0, 24.0);
            }
        }
    }

    private static int ScoreWords(string text, IEnumerable<string> words, List<string> evidence, string tag)
    {
        var score = 0;
        foreach (var word in words)
        {
            if (!text.Contains(word, StringComparison.Ordinal))
                continue;
            score++;
            evidence.Add($"{tag}:{word}");
        }

        return score;
    }

    private static int CountHits(string text, IEnumerable<string> words)
    {
        var hits = 0;
        foreach (var word in words)
        {
            if (text.Contains(word, StringComparison.Ordinal))
                hits++;
        }

        return hits;
    }

    private static (T Key, int Max, int Second) PickTop<T>(IReadOnlyDictionary<T, int> map) where T : notnull
    {
        var maxKey = map.Keys.First();
        var max = int.MinValue;
        var second = int.MinValue;
        foreach (var pair in map)
        {
            if (pair.Value > max)
            {
                second = max;
                max = pair.Value;
                maxKey = pair.Key;
            }
            else if (pair.Value > second)
            {
                second = pair.Value;
            }
        }

        if (max <= 0)
            return (map.Keys.First(), 0, 0);
        if (second == int.MinValue)
            second = 0;
        return (maxKey, max, second);
    }

    private static string? GetLastUserMessage(IReadOnlyList<ChatMessage> cascadeConversation)
    {
        for (var i = cascadeConversation.Count - 1; i >= 0; i--)
        {
            var message = cascadeConversation[i];
            if (!string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
                continue;
            var text = (message.Content ?? "").Trim();
            if (text.Length > 0)
                return text;
        }

        return null;
    }
}
