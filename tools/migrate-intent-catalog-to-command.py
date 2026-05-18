#!/usr/bin/env python3
"""One-shot: [[melody_root]]+[[slash_route]] -> [[command]] with nested forms."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "IntentMelody" / "intent-catalog.toml"


def parse_tables(text: str, name: str) -> list[dict[str, str]]:
    blocks: list[dict[str, str]] = []
    current: dict[str, str] | None = None
    header = re.compile(rf"^\[\[{re.escape(name)}\]\]\s*$")
    for line in text.splitlines():
        if header.match(line.strip()):
            if current:
                blocks.append(current)
            current = {}
            continue
        if current is None:
            continue
        m = re.match(r'^([a-z_]+)\s*=\s*"(.*)"\s*$', line.strip())
        if m:
            current[m.group(1)] = m.group(2)
        elif line.strip().startswith("#") or not line.strip():
            pass
        elif line.strip().startswith("["):
            if current:
                blocks.append(current)
            current = None
    if current:
        blocks.append(current)
    return blocks


def main() -> None:
    text = ROOT.read_text(encoding="utf-8")
    tail_section = text.split("# ------------------------------------------------------------------------------", 1)[0]
    tail_lines = []
    in_tail = False
    for line in text.splitlines():
        if "[[tail_wire_class]]" in line:
            in_tail = True
        if in_tail and line.startswith("[[melody_root]]"):
            break
        if in_tail:
            tail_lines.append(line)

    melodies = parse_tables(text, "melody_root")
    slashes = parse_tables(text, "slash_route")

    by_cmd: dict[str, dict] = {}
    for m in melodies:
        cid = m.get("command_id", "")
        if not cid:
            continue
        by_cmd.setdefault(cid, {"melody": m, "slashes": []})

    for s in slashes:
        cid = s.get("command_id", "")
        if s.get("kind") == "help":
            by_cmd.setdefault("__local_help__", {"melody": None, "slashes": []})
            by_cmd["__local_help__"]["slashes"].append(s)
        elif cid:
            by_cmd.setdefault(cid, {"melody": None, "slashes": []})
            by_cmd[cid]["slashes"].append(s)
        else:
            by_cmd.setdefault(cid or "__no_cmd__", {"melody": None, "slashes": []})
            by_cmd[cid or "__no_cmd__"]["slashes"].append(s)

    out: list[str] = [
        "# Единый intent-каталог: якорь command_id, формы melody (c:) и slash (/).",
        "# Оверлей: IntentMelody/intent-catalog.toml рядом с exe.",
        "# ADR 0109, 0119.",
        "",
        "intent_catalog_schema_version = 1",
        "",
    ]
    out.extend(tail_lines)
    out.append("")

    for cid in sorted(by_cmd.keys(), key=lambda x: (x == "__local_help__", x)):
        entry = by_cmd[cid]
        m = entry.get("melody")
        slashes = entry.get("slashes") or []
        if cid == "__local_help__":
            out.append("[[command]]")
            for s in slashes:
                out.extend(emit_slash(s))
            out.append("")
            continue

        out.append("[[command]]")
        out.append(f'command_id = "{cid}"')
        if m:
            out.append("")
            out.append("[command.melody]")
            for key in (
                "slug",
                "shape",
                "show_usage_hint_if_bare_slug",
                "tail_signature",
                "wire_class",
                "chord_commit",
                "palette_hint_slug",
                "palette_usage_hint",
                "palette_usage_category",
            ):
                if key in m:
                    val = m[key]
                    if key == "show_usage_hint_if_bare_slug":
                        out.append(f"{key} = {val}")
                    else:
                        out.append(f'{key} = "{val}"')
        for s in slashes:
            out.append("")
            out.extend(emit_slash(s))
        out.append("")

    ROOT.write_text("\n".join(out).rstrip() + "\n", encoding="utf-8")
    print(f"Wrote {ROOT} ({len(by_cmd)} commands)")


def emit_slash(s: dict[str, str]) -> list[str]:
    lines = ["[[command.slash]]"]
    for key in ("path", "help", "group", "mfd_page", "primary_surface", "kind"):
        if key in s:
            lines.append(f'{key} = "{s[key]}"')
    return lines


if __name__ == "__main__":
    main()
