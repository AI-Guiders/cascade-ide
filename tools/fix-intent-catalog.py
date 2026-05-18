#!/usr/bin/env python3
"""Fix intent-catalog.toml: restore melody_*, slash.args, append slashes at block end."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "IntentMelody" / "intent-catalog.toml"

MELODY: dict[str, list[str]] = {
    "apply_edit": [
        'melody_slug = "eld"',
        'melody_shape = "parametric"',
        'melody_tail_signature = "<start:ln>:<end:ln>"',
        'melody_wire_class = "int_chain_colon_space"',
        'melody_chord_commit = "enter"',
        'melody_palette_usage_hint = "c:eld:<start>:<end> или одна строка c:eld:<line> (в аккорде c:eld;<start>;<end> / c:eld;<line> — «;» без Shift) — удалить строки"',
        'melody_palette_usage_category = "Editor -> Line -> Delete"',
    ],
    "select": [
        'melody_slug = "els"',
        'melody_shape = "parametric"',
        'melody_tail_signature = "<start:ln>:<end:ln>"',
        'melody_wire_class = "int_chain_colon_space"',
        'melody_chord_commit = "enter"',
        'melody_palette_usage_hint = "c:els:<start>:<end> или одна строка c:els:<line> (в аккорде c:els;<start>;<end> / c:els;<line> — «;» без Shift) — выделить строки"',
        'melody_palette_usage_category = "Editor -> Line -> Select"',
    ],
    "chat_export_readable": ['melody_slug = "cex"', 'melody_shape = "simple"'],
    "chat_open_selected_thread": ['melody_slug = "ato"', 'melody_shape = "simple"'],
    "chat_select_next_message": ['melody_slug = "amn"', 'melody_shape = "simple"'],
    "chat_select_next_thread": ['melody_slug = "atn"', 'melody_shape = "simple"'],
    "chat_select_prev_message": ['melody_slug = "amp"', 'melody_shape = "simple"'],
    "chat_select_prev_thread": ['melody_slug = "atp"', 'melody_shape = "simple"'],
    "chat_show_thread_overview": ['melody_slug = "atb"', 'melody_shape = "simple"'],
    "chat_toggle_selected_thinking": ['melody_slug = "amt"', 'melody_shape = "simple"'],
    "chat_toggle_show_thinking_in_history": ['melody_slug = "amh"', 'melody_shape = "simple"'],
    "fork_chat_thread": ['melody_slug = "ctf"', 'melody_shape = "simple"'],
    "send_chat": ['melody_slug = "cs"', 'melody_shape = "simple"'],
    "show_chat_page": ['melody_slug = "cps"', 'melody_shape = "simple"'],
    "build_solution_ui": ['melody_slug = "br"', 'melody_shape = "simple"'],
    "build_structured": ['melody_slug = "bs"', 'melody_shape = "simple"'],
    "run_tests": ['melody_slug = "bt"', 'melody_shape = "simple"'],
    "debug_attach": ['melody_slug = "da"', 'melody_shape = "simple"'],
    "debug_continue": ['melody_slug = "dc"', 'melody_shape = "simple"'],
    "debug_launch": ['melody_slug = "dl"', 'melody_shape = "simple"'],
    "debug_step_into": ['melody_slug = "di"', 'melody_shape = "simple"'],
    "debug_step_out": ['melody_slug = "df"', 'melody_shape = "simple"'],
    "debug_step_over": ['melody_slug = "dn"', 'melody_shape = "simple"'],
    "debug_stop": ['melody_slug = "dx"', 'melody_shape = "simple"'],
    "git_commit": ['melody_slug = "gc"', 'melody_shape = "simple"'],
    "git_push": ['melody_slug = "gp"', 'melody_shape = "simple"'],
    "git_status": ['melody_slug = "gs"', 'melody_shape = "simple"'],
    "git_submodule": ['melody_slug = "gsu"', 'melody_shape = "simple"'],
    "open_solution_dialog": ['melody_slug = "so"', 'melody_shape = "simple"'],
    "toggle_workspace_splitters_lock": ['melody_slug = "tol"', 'melody_shape = "simple"'],
    "show_environment_readiness_page": ['melody_slug = "ers"', 'melody_shape = "simple"'],
    "show_hybrid_index_page": ['melody_slug = "his"', 'melody_shape = "simple"'],
    "show_terminal_panel": ['melody_slug = "ts"', 'melody_shape = "simple"'],
    "show_web_ai_portal_page": [
        'melody_slug = "wai"',
        'melody_shape = "parametric"',
        "melody_show_usage_hint_if_bare_slug = false",
        'melody_tail_signature = "<url:url>"',
        'melody_wire_class = "url_remainder"',
        'melody_chord_commit = "enter"',
        'melody_palette_hint_slug = "wai-url"',
        'melody_palette_usage_hint = "c:wai:<адрес> - веб-портал AI; адрес можно без схемы (или c:wai: для страницы по умолчанию)"',
        'melody_palette_usage_category = "Web AI Portal"',
    ],
}

ARGS_AFTER: dict[str, list[str]] = {
    "/intercom show": ["", "[command.form.slash.args]", 'surface = "intercom"'],
    "/editor show": ["", "[command.form.slash.args]", 'surface = "editor"'],
    "/output show": ["", "[command.form.slash.args]", 'page = "Build"'],
    "/tests show": ["", "[command.form.slash.args]", 'page = "Tests"'],
    "/debug show": ["", "[command.form.slash.args]", 'page = "DebugStack"'],
    "/repository show": ["", "[command.form.slash.args]", 'page = "Git"'],
    "/editor panel": ["", "[command.form.slash.args]", 'page = "Editor"'],
    "/terminal show": ["", "[command.form.slash.args]", 'page = "Terminal"'],
    "/problems show": ["", "[command.form.slash.args]", 'page = "Problems"'],
    "/events show": ["", "[command.form.slash.args]", 'page = "Events"'],
    "/workspace show": ["", "[command.form.slash.args]", 'page = "WorkspaceHealth"'],
    "/settings show": ["", "[command.form.slash.args]", 'page = "AiChatSettings"'],
}


def insert_melodies(lines: list[str]) -> list[str]:
    out: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        out.append(line)
        m = re.match(r'^command_id = "([^"]*)"', line.strip())
        if m:
            cmd = m.group(1)
            nxt = lines[i + 1].strip() if i + 1 < len(lines) else ""
            if cmd in MELODY and not nxt.startswith("melody_"):
                out.extend(MELODY[cmd])
        i += 1
    return out


def insert_args(lines: list[str]) -> list[str]:
    out: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        out.append(line)
        m = re.match(r'^path = "([^"]+)"', line.strip())
        if m:
            path = m.group(1)
            if path in ARGS_AFTER:
                j = i + 1
                chunk = []
                while j < len(lines):
                    s = lines[j].strip()
                    if s.startswith("group = ") or s.startswith("kind = "):
                        chunk.append(lines[j])
                        j += 1
                        continue
                    break
                out.extend(chunk)
                i = j - 1
                tail = "\n".join(out[-6:])
                if "[command.form.slash.args]" not in tail:
                    out.extend(ARGS_AFTER[path])
        i += 1
    return out


def strip_trailing_garbage(lines: list[str]) -> list[str]:
    while lines and lines[-1].strip().startswith("# =====") or (
        lines and lines[-1].strip() == "[[command]]" and "Other" in (lines[-3] if len(lines) > 3 else "")
    ):
        lines.pop()
    # remove duplicate empty help at end
    text = "\n".join(lines)
    marker = "# ==============================================================================\n# Other"
    if marker in text:
        text = text[: text.index(marker)].rstrip()
    return text.splitlines()


def main() -> None:
    lines = ROOT.read_text(encoding="utf-8").splitlines()
    lines = strip_trailing_garbage(lines)
    lines = insert_melodies(lines)
    lines = insert_args(lines)
    # fix git commit help TOML
    for i, ln in enumerate(lines):
        if 'path = "/git commit"' in ln:
            j = i + 1
            if j < len(lines) and lines[j].startswith("help = "):
                lines[j] = (
                    'help = "Коммит: хвост — сообщение (можно в кавычках: «feat: …»)."'
                )
    ROOT.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")
    print(f"Fixed {ROOT}")


if __name__ == "__main__":
    main()
