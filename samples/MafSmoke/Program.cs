using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// ADR 0087 PoC: Microsoft Agent Framework + Ollama (local), one tool invocation.
// Prereqs: Ollama running, model pulled (e.g. ollama pull llama3.2).
// Env: OLLAMA_ENDPOINT (optional, default http://localhost:11434), OLLAMA_MODEL (optional, default llama3.2).

var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
var uri = string.IsNullOrWhiteSpace(endpoint)
    ? new Uri("http://localhost:11434")
    : new Uri(endpoint);
var modelId = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";

OllamaChatClient chatClient = new(uri, modelId);

AIAgent agent = chatClient.AsAIAgent(
    instructions:
    "You are a concise assistant. When the user asks for a rollout plan id, "
    + "you must call the get_rollout_plan tool and summarize its reply in one sentence.",
    tools:
    [
        AIFunctionFactory.Create(GetRolloutPlan),
    ]);

Console.WriteLine(await agent.RunAsync("Give me rollout plan RP-042 status."));

/// <summary>Stub tool proving MAF ⇄ tool round-trip (ADR 0087 PoC).</summary>
[Description("Looks up a fake rollout plan id for Cascade IDE plumbing checks.")]
static string GetRolloutPlan([Description("Plan id such as RP-042.")] string planId)
    => $"{planId}: stub OK (MAF smoke tool invoked).";
