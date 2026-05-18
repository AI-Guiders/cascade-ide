#!/usr/bin/env python3
"""Форматирование intent-catalog.toml: command-first раскладка (читаемость; schema остаётся v1)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "IntentMelody" / "intent-catalog.toml"

HEADER = """# intent-catalog.toml — command-first каталог форм ввода (ADR 0109, 0119)
# Оверлей: IntentMelody/intent-catalog.toml рядом с exe (legacy: intent-melody-aliases.toml).
#
# Cookbook:
# - Якорь: command_id (канон IdeCommands / MCP).
# - Melody: поля melody_* на [[command]] (0–1 slug на команду); в палитре c:<slug>.
# - Slash: [[command.form.slash]] (0..N); intent-first пути /namespace action.
# - slash_group на [[command]] — группа autocomplete по умолчанию для всех slash ниже.
# - [command.form.slash.args] — статические args (page, surface); legacy mfd_page тоже читается.
# - enabled = false — отключить команду или отдельный slash без удаления из файла.
# - IML v2 (параметрические мелодии, tail_wire_class) — смысл языка, ADR 0109; не путать с intent_catalog_schema_version.
# - Параметрический slash: любая команда с melody_shape=parametric — хвост по wire_class (ADR 0124).
#   Редактор: /editor line select|delete <строки>. Портал: /portal open [url] (как c:wai).
# - Норматив: docs/intent-melody-language-v1.md
#
intent_catalog_schema_version = 1

# ------------------------------------------------------------------------------
# [[tail_wire_class]] — провода хвоста parametric melody (c:)
# ------------------------------------------------------------------------------
"""

TAIL_WIRE = """
[[tail_wire_class]]
id = "url_remainder"
kind = "single_remainder"

[[tail_wire_class]]
id = "int_chain_colon_space"
kind = "delimited_slots"
between_slots_any_of = [":", ";", " "]
"""

SECTION_ORDER = [
    ("Editor (parametric line)", lambda c: c["command_id"] in ("select", "apply_edit")),
    ("Intercom", lambda c: c["command_id"].startswith("chat_") or c["command_id"] in ("fork_chat_thread", "send_chat", "show_chat_page")),
    ("Build & quality", lambda c: "build" in c["command_id"] or c["command_id"] in ("run_code_cleanup",)),
    ("Tests", lambda c: "test" in c["command_id"]),
    ("Debug", lambda c: c["command_id"].startswith("debug_")),
    ("Git", lambda c: c["command_id"].startswith("git_")),
    ("Workspace & search", lambda c: c["command_id"] in ("get_ide_state", "search_workspace_text", "get_current_file_diagnostics", "focus_editor", "set_primary_work_surface", "open_solution_dialog", "toggle_workspace_splitters_lock")),
    ("Panels (MFD)", lambda c: c["command_id"] in ("set_mfd_shell_page", "show_environment_readiness_page", "show_hybrid_index_page", "show_web_ai_portal_page", "show_terminal_panel")),
    ("Help", lambda c: c["command_id"] == "" and any(s.get("kind") == "help" for s in c["slashes"])),
]


MELODY_KEY_MAP = {
    "melody_slug": "slug",
    "melody_shape": "shape",
    "melody_show_usage_hint_if_bare_slug": "show_usage_hint_if_bare_slug",
    "melody_tail_signature": "tail_signature",
    "melody_wire_class": "wire_class",
    "melody_chord_commit": "chord_commit",
    "melody_palette_hint_slug": "palette_hint_slug",
    "melody_palette_usage_hint": "palette_usage_hint",
    "melody_palette_usage_category": "palette_usage_category",
}


def _store_melody_field(melody: dict, key: str, val: str, raw: str) -> None:
    mapped = MELODY_KEY_MAP.get(key, key)
    if mapped == "show_usage_hint_if_bare_slug":
        melody[mapped] = raw == "true"
    else:
        melody[mapped] = val


def parse_blocks(text: str) -> list[dict]:
    blocks: list[dict] = []
    cur: dict | None = None
    mode: str | None = None

    def flush():
        nonlocal cur
        if cur and (cur.get("command_id") is not None or cur.get("melody") or cur.get("slashes")):
            blocks.append(cur)
        cur = None

    for line in text.splitlines():
        s = line.strip()
        if s == "[[command]]":
            flush()
            cur = {"command_id": "", "melody": {}, "slashes": [], "slash_group": None, "enabled": True}
            mode = "command"
            continue
        if cur is None:
            continue
        if s == "[command.melody]" or s == "[command.form.melody]":
            mode = "melody"
            continue
        if s == "[[command.slash]]" or s == "[[command.form.slash]]":
            cur["slashes"].append({})
            mode = "slash"
            continue
        if s == "[command.form.slash.args]":
            if cur["slashes"]:
                mode = "args"
            continue
        m = re.match(r'^([a-z_]+)\s*=\s*(.+)$', s)
        if not m:
            continue
        key, raw = m.group(1), m.group(2).strip()
        val = raw.strip('"') if raw.startswith('"') else raw
        if key == "enabled" and raw in ("true", "false"):
            val = raw == "true"
        if mode == "command":
            if key == "command_id":
                cur["command_id"] = val
            elif key == "slash_group":
                cur["slash_group"] = val
            elif key.startswith("melody_"):
                _store_melody_field(cur["melody"], key, val, raw)
        elif mode == "melody":
            _store_melody_field(cur["melody"], key, val, raw)
        elif mode == "slash" and cur["slashes"]:
            cur["slashes"][-1][key] = val
        elif mode == "args" and cur["slashes"]:
            if key in ("page", "surface", "mfd_page"):
                cur["slashes"][-1][key] = val

    flush()
    return blocks


def infer_slash_group(cmd_id: str, slashes: list[dict]) -> str | None:
    if cmd_id == "set_mfd_shell_page":
        return "Панели"
    groups = {s.get("group") for s in slashes if s.get("group")}
    if len(groups) == 1:
        return groups.pop()
    return None


def passport(cmd_id: str, melody: dict, slashes: list[dict]) -> str:
    parts = []
    slug = melody.get("slug")
    if slug:
        parts.append(f"c:{slug}")
    paths = [s.get("path", "") for s in slashes if s.get("path")]
    if paths:
        parts.append(", ".join(paths[:3]) + ("…" if len(paths) > 3 else ""))
    forms = " · ".join(parts) if parts else cmd_id or "/help"
    hint = slashes[0].get("help", "") if slashes else ""
    short = (hint[:60] + "…") if len(hint) > 60 else hint
    label = cmd_id or "local"
    return f"# {label} — {short or forms} ({forms})"


def emit_melody_flat(melody: dict) -> list[str]:
    if not melody.get("slug"):
        return []
    lines = []
    mapping = [
        ("melody_slug", "slug"),
        ("melody_shape", "shape"),
        ("melody_show_usage_hint_if_bare_slug", "show_usage_hint_if_bare_slug"),
        ("melody_tail_signature", "tail_signature"),
        ("melody_wire_class", "wire_class"),
        ("melody_chord_commit", "chord_commit"),
        ("melody_palette_hint_slug", "palette_hint_slug"),
        ("melody_palette_usage_hint", "palette_usage_hint"),
        ("melody_palette_usage_category", "palette_usage_category"),
    ]
    for out_key, in_key in mapping:
        if in_key not in melody:
            continue
        v = melody[in_key]
        if in_key == "show_usage_hint_if_bare_slug":
            lines.append(f"{out_key} = {str(v).lower()}")
        else:
            lines.append(f'{out_key} = "{v}"')
    return lines


def emit_slash(s: dict) -> list[str]:
    lines = ["", "[[command.form.slash]]"]
    if s.get("enabled") is False:
        lines.append("enabled = false")
    lines.append(f'path = "{s["path"]}"')
    lines.append(f'help = "{s["help"]}"')
    if s.get("group"):
        lines.append(f'group = "{s["group"]}"')
    if s.get("kind"):
        lines.append(f'kind = "{s["kind"]}"')
    page = s.get("mfd_page")
    surface = s.get("primary_surface")
    if page or surface:
        lines.append("")
        lines.append("[command.form.slash.args]")
        if page:
            lines.append(f'page = "{page}"')
        if surface:
            lines.append(f'surface = "{surface}"')
    return lines


def emit_command(block: dict) -> list[str]:
    cmd_id = block["command_id"]
    melody = block["melody"]
    slashes = block["slashes"]
    slash_group = block.get("slash_group") or infer_slash_group(cmd_id, slashes)

    lines = ["", passport(cmd_id, melody, slashes), "[[command]]"]
    if cmd_id:
        lines.append(f'command_id = "{cmd_id}"')
    if slash_group and not all(s.get("group") == slash_group for s in slashes if s.get("group")):
        lines.append(f'slash_group = "{slash_group}"')
    elif slash_group and len(slashes) > 1:
        lines.append(f'slash_group = "{slash_group}"')
    lines.extend(emit_melody_flat(melody))
    for s in slashes:
        if slash_group and not s.get("group"):
            s = {**s, "group": None}  # inherit in loader
        lines.extend(emit_slash(s))
    return lines


def section_for(block: dict) -> str:
    for title, pred in SECTION_ORDER:
        if pred(block):
            return title
    return "Other"


def main() -> None:
    text = ROOT.read_text(encoding="utf-8")
    blocks = parse_blocks(text)

    by_section: dict[str, list[dict]] = {}
    for b in blocks:
        sec = section_for(b)
        by_section.setdefault(sec, []).append(b)

    out = [HEADER.rstrip(), TAIL_WIRE.rstrip()]
    order_titles = [t for t, _ in SECTION_ORDER] + ["Other"]
    for title in order_titles:
        items = by_section.get(title)
        if not items:
            continue
        out.append("")
        out.append(f"# {'=' * 78}")
        out.append(f"# {title}")
        out.append(f"# {'=' * 78}")
        for block in sorted(items, key=lambda x: x["command_id"] or "zzz"):
            out.extend(emit_command(block))

    ROOT.write_text("\n".join(out).rstrip() + "\n", encoding="utf-8")
    print(f"Wrote v2 catalog: {ROOT} ({len(blocks)} commands)")


if __name__ == "__main__":
    main()
