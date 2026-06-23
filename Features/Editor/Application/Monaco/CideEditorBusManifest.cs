namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>CECB contract (ADR 0163 §2.2, §2.6). Shared with <c>bus-manifest.js</c>.</summary>
public static class CideEditorBusManifest
{
    public static class SetIds
    {
        public const string Diagnostics = "diagnostics";
        public const string Highlights = "highlights";
        public const string Breakpoints = "breakpoints";
        public const string DebugLine = "debugLine";
        public const string AgentReveal = "agentReveal";
        public const string CfGutter = "cfGutter";

        /// <summary>Legacy bridge id; maps to <see cref="DebugLine"/>.</summary>
        public const string DebugLineLegacy = "debug-line";

        /// <summary>Legacy bridge id; maps to <see cref="AgentReveal"/>.</summary>
        public const string AgentRevealLegacy = "agent-reveal";

        public static string Normalize(string? setId) =>
            setId switch
            {
                DebugLineLegacy => DebugLine,
                AgentRevealLegacy => AgentReveal,
                "cf-gutter" => CfGutter,
                _ => setId ?? "default",
            };
    }

    public static class Capabilities
    {
        public const string Completion = "capability/completion";
        public const string Hover = "capability/hover";
        public const string SignatureHelp = "capability/signatureHelp";
        public const string Definition = "capability/definition";
        public const string Navigate = "capability/navigate";
        public const string InlayHints = "capability/inlayHints";
        public const string CodeLens = "capability/codeLens";
        public const string CodeLensClick = "capability/codeLensClick";

        public const string CompletionResult = "capability/completionResult";
        public const string HoverResult = "capability/hoverResult";
        public const string SignatureResult = "capability/signatureResult";
        public const string DefinitionResult = "capability/definitionResult";
        public const string InlayHintsResult = "capability/inlayHintsResult";
        public const string CodeLensResult = "capability/codeLensResult";
    }

    public static class Editor
    {
        public const string SetModel = "editor/setModel";
        public const string ApplyEdits = "editor/applyEdits";
        public const string SetDecorations = "editor/setDecorations";
        public const string SetTheme = "editor/setTheme";
        public const string SetGutterGlyphs = "editor/setGutterGlyphs";
        public const string SetIntelligence = "editor/setIntelligence";
        public const string RevealRange = "editor/revealRange";
        public const string SetSelectionByOffset = "editor/setSelectionByOffset";
        public const string SetAgentReveal = "editor/setAgentReveal";
        public const string ClearAgentReveal = "editor/clearAgentReveal";
        public const string SetEpochDim = "editor/setEpochDim";
        public const string SetStickyScroll = "editor/setStickyScroll";
        public const string SetCfContentLane = "editor/setCfContentLane";
        public const string SetInlayHints = "editor/setInlayHints";

        public const string DidChange = "editor/didChange";
        public const string DidChangeCursorSelection = "editor/didChangeCursorSelection";
        public const string DidScroll = "editor/didScroll";
        public const string DidGutterClick = "editor/didGutterClick";
        public const string Ready = "editor/ready";
    }

    public static class Legacy
    {
        public const string RequestCompletion = "editor/requestCompletion";
        public const string CompletionResult = "editor/completionResult";
        public const string RequestHover = "editor/requestHover";
        public const string HoverResult = "editor/hoverResult";
        public const string RequestSignature = "editor/requestSignature";
        public const string SignatureResult = "editor/signatureResult";
    }

    public static string NormalizeInboundType(string? type) =>
        type switch
        {
            Legacy.RequestCompletion => Capabilities.Completion,
            Legacy.RequestHover => Capabilities.Hover,
            Legacy.RequestSignature => Capabilities.SignatureHelp,
            _ => type ?? "",
        };

    public static bool IsCapabilityRequest(string? type) =>
        type is Capabilities.Completion or Capabilities.Hover or Capabilities.SignatureHelp
            or Capabilities.Definition or Capabilities.InlayHints or Capabilities.CodeLens
            or Legacy.RequestCompletion or Legacy.RequestHover or Legacy.RequestSignature;

    public static bool IsCapabilitySideChannel(string? type) =>
        type is Capabilities.CodeLensClick;

    public static bool IsCapabilityResult(string? type) =>
        type is Capabilities.CompletionResult or Capabilities.HoverResult
            or Capabilities.SignatureResult or Capabilities.DefinitionResult
            or Capabilities.InlayHintsResult or Capabilities.CodeLensResult
            or Legacy.CompletionResult or Legacy.HoverResult or Legacy.SignatureResult;
}
