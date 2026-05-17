#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Translate docs/adr/*.md to docs/en/adr/*.md (Russian -> English).

Uses deep-translator (Google). Run: pip install deep-translator && python tools/translate_adrs_to_en.py
Options: --only 0021,0100  |  --from 50  |  --force  |  --incomplete-only  |  --dry-run
"""

from __future__ import annotations

import argparse
import re
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "docs" / "adr"
DST = ROOT / "docs" / "en" / "adr"

META_FILES = ("README.md", "status-lifecycle.md")
SKIP = set()  # numbered ADRs only in main loop; META via --meta

# Do not translate: code paths, ADR ids in links, HTML anchors, fenced blocks
FENCE_RE = re.compile(r"^(```+|~~~+)")

LINK_RE = re.compile(r"(\[.*?\]\([^)]+\)|`[^`]+`|\*\*[^*]+\*\*)")
CYRILLIC_RE = re.compile(r"[А-Яа-яЁё]")
MAX_RETRIES = 4
RETRY_SLEEP = (1.0, 2.5, 5.0, 8.0)


def rewrite_links(text: str) -> str:
    """Adjust relative paths from docs/adr/ to docs/en/adr/."""
    text = text.replace("](../ui-ux/", "](../ui-ux/")
    text = text.replace("](ui-ux/", "](../ui-ux/")
    text = text.replace("](../design/", "](../../design/")
    text = text.replace("](../architecture/", "](../../architecture/")
    text = text.replace("](../architecture-policy.md", "](../../architecture-policy.md")
    text = text.replace("](../MCP-PROTOCOL.md", "](../../MCP-PROTOCOL.md")
    text = text.replace("](../LANGUAGE-SERVICES-PLAN.md", "](../../LANGUAGE-SERVICES-PLAN.md")
    text = text.replace("](../intent-melody-language-v1.md", "](../../intent-melody-language-v1.md")
    text = text.replace("](../git-and-submodules-v1.md", "](../../git-and-submodules-v1.md")
    text = text.replace("](../COMMERCIAL-NOTICE.md", "](../../COMMERCIAL-NOTICE.md")
    text = text.replace("](../en/", "](../")  # fix double en
    # Header status line
    text = re.sub(
        r"^\*\*Статус:\*\*",
        "**Status:**",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    text = re.sub(
        r"^\*\*Дата:\*\*",
        "**Date:**",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    text = re.sub(
        r"^\*\*Обновлено:\*\*",
        "**Updated:**",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    # Remove duplicate Summary (EN) if source already had one from partial work
    return text


def split_translate_blocks(text: str, translate_fn) -> str:
    """Translate prose; keep fenced code blocks unchanged."""
    lines = text.splitlines(keepends=True)
    out: list[str] = []
    buf: list[str] = []
    in_fence = False
    fence_mark = ""

    def flush():
        nonlocal buf
        if not buf:
            return
        chunk = "".join(buf)
        if chunk.strip():
            try:
                out.append(translate_fn(chunk))
            except Exception as e:
                print(f"  WARN translate chunk failed: {e}")
                out.append(chunk)
        else:
            out.append(chunk)
        buf = []

    for line in lines:
        m = FENCE_RE.match(line.strip())
        if m:
            flush()
            mark = m.group(1)
            if not in_fence:
                in_fence = True
                fence_mark = mark
                out.append(line)
            elif line.strip().startswith(fence_mark):
                in_fence = False
                fence_mark = ""
                out.append(line)
            else:
                out.append(line)
            continue
        if in_fence:
            out.append(line)
        else:
            buf.append(line)
    flush()
    return "".join(out)


def translate_file(src: Path, dst: Path, translator, *, force: bool) -> bool:
    if dst.exists() and not force:
        return False
    raw = src.read_text(encoding="utf-8")
    # Skip if already has English-only header and no Cyrillic (re-run safe)
    if dst.exists() and not force:
        existing = dst.read_text(encoding="utf-8")
        if "**Status:**" in existing[:500] and not re.search(r"[А-Яа-яЁё]", existing[:2000]):
            return False

    def translate_piece(piece: str) -> str:
        last_err: Exception | None = None
        for attempt, delay in enumerate(RETRY_SLEEP):
            try:
                return translator.translate(piece)
            except Exception as e:
                last_err = e
                print(f"  WARN translate retry {attempt + 1}/{MAX_RETRIES}: {e}", flush=True)
                time.sleep(delay)
        if last_err:
            raise last_err
        return piece

    def translate_fn(chunk: str) -> str:
        chunk = chunk.strip("\n")
        if not chunk.strip():
            return chunk
        if not CYRILLIC_RE.search(chunk):
            return chunk
        # Google limit ~5000 chars
        parts: list[str] = []
        remaining = chunk
        while remaining:
            piece = remaining[:4500]
            if len(remaining) > 4500:
                cut = piece.rfind("\n\n")
                if cut < 1000:
                    cut = piece.rfind("\n")
                if cut < 1000:
                    cut = 4500
                piece = remaining[:cut]
            parts.append(translate_piece(piece))
            remaining = remaining[len(piece) :]
            if remaining:
                time.sleep(0.2)
        return "\n".join(parts) if len(parts) > 1 else parts[0]

    translated = split_translate_blocks(raw, translate_fn)
    translated = rewrite_links(translated)
    # Drop redundant Summary (EN) duplicate section if machine translated doubled
    translated = re.sub(
        r"<a id=\"summary-en\"></a>\s*\n## Summary \(EN\)\s*\n.*?\n(?=## (?!Summary))",
        "",
        translated,
        count=1,
        flags=re.DOTALL,
    )
    dst.parent.mkdir(parents=True, exist_ok=True)
    note = (
        "<!-- English translation of "
        + src.as_posix().split("docs/", 1)[-1]
        + ". Canonical Russian: ../../adr/"
        + src.name
        + " -->\n\n"
    )
    dst.write_text(note + translated, encoding="utf-8")
    return True


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", nargs="*", help="ADR numbers or filename stems")
    parser.add_argument("--from", dest="from_num", type=int, default=0, help="Min ADR number")
    parser.add_argument("--force", action="store_true")
    parser.add_argument(
        "--incomplete-only",
        action="store_true",
        help="Re-translate only EN files that still contain Cyrillic",
    )
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--meta", action="store_true", help="Translate README.md and status-lifecycle.md")
    args = parser.parse_args()

    try:
        from deep_translator import GoogleTranslator
    except ImportError:
        raise SystemExit("Install: pip install deep-translator")

    translator = GoogleTranslator(source="ru", target="en")
    done = 0
    if args.meta:
        for name in META_FILES:
            path = SRC / name
            if not path.is_file():
                continue
            dst = DST / name
            if args.dry_run:
                print(f"would translate {name}")
                continue
            print(f"translate {name}...", flush=True)
            if translate_file(path, dst, translator, force=args.force):
                done += 1
            time.sleep(0.5)

    files = sorted(SRC.glob("*.md"))
    for path in files:
        if path.name in META_FILES:
            continue
        m = re.match(r"^(\d+)-", path.name)
        if not m:
            continue
        num = int(m.group(1))
        if num < args.from_num:
            continue
        if args.only:
            stems = {o.replace(".md", "") for o in args.only}
            if path.stem not in stems and str(num) not in args.only:
                continue
        dst = DST / path.name
        if args.incomplete_only:
            if not dst.is_file() or not CYRILLIC_RE.search(dst.read_text(encoding="utf-8")):
                continue
        if args.dry_run:
            print(f"would translate {path.name}")
            continue
        print(f"translate {path.name}...", flush=True)
        force = args.force or args.incomplete_only
        if translate_file(path, dst, translator, force=force):
            done += 1
        time.sleep(0.35)
    print(f"OK: {done} files written to {DST.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
