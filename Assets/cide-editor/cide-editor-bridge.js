/* CIDE Monaco host bridge (ADR 0162). Offline Monaco under monaco/min/vs; posts JSON to WebView2. */
(function () {
  'use strict';

  const root = typeof window !== 'undefined' ? window : globalThis;

  const MONACO_VS = 'monaco/min/vs';
  const BUS = {
    setIds: {
      diagnostics: 'diagnostics',
      highlights: 'highlights',
      breakpoints: 'breakpoints',
      debugLine: 'debugLine',
      agentReveal: 'agentReveal',
      cfGutter: 'cfGutter',
    },
    capabilities: {
      completion: 'capability/completion',
      hover: 'capability/hover',
      signatureHelp: 'capability/signatureHelp',
      definition: 'capability/definition',
      inlayHints: 'capability/inlayHints',
      codeLens: 'capability/codeLens',
      codeLensClick: 'capability/codeLensClick',
      semanticTokens: 'capability/semanticTokens',
      completionResult: 'capability/completionResult',
      hoverResult: 'capability/hoverResult',
      signatureResult: 'capability/signatureResult',
      definitionResult: 'capability/definitionResult',
      inlayHintsResult: 'capability/inlayHintsResult',
      codeLensResult: 'capability/codeLensResult',
      semanticTokensResult: 'capability/semanticTokensResult',
    },
  };

  function normalizeSetId(setId) {
    if (setId === 'debug-line') return BUS.setIds.debugLine;
    if (setId === 'agent-reveal') return BUS.setIds.agentReveal;
    if (setId === 'cf-gutter') return BUS.setIds.cfGutter;
    return setId || 'default';
  }

  /** @type {import('monaco-editor').editor.IStandaloneCodeEditor | null} */
  let editor = null;
  let modelVersion = 0;
  let suppressChange = false;
  let intelligenceEnabled = true;
  const decorationSets = new Map();
  const decorationLayerManager = root.CideDecorationLayerManager
    ? root.CideDecorationLayerManager.create()
    : null;
  const pendingRequests = new Map();
  let cfGlyphStyleEl = null;
  let disposables = [];
  let hostInlayHints = [];
  let hostSemanticLegend = null;
  let cfLaneActive = false;

  function postToHost(msg) {
    const body = JSON.stringify(msg);
    if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
      window.chrome.webview.postMessage(body);
      return;
    }
    console.log('host←', body);
  }

  function offsetFromPosition(model, lineNumber, column) {
    return model.getOffsetAt({ lineNumber, column });
  }

  function publishModelState(reason) {
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;
    const sel = editor.getSelection();
    const caretOffset = offsetFromPosition(model, sel.positionLineNumber, sel.positionColumn);
    const selStart = offsetFromPosition(model, sel.startLineNumber, sel.startColumn);
    const selEnd = offsetFromPosition(model, sel.endLineNumber, sel.endColumn);
    postToHost({
      type: reason === 'change' ? 'editor/didChange' : 'editor/didChangeCursorSelection',
      version: modelVersion,
      text: model.getValue(),
      caretOffset,
      selectionStart: selStart,
      selectionLength: Math.max(0, selEnd - selStart),
    });
  }

  function publishScrollState() {
    if (!editor) return;
    const topLine = Math.max(1, editor.getVisibleRanges()[0]?.startLineNumber ?? 1);
    postToHost({ type: 'editor/didScroll', topLine });
  }

  function waitForHostResponse(type, requestId, timeoutMs) {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        pendingRequests.delete(requestId);
        reject(new Error('host_timeout'));
      }, timeoutMs ?? 8000);
      pendingRequests.set(requestId, { type, resolve, reject, timer });
    });
  }

  function resolveHostResponse(msg) {
    const payload = msg.payload || msg;
    const requestId = payload.requestId;
    if (requestId == null) return;
    const pending = pendingRequests.get(requestId);
    if (!pending) return;
    clearTimeout(pending.timer);
    pendingRequests.delete(requestId);
    pending.resolve(payload);
  }

  let agentRevealTimer = null;

  function versionGuard(expectedVersion) {
    return expectedVersion == null || expectedVersion === modelVersion;
  }

  function applyDecorations(setId, decorations) {
    if (decorationLayerManager) {
      decorationLayerManager.apply(setId, decorations, editor, monaco, normalizeSetId);
      return;
    }
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;
    const normalizedId = normalizeSetId(setId);
    const maxOffset = model.getValueLength();
    const monacoDecos = (decorations || []).map((d) => {
      const start = Math.max(0, Math.min(d.startOffset ?? 0, maxOffset));
      const end = Math.max(start, Math.min(start + Math.max(0, d.length ?? 0), maxOffset));
      const className = d.className || '';
      const options = {
        hoverMessage: d.hoverMessage ? { value: d.hoverMessage } : undefined,
        glyphMarginClassName: d.glyphMarginClassName || undefined,
      };
      let range;
      if (d.isWholeLine) {
        options.isWholeLine = true;
        if (className) {
          if (className.startsWith('squiggly-')) {
            options.inlineClassName = className;
          } else {
            options.className = className;
          }
        }
        const line = model.getPositionAt(start).lineNumber;
        range = new monaco.Range(line, 1, line, model.getLineMaxColumn(line));
      } else if (className) {
        options.inlineClassName = className;
        range = monaco.Range.fromPositions(
          model.getPositionAt(start),
          model.getPositionAt(end)
        );
      } else {
        range = monaco.Range.fromPositions(
          model.getPositionAt(start),
          model.getPositionAt(end)
        );
      }
      return { range, options };
    });
    const handles = editor.deltaDecorations(decorationSets.get(normalizedId) || [], monacoDecos);
    decorationSets.set(normalizedId, handles);
  }

  function agentRevealClassForLine(line, startLine, endLine) {
    const base = 'cide-agent-reveal-line';
    if (startLine === endLine) return `${base} cide-agent-reveal-single`;
    if (line === startLine) return `${base} cide-agent-reveal-top`;
    if (line === endLine) return `${base} cide-agent-reveal-bottom`;
    return `${base} cide-agent-reveal-middle`;
  }

  function applyAgentReveal(payload) {
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;
    if (agentRevealTimer) {
      clearTimeout(agentRevealTimer);
      agentRevealTimer = null;
    }
    const startLine = Math.max(1, payload.startLine ?? 1);
    const endLine = Math.max(startLine, payload.endLine ?? startLine);
    const endLineClamped = Math.min(endLine, model.getLineCount());
    const startLineClamped = Math.min(startLine, endLineClamped);
    const decos = [];
    for (let line = startLineClamped; line <= endLineClamped; line++) {
      decos.push({
        range: new monaco.Range(line, 1, line, model.getLineMaxColumn(line)),
        options: {
          isWholeLine: true,
          className: agentRevealClassForLine(line, startLineClamped, endLineClamped),
        },
      });
    }
    const handles = editor.deltaDecorations(decorationSets.get(BUS.setIds.agentReveal) || [], decos);
    decorationSets.set(BUS.setIds.agentReveal, handles);
    if (!payload.persistent) {
      const durationMs = payload.durationMs != null && payload.durationMs > 0 ? payload.durationMs : 3000;
      agentRevealTimer = setTimeout(() => {
        if (!editor) return;
        const cleared = editor.deltaDecorations(decorationSets.get(BUS.setIds.agentReveal) || [], []);
        decorationSets.set(BUS.setIds.agentReveal, cleared);
        agentRevealTimer = null;
      }, durationMs);
    }
  }

  function clearAgentReveal() {
    if (agentRevealTimer) {
      clearTimeout(agentRevealTimer);
      agentRevealTimer = null;
    }
    if (!editor) return;
    const cleared = editor.deltaDecorations(decorationSets.get(BUS.setIds.agentReveal) || [], []);
    decorationSets.set(BUS.setIds.agentReveal, cleared);
  }

  function applyEpochDim(dimmed) {
    const container = document.getElementById('container');
    if (!container) return;
    container.classList.toggle('cide-epoch-dim', !!dimmed);
  }

  function applySelectionByOffset(payload) {
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;
    const start = Math.max(0, Math.min(payload.selectionStart ?? 0, model.getValueLength()));
    const len = Math.max(0, payload.selectionLength ?? 0);
    const end = Math.min(model.getValueLength(), start + len);
    const startPos = model.getPositionAt(start);
    const endPos = model.getPositionAt(end);
    const range = new monaco.Range(
      startPos.lineNumber,
      startPos.column,
      endPos.lineNumber,
      endPos.column
    );
    editor.setSelection(range);
    editor.revealRangeInCenter(range, monaco.editor.ScrollType.Smooth);
    publishModelState('selection');
  }

  function applyGutterGlyphs(glyphs) {
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;

    if (!cfGlyphStyleEl) {
      cfGlyphStyleEl = document.createElement('style');
      cfGlyphStyleEl.id = 'cide-cf-glyphs';
      document.head.appendChild(cfGlyphStyleEl);
    }

    let css = '';
    const monacoDecos = [];
    for (const g of glyphs || []) {
      const line = g.lineOneBased ?? g.line;
      if (!line) continue;
      const glyph = (g.textGlyph || '').replace(/\\/g, '\\\\').replace(/'/g, "\\'");
      const kind = g.visualKind || 'Circle';
      const cls = `cide-cf-glyph cide-cf-${kind} cide-cf-line-${line}`;
      css += `.monaco-editor .margin-view-overlays .${cls}::before { content: '${glyph}'; }\n`;
      monacoDecos.push({
        range: new monaco.Range(line, 1, line, 1),
        options: {
          glyphMarginClassName: cls,
          glyphMarginHoverMessage: g.toolTip ? { value: g.toolTip } : undefined,
        },
      });
    }
    cfGlyphStyleEl.textContent = css;
    const handles = editor.deltaDecorations(decorationSets.get(BUS.setIds.cfGutter) || [], monacoDecos);
    decorationSets.set(BUS.setIds.cfGutter, handles);
  }

  function applyCfContentLane(active, widthPx) {
    cfLaneActive = !!active;
    const dom = editor && editor.getDomNode ? editor.getDomNode() : null;
    if (!dom) return;
    dom.classList.toggle('cide-cf-lane-shift', cfLaneActive);
    dom.style.setProperty('--cide-cf-lane-width', cfLaneActive ? `${widthPx || 18}px` : '0px');
  }

  function applyHostInlayHints(hints) {
    hostInlayHints = hints || [];
    if (!editor) return;
    const action = editor.getAction('editor.action.inlayHints.refresh');
    if (action) {
      action.run();
    }
  }

  function applyHostSemanticLegend(legend) {
    if (legend && legend.tokenTypes && legend.tokenTypes.length > 0) {
      hostSemanticLegend = {
        tokenTypes: legend.tokenTypes,
        tokenModifiers: legend.tokenModifiers || [],
      };
    } else {
      hostSemanticLegend = null;
    }
    registerIntelligenceProviders();
    if (editor && editor.getModel()) {
      editor.getModel().forceTokenization(editor.getModel().getLineCount());
    }
  }

  function mapInlayKind(kind) {
    if (kind === 'parameter') return monaco.languages.InlayHintKind.Parameter;
    return monaco.languages.InlayHintKind.Type;
  }

  function mapHostInlayHint(model, h) {
    const line = h.line;
    const column = h.atEndOfLine ? model.getLineMaxColumn(line) : h.column;
    const hint = {
      position: { lineNumber: line, column },
      label: h.label,
      paddingLeft: !h.atEndOfLine,
    };
    switch (h.kind) {
      case 'diagnostic-error':
        hint.fontColor = '#f48771';
        break;
      case 'diagnostic-warning':
        hint.fontColor = '#cca700';
        break;
      case 'diagnostic-info':
        hint.fontColor = '#3794ff';
        break;
      default:
        hint.kind = mapInlayKind(h.kind);
        break;
    }
    return hint;
  }

  function registerIntelligenceProviders() {
    disposables.forEach((d) => d.dispose());
    disposables = [];

    if (!intelligenceEnabled) return;

    function requestCapability(type, position) {
      const requestId = (Date.now() % 2000000000) + Math.floor(Math.random() * 1000);
      postToHost({ type, requestId, line: position.lineNumber, column: position.column });
      return requestId;
    }

    function requestCapabilityAt(type, line, column) {
      const requestId = (Date.now() % 2000000000) + Math.floor(Math.random() * 1000);
      postToHost({ type, requestId, line, column });
      return requestId;
    }

    function mapCompletionKind(kind) {
      switch (kind) {
        case 'method':
        case 'function':
        case 'constructor':
          return monaco.languages.CompletionItemKind.Method;
        case 'property':
          return monaco.languages.CompletionItemKind.Property;
        case 'field':
        case 'variable':
        case 'enumMember':
        case 'constant':
          return monaco.languages.CompletionItemKind.Field;
        case 'event':
          return monaco.languages.CompletionItemKind.Event;
        case 'class':
          return monaco.languages.CompletionItemKind.Class;
        case 'interface':
          return monaco.languages.CompletionItemKind.Interface;
        case 'struct':
          return monaco.languages.CompletionItemKind.Struct;
        case 'enum':
          return monaco.languages.CompletionItemKind.Enum;
        case 'delegate':
          return monaco.languages.CompletionItemKind.Function;
        case 'keyword':
          return monaco.languages.CompletionItemKind.Keyword;
        default:
          return monaco.languages.CompletionItemKind.Text;
      }
    }

    disposables.push(monaco.languages.registerCompletionItemProvider('csharp', {
      triggerCharacters: ['.', '@'],
      provideCompletionItems: async (model, position) => {
        const requestId = requestCapability(BUS.capabilities.completion, position);
        try {
          const payload = await waitForHostResponse(BUS.capabilities.completionResult, requestId);
          const items = (payload.items || []).map((item) => ({
            label: item.label,
            kind: mapCompletionKind(item.kind),
            insertText: item.insertText ?? item.label,
            detail: item.detail,
          }));
          return { suggestions: items };
        } catch {
          return { suggestions: [] };
        }
      },
    }));

    disposables.push(monaco.languages.registerHoverProvider('csharp', {
      provideHover: async (model, position) => {
        const requestId = requestCapability(BUS.capabilities.hover, position);
        try {
          const payload = await waitForHostResponse(BUS.capabilities.hoverResult, requestId);
          if (!payload.markdown) return null;
          return { contents: [{ value: payload.markdown }] };
        } catch {
          return null;
        }
      },
    }));

    disposables.push(monaco.languages.registerSignatureHelpProvider('csharp', {
      signatureHelpTriggerCharacters: ['(', ','],
      provideSignatureHelp: async (model, position) => {
        const requestId = requestCapability(BUS.capabilities.signatureHelp, position);
        try {
          const payload = await waitForHostResponse(BUS.capabilities.signatureResult, requestId);
          if (!payload.signature) return null;
          return {
            value: {
              signatures: [{ label: payload.signature, parameters: [] }],
              activeSignature: 0,
              activeParameter: 0,
            },
            dispose: () => {},
          };
        } catch {
          return null;
        }
      },
    }));

    disposables.push(monaco.languages.registerDefinitionProvider('csharp', {
      provideDefinition: async (model, position) => {
        const requestId = requestCapability(BUS.capabilities.definition, position);
        try {
          const payload = await waitForHostResponse(BUS.capabilities.definitionResult, requestId);
          const loc = payload.location;
          if (!loc || !loc.filePath) return null;
          const uri = monaco.Uri.parse('file:///' + String(loc.filePath).replace(/\\/g, '/'));
          return {
            uri,
            range: new monaco.Range(loc.line ?? 1, loc.column ?? 1, loc.line ?? 1, loc.column ?? 1),
          };
        } catch {
          return null;
        }
      },
    }));

    disposables.push(monaco.languages.registerInlayHintsProvider('csharp', {
      provideInlayHints: async (model, range) => {
        if (hostInlayHints.length > 0) {
          return {
            hints: hostInlayHints
              .filter((h) => h.line >= range.startLineNumber && h.line <= range.endLineNumber)
              .map((h) => mapHostInlayHint(model, h)),
            dispose: () => {},
          };
        }
        const requestId = requestCapabilityAt(BUS.capabilities.inlayHints, range.startLineNumber, 1);
        try {
          const payload = await waitForHostResponse(BUS.capabilities.inlayHintsResult, requestId);
          return {
            hints: (payload.hints || []).map((h) => mapHostInlayHint(model, h)),
            dispose: () => {},
          };
        } catch {
          return { hints: [], dispose: () => {} };
        }
      },
    }));

    disposables.push(monaco.languages.registerCodeLensProvider('csharp', {
      provideCodeLenses: async () => {
        const requestId = requestCapabilityAt(BUS.capabilities.codeLens, 1, 1);
        try {
          const payload = await waitForHostResponse(BUS.capabilities.codeLensResult, requestId);
          const lenses = (payload.lenses || []).map((l) => ({
            range: new monaco.Range(l.line, l.column || 1, l.line, l.column || 1),
            id: l.id,
            command: {
              id: 'cide.executeCodeLens',
              title: l.title,
              arguments: [l.id],
            },
          }));
          return { lenses, dispose: () => {} };
        } catch {
          return { lenses: [], dispose: () => {} };
        }
      },
    }));

    registerCodeLensCommand();

    if (hostSemanticLegend && hostSemanticLegend.tokenTypes && hostSemanticLegend.tokenTypes.length > 0) {
      disposables.push(monaco.languages.registerDocumentSemanticTokensProvider('csharp', {
        getLegend: () => ({
          tokenTypes: hostSemanticLegend.tokenTypes,
          tokenModifiers: hostSemanticLegend.tokenModifiers || [],
        }),
        provideDocumentSemanticTokens: async () => {
          const requestId = requestCapabilityAt(BUS.capabilities.semanticTokens, 1, 1);
          try {
            const payload = await waitForHostResponse(BUS.capabilities.semanticTokensResult, requestId);
            const arr = payload.data || [];
            return {
              resultId: payload.resultId || undefined,
              data: Uint32Array.from(arr),
            };
          } catch {
            return { data: new Uint32Array(0) };
          }
        },
        releaseDocumentSemanticTokens: () => {},
      }));
    }
  }

  function registerCodeLensCommand() {
    disposables.push(monaco.editor.registerCommand('cide.executeCodeLens', (_ctx, lensId) => {
      if (!lensId) return;
      postToHost({ type: BUS.capabilities.codeLensClick, lensId: String(lensId) });
    }));
  }

  function registerBundledMonarchGrammars() {
    if (typeof window.cideRegisterMonarchGrammars === 'function') {
      window.cideRegisterMonarchGrammars(monaco);
    }
  }

  window.cideEditorHost = {
    dispatchFromHost(raw) {
      const msg = typeof raw === 'string' ? JSON.parse(raw) : raw;
      const payload = msg.payload || msg;
      switch (msg.type) {
        case 'editor/completionResult':
        case 'capability/completionResult':
        case 'editor/hoverResult':
        case 'capability/hoverResult':
        case 'editor/signatureResult':
        case 'capability/signatureResult':
        case 'capability/definitionResult':
        case 'capability/inlayHintsResult':
        case 'capability/codeLensResult':
        case 'capability/semanticTokensResult':
          resolveHostResponse(msg);
          break;
        case 'editor/setModel': {
          if (!editor) return;
          const model = editor.getModel();
          if (!model) return;
          suppressChange = true;
          try {
            modelVersion = payload.version ?? 0;
            if (payload.languageId && model.getLanguageId() !== payload.languageId) {
              monaco.editor.setModelLanguage(model, payload.languageId);
            }
            const current = model.getValue();
            if (current !== payload.text) {
              model.setValue(payload.text ?? '');
            }
          } finally {
            suppressChange = false;
          }
          publishModelState('selection');
          publishScrollState();
          break;
        }
        case 'editor/applyEdits': {
          if (!editor) return;
          const model = editor.getModel();
          if (!model) return;
          if (payload.expectedVersion != null && payload.expectedVersion !== modelVersion) {
            console.warn('applyEdits version mismatch', payload.expectedVersion, modelVersion);
            return;
          }
          suppressChange = true;
          try {
            const edits = (payload.edits || []).map((e) => ({
              range: monaco.Range.fromPositions(
                model.getPositionAt(e.startOffset),
                model.getPositionAt(e.startOffset + Math.max(0, e.length))
              ),
              text: e.text ?? '',
            }));
            if (edits.length > 0) {
              editor.executeEdits('cide-host', edits);
              modelVersion += 1;
            }
          } finally {
            suppressChange = false;
          }
          publishModelState('change');
          break;
        }
        case 'editor/setDecorations':
          if (!versionGuard(payload.expectedModelVersion)) break;
          applyDecorations(payload.setId || 'default', payload.decorations || []);
          break;
        case 'editor/setGutterGlyphs':
          applyGutterGlyphs(payload.glyphs || []);
          break;
        case 'editor/setCfContentLane':
          applyCfContentLane(payload.active, payload.widthPixels);
          break;
        case 'editor/setInlayHints':
          if (!versionGuard(payload.expectedModelVersion)) break;
          applyHostInlayHints(payload.hints || []);
          break;
        case 'editor/setSemanticTokensLegend':
          applyHostSemanticLegend(payload);
          break;
        case 'editor/setStickyScroll':
          window.cideEditorHost._stickyLabel = payload.label || null;
          break;
        case 'editor/setIntelligence':
          intelligenceEnabled = payload.enabled !== false;
          registerIntelligenceProviders();
          break;
        case 'editor/revealRange': {
          if (!editor) return;
          const model = editor.getModel();
          if (!model) return;
          const startLine = Math.max(1, payload.startLine ?? 1);
          const endLine = Math.max(startLine, payload.endLine ?? startLine);
          const endCol = model.getLineMaxColumn(Math.min(endLine, model.getLineCount()));
          const startCol = payload.column != null ? Math.max(1, payload.column) : 1;
          const range = new monaco.Range(
            startLine,
            Math.min(startCol, model.getLineMaxColumn(startLine)),
            Math.min(endLine, model.getLineCount()),
            endCol
          );
          if (payload.select !== false) {
            editor.setSelection(range);
            publishModelState('selection');
          }
          editor.revealRangeInCenter(range, monaco.editor.ScrollType.Smooth);
          break;
        }
        case 'editor/setSelectionByOffset':
          applySelectionByOffset(payload);
          break;
        case 'editor/setAgentReveal':
          applyAgentReveal(payload);
          break;
        case 'editor/setEpochDim':
          applyEpochDim(payload.dimmed);
          break;
        case 'editor/clearAgentReveal':
          clearAgentReveal();
          break;
        case 'editor/setTheme':
          if (payload && payload.defineTheme && payload.defineTheme.id && payload.defineTheme.data) {
            monaco.editor.defineTheme(payload.defineTheme.id, payload.defineTheme.data);
          }
          if (payload && payload.theme) {
            monaco.editor.setTheme(payload.theme);
          }
          break;
        default:
          break;
      }
    },
    _stickyLabel: null,
  };

  function bootEditor() {
    monaco.editor.defineTheme('cascade-dark', {
      base: 'vs-dark',
      inherit: true,
      rules: [],
      colors: {
        'editor.background': '#1e1e1e',
        'editor.foreground': '#d4d4d4',
        'editor.selectionBackground': '#264f78',
        'editor.selectionHighlightBackground': '#264f7855',
        'editor.wordHighlightBackground': '#575757b8',
        'editor.wordHighlightStrongBackground': '#004972b8',
      },
    });

    editor = monaco.editor.create(document.getElementById('container'), {
      value: '',
      language: 'plaintext',
      theme: 'cascade-dark',
      automaticLayout: true,
      fontSize: 14,
      fontFamily: 'Consolas, Cascadia Code, monospace',
      minimap: { enabled: true },
      scrollBeyondLastLine: false,
      renderWhitespace: 'selection',
      wordBasedSuggestions: 'off',
      glyphMargin: true,
      quickSuggestions: false,
      suggestOnTriggerCharacters: true,
    });

    editor.onDidChangeModelContent(() => {
      if (suppressChange) return;
      modelVersion += 1;
      publishModelState('change');
    });

    editor.onDidChangeCursorSelection(() => publishModelState('selection'));
    editor.onDidScrollChange(() => publishScrollState());

    editor.onMouseDown((e) => {
      const target = e.target;
      if (!target || !target.position) return;
      const t = target.type;
      if (t === monaco.editor.MouseTargetType.GUTTER_GLYPH_MARGIN
          || t === monaco.editor.MouseTargetType.GUTTER_LINE_NUMBERS) {
        postToHost({ type: 'editor/didGutterClick', line: target.position.lineNumber });
      }
    });

    registerBundledMonarchGrammars();
    registerIntelligenceProviders();
    postToHost({ type: 'editor/ready', version: modelVersion });
  }

  function loadMonaco() {
    if (window.require) {
      window.require.config({ paths: { vs: MONACO_VS } });
      window.require(['vs/editor/editor.main'], bootEditor);
      return;
    }
    const loader = document.createElement('script');
    loader.src = MONACO_VS + '/loader.js';
    loader.onload = () => {
      window.require.config({ paths: { vs: MONACO_VS } });
      window.require(['vs/editor/editor.main'], bootEditor);
    };
    loader.onerror = () => postToHost({ type: 'editor/ready', error: 'monaco_load_failed' });
    document.head.appendChild(loader);
  }

  loadMonaco();
})();
