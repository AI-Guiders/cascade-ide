#nullable enable

using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Agent.Harness;

namespace CascadeIDE.ViewModels;

/// <summary>Agent harness: auto-verify coalescing after .cs writes (ADR 0166 interim).</summary>
public partial class MainWindowViewModel
{
    private AgentVerifyCoalescer? _autoVerifyCoalescer;

    private AgentVerifyCoalescer AutoVerifyCoalescer =>
        _autoVerifyCoalescer ??= new AgentVerifyCoalescer(
            _settings.Agent.Environment.CoalesceWindowMs,
            FireCoalescedAutoVerify);

    private void FireCoalescedAutoVerify()
    {
        var path = Workspace.SolutionPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!AgentVerifyPolicyParser.TryParse(_settings.Agent.Environment.DefaultVerifyPolicy, out var policy))
            policy = AgentVerifyPolicy.Standard;

        UiScheduler.Default.Post(() => _agentEnvironment.StartVerify(path, policy));
    }

    internal void MaybeScheduleAutoVerifyAfterCsWrite(string? filePath)
    {
        if (!_settings.Agent.Harness.AutoVerifyAfterCsWrite)
            return;

        if (string.IsNullOrWhiteSpace(filePath)
            || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(Workspace.SolutionPath))
            return;

        AutoVerifyCoalescer.Schedule();
    }

    internal bool ResolveAcpAutoInjectIdeMcp()
    {
        if (_settings.Agent.Harness.SuppressAcpIdeStdioInject)
            return false;

        return AcpAutoInjectIdeMcp;
    }
}
