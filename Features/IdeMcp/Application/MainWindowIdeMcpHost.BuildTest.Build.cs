using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    /// <summary>РљРѕРґ РІС‹С…РѕРґР° Рё СѓСЃРїРµС… РґР»СЏ С„РёРЅР°Р»СЊРЅРѕРіРѕ <see cref="BuildStateChanged"/> РїРѕСЃР»Рµ MCP-РѕРїРµСЂР°С†РёРё РЅР° РїР°РЅРµР»Рё СЃР±РѕСЂРєРё.</summary>
    private sealed class IdeMcpMutableBuildPhaseOutcome
    {
        public int? ExitCode;
        public bool? Succeeded;
    }

    /// <summary>
    /// РџР°СЂР° СЃРѕР±С‹С‚РёР№ В«СЃР±РѕСЂРєР° РёРґС‘С‚В» / В«СЃР±РѕСЂРєР° Р·Р°РІРµСЂС€РёР»Р°СЃСЊВ» РЅР° IDE DataBus (ADR 0099); С‚РµР»Рѕ Р·Р°РґР°С‘С‚ <see cref="IdeMcpMutableBuildPhaseOutcome"/> РґРѕ return.
    /// </summary>
    private async Task<T> WithIdeMcpPublishedBuildStateAsync<T>(Func<IdeMcpMutableBuildPhaseOutcome, Task<T>> body)
    {
        await _host.PublishIdeBuildStateOnUiAsync(new BuildStateChanged(true)).ConfigureAwait(false);
        var outcome = new IdeMcpMutableBuildPhaseOutcome();
        try
        {
            return await body(outcome).ConfigureAwait(false);
        }
        finally
        {
            await _host.PublishIdeBuildStateOnUiAsync(new BuildStateChanged(false, outcome.ExitCode, outcome.Succeeded))
                .ConfigureAwait(false);
        }
    }

    /// <summary>Р”РёР°РіРЅРѕСЃС‚РёРєРё РѕС‚РєСЂС‹С‚РѕРіРѕ .cs С„Р°Р№Р»Р° (РѕС€РёР±РєРё Рё РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёСЏ Roslyn). JSON: РјР°СЃСЃРёРІ { id, message, severity, line, column }. Р”Р»СЏ РЅРµ-C# РёР»Рё РїСЂРё РѕС‚СЃСѓС‚СЃС‚РІРёРё С„Р°Р№Р»Р° вЂ” [].</summary>
    public async Task<string> GetCurrentFileDiagnosticsAsync()
    {
        var (path, text) = await UiScheduler.Default.InvokeAsync(() => (_host.CurrentFilePath ?? "", _host.EditorText ?? ""));
        return await Task.Run(() => _host.McpContextMinimizer.GetDiagnosticsJson(path, text)).ConfigureAwait(false);
    }

    /// <summary>РЎРїРёСЃРѕРє С„Р°Р№Р»РѕРІ Рё РґРµСЂРµРІРѕ СЂРµС€РµРЅРёСЏ. file_entries вЂ” РїР»РѕСЃРєРёР№ СЃРїРёСЃРѕРє СЃ path, title, relative_path. solution_tree вЂ” РёРµСЂР°СЂС…РёСЏ (solution в†’ projects в†’ folders в†’ files). Р’С‹РїРѕР»РЅСЏРµС‚СЃСЏ РІ UI-РїРѕС‚РѕРєРµ.</summary>
    public Task<string> GetSolutionFilesAsync() =>
        UiScheduler.Default.InvokeAsync(() =>
            IdeMcpBuildTestOrchestrator.BuildSolutionFilesJson(_host.Workspace.SolutionPath, _host.Workspace.SolutionRoots));
    public async Task<string> BuildAsync()
    {
        var path = await UiScheduler.Default.InvokeAsync(() => _host.Workspace.SolutionPath ?? "");
        if (!IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(path))
        {
            var surf = IdeMcpBuildTestOrchestrator.BuildMissingSolutionPanelSurface();
            UiScheduler.Default.Post(() =>
            {
                _host.BuildOutputPanel.Set(surf.BuildOutputPanelFullText);
                _host.IsBuildOutputVisible = true;
            });
            return surf.McpReplyText;
        }
        var pathCopy = path;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            _host.BuildOutputPanel.Set(IdeMcpBuildTestOrchestrator.BuildOperationHeader("РЎР±РѕСЂРєР°", pathCopy));
            _host.IsBuildOutputVisible = true;
        }).ConfigureAwait(false);

        return await WithIdeMcpPublishedBuildStateAsync(async outcome =>
        {
            try
            {
                void AppendBuildChunk(string chunk) => _host.BuildOutputPanel.Append(chunk);
                var (outStr, success, exitCode, binlogPath) = await _host.McpBuildTest
                    .BuildWithBinlogAsync(path, AppendBuildChunk, cancellationToken: default)
                    .ConfigureAwait(false);
                outcome.ExitCode = exitCode;
                outcome.Succeeded = success;

                await UiScheduler.Default.InvokeAsync(() =>
                {
                    _host.BuildOutputPanel.FlushPending();
                    _host.McpLastBuildBinlogPath = binlogPath;
                }).ConfigureAwait(false);
                return outStr;
            }
            catch (Exception ex)
            {
                outcome.Succeeded = false;
                var surf = IdeMcpBuildTestOrchestrator.FailedBuildPanelSurface(ex.Message);
                UiScheduler.Default.Post(() =>
                {
                    _host.BuildOutputPanel.Set(surf.BuildOutputPanelFullText);
                    _host.IsBuildOutputVisible = true;
                });
                return surf.McpReplyText;
            }
        }).ConfigureAwait(false);
    }
    public async Task<string> BuildStructuredAsync()
    {
        var raw = await this.BuildAsync().ConfigureAwait(false);
        return Services.McpDotnetBuildTestService.SerializeStructuredBuild(raw, _host.McpLastBuildBinlogPath);
    }
    public async Task<string> RunCodeCleanupAsync(string? includePath)
    {
        var path = await UiScheduler.Default.InvokeAsync(() => _host.Workspace.SolutionPath ?? "");
        if (!IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(path))
            return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupFailure(IdeMcpBuildTestOrchestrator.MissingSolutionMessage());

        var pathCopy = path;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            _host.BuildOutputPanel.Set(IdeMcpBuildTestOrchestrator.BuildOperationHeader("Code cleanup", pathCopy));
            _host.IsBuildOutputVisible = true;
        }).ConfigureAwait(false);

        return await WithIdeMcpPublishedBuildStateAsync(async outcome =>
        {
            try
            {
                void AppendBuildChunk(string chunk) => _host.BuildOutputPanel.Append(chunk);
                var (success, exitCode, outStr) = await _host.McpBuildTest
                    .RunCodeCleanupAsync(path, includePath, AppendBuildChunk, cancellationToken: default)
                    .ConfigureAwait(false);
                outcome.ExitCode = exitCode;
                outcome.Succeeded = success;
                var rawTruncated = IdeMcpBuildTestOrchestrator.BuildTruncatedRawOutput(outStr, 4000);

                await UiScheduler.Default.InvokeAsync(() => _host.BuildOutputPanel.FlushPending()).ConfigureAwait(false);

                return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupResult(success, exitCode, rawTruncated);
            }
            catch (Exception ex)
            {
                outcome.Succeeded = false;
                return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupFailure(ex.Message);
            }
        }).ConfigureAwait(false);
    }

}
