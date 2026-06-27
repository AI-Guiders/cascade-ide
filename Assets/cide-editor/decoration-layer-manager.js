/* CECB DecorationLayerManager (ADR 0163 §2.6, monaco-presentation-projection-v1). */
(function (global) {
  'use strict';

  function createDecorationLayerManager() {
    const decorationSets = new Map();

    function applyWholeLineClass(className, options) {
      options.isWholeLine = true;
      if (!className) return;
      if (className.startsWith('squiggly-')) {
        options.inlineClassName = className;
      } else {
        options.className = className;
      }
    }

    function resolveRange(d, model, monaco) {
      if (d.startLine != null && d.startLine > 0) {
        const startLine = d.startLine;
        const endLine = d.endLine && d.endLine > 0 ? d.endLine : startLine;
        const startCol = d.startColumn && d.startColumn > 0 ? d.startColumn : 1;
        const endCol = d.endColumn && d.endColumn > 0
          ? d.endColumn
          : model.getLineMaxColumn(Math.min(endLine, model.getLineCount()));
        return new monaco.Range(
          startLine,
          startCol,
          Math.min(endLine, model.getLineCount()),
          endCol
        );
      }

      const maxOffset = model.getValueLength();
      const start = Math.max(0, Math.min(d.startOffset ?? 0, maxOffset));
      const end = Math.max(start, Math.min(start + Math.max(0, d.length ?? 0), maxOffset));
      return monaco.Range.fromPositions(model.getPositionAt(start), model.getPositionAt(end));
    }

    function toMonacoDecoration(d, model, monaco) {
      const className = d.className || '';
      const options = {
        hoverMessage: d.hoverMessage ? { value: d.hoverMessage } : undefined,
        glyphMarginClassName: d.glyphMarginClassName || undefined,
      };
      if (d.isWholeLine) {
        applyWholeLineClass(className, options);
      } else if (className) {
        options.inlineClassName = className;
      }
      return { range: resolveRange(d, model, monaco), options };
    }

    return {
      apply(setId, decorations, editor, monaco, normalizeSetId) {
        if (!editor) return;
        const model = editor.getModel();
        if (!model) return;
        const normalizedId = normalizeSetId(setId);
        const monacoDecos = (decorations || []).map((d) => toMonacoDecoration(d, model, monaco));
        const handles = editor.deltaDecorations(decorationSets.get(normalizedId) || [], monacoDecos);
        decorationSets.set(normalizedId, handles);
      },
    };
  }

  global.CideDecorationLayerManager = { create: createDecorationLayerManager };
})(typeof window !== 'undefined' ? window : globalThis);
