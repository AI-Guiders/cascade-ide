#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Generate MkDocs ADR navigator pages grouped by lifecycle status.

Reads docs/adr/*.md (line 3 **Статус:**), writes docs/site/adr-nav/ (RU) and docs/en/site/adr-nav/ (EN).
Run before mkdocs build: python tools/gen_adr_pages.py
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADR_DIR = ROOT / "docs" / "adr"
ADR_EN_DIR = ROOT / "docs" / "en" / "adr"
OUT_RU = ROOT / "docs" / "site" / "adr-nav"
OUT_EN = ROOT / "docs" / "en" / "site" / "adr-nav"

SKIP_NAMES = {"README.md", "status-lifecycle.md"}

STATUS_RE = re.compile(
    r"^\*\*(?:Статус|Status):\*\*\s*(.+?)\s*$",
    re.IGNORECASE,
)
TITLE_RE = re.compile(r"^#\s+ADR\s+(\d+):\s*(.+)$", re.IGNORECASE)
TITLE_ALT_RE = re.compile(r"^#\s+(.+)$")

BUCKETS = [
    (
        "proposed",
        "Proposed",
        "Proposed",
        "Draft for discussion — not yet accepted.",
        "Черновик на обсуждение — решение ещё не принято.",
    ),
    (
        "accepted",
        "Accepted",
        "Accepted",
        "Accepted as norm; implementation not complete or intentionally phased.",
        "Принято как норма; внедрение в коде не завершено или намеренно растянуто.",
    ),
    (
        "accepted-in-progress",
        "Accepted · In progress",
        "Accepted · In progress",
        "Accepted; implementation in progress.",
        "Принято; реализация идёт (явная пометка In progress).",
    ),
    (
        "accepted-implemented",
        "Accepted · Implemented",
        "Accepted · Implemented",
        "Accepted and main delivery is in the codebase.",
        "Принято и основная поставка в коде выполнена.",
    ),
    (
        "superseded",
        "Superseded",
        "Superseded",
        "Replaced by another ADR — see link in the document.",
        "Заменено другим ADR — см. ссылку в тексте.",
    ),
    (
        "other",
        "Deferred / Deprecated / other",
        "Deferred / Deprecated / other",
        "Deferred, deprecated, or non-standard status wording.",
        "Отложено, устарело или нестандартная формулировка статуса.",
    ),
]


@dataclass(frozen=True)
class AdrRecord:
    num: int
    slug: str
    title: str
    status_raw: str
    bucket: str


def parse_title(lines: list[str], num: int, slug: str) -> str:
    for line in lines[:8]:
        m = TITLE_RE.match(line.strip())
        if m and int(m.group(1)) == num:
            return m.group(2).strip()
        m2 = TITLE_ALT_RE.match(line.strip())
        if m2 and line.startswith("# ADR"):
            return m2.group(1).strip()
    # fallback: slug
    part = slug.split("-", 1)[-1] if "-" in slug else slug
    return part.replace("-", " ").title()


def classify(status_raw: str) -> str:
    s = status_raw.strip()
    low = s.lower()
    if low.startswith("proposed"):
        return "proposed"
    if low.startswith("superseded"):
        return "superseded"
    if low.startswith("deprecated") or low.startswith("deferred"):
        return "other"
    if "superseded" in low:
        return "superseded"
    if low.startswith("accepted"):
        if "implemented" in low or "implemented" in s:
            return "accepted-implemented"
        if "in progress" in low:
            return "accepted-in-progress"
        return "accepted"
    return "other"


def load_records(*, lang: str = "ru") -> list[AdrRecord]:
    base = ADR_EN_DIR if lang == "en" else ADR_DIR
    records: list[AdrRecord] = []
    for path in sorted(ADR_DIR.glob("*.md")):
        if path.name in SKIP_NAMES:
            continue
        m = re.match(r"^(\d+)-(.+)\.md$", path.name)
        if not m:
            continue
        read_path = base / path.name if lang == "en" and (base / path.name).is_file() else path
        num = int(m.group(1))
        slug = path.name[:-3]
        text = read_path.read_text(encoding="utf-8")
        lines = text.splitlines()
        status_raw = ""
        for line in lines[:12]:
            sm = STATUS_RE.match(line.strip())
            if sm:
                status_raw = sm.group(1).strip()
                break
        if not status_raw:
            status_raw = "(no status line)"
        records.append(
            AdrRecord(
                num=num,
                slug=slug,
                title=parse_title(lines, num, slug),
                status_raw=status_raw,
                bucket=classify(status_raw),
            )
        )
    records.sort(key=lambda r: r.num)
    return records


def md_table(rows: list[AdrRecord], adr_prefix: str) -> str:
    if not rows:
        return "_No records._\n"
    lines = [
        "| ID | Title | Status (raw) |",
        "|----|-------|----------------|",
    ]
    for r in rows:
        link = f"[{r.num:04d}]({adr_prefix}{r.slug}.md)"
        title = r.title.replace("|", "\\|")
        raw = r.status_raw.replace("|", "\\|")
        lines.append(f"| {link} | {title} | {raw} |")
    return "\n".join(lines) + "\n"


def write_bucket(
    out_dir: Path,
    bucket_id: str,
    title_en: str,
    title_ru: str,
    blurb_en: str,
    blurb_ru: str,
    rows: list[AdrRecord],
    *,
    lang: str,
) -> None:
    adr_prefix = "../../adr/" if lang == "ru" else "../../adr/"
    nav_prefix = "./" if lang == "ru" else "./"
    if lang == "ru":
        title = title_ru
        page_blurb = blurb_ru
        back = "[← Навигатор ADR](index.md)"
        gen = "Сгенерировано `tools/gen_adr_pages.py`. Не редактировать вручную."
    else:
        title = title_en
        page_blurb = blurb_en
        back = "[← ADR navigator](index.md)"
        gen = "Generated by `tools/gen_adr_pages.py`. Do not edit by hand."

    body = md_table(rows, adr_prefix)
    content = f"""---
hide:
  - toc
---

# {title}

{page_blurb}

{back}

{body}

---

_{gen}_
"""
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / f"{bucket_id}.md").write_text(content, encoding="utf-8")


def write_index(out_dir: Path, records: list[AdrRecord], *, lang: str) -> None:
    counts: dict[str, int] = {b[0]: 0 for b in BUCKETS}
    for r in records:
        counts[r.bucket] = counts.get(r.bucket, 0) + 1

    if lang == "ru":
        h1 = "Навигатор ADR по статусам"
        intro = (
            "Сгруппированный индекс архитектурных решений по [жизненному циклу](../adr/status-lifecycle.md). "
            "Полный табличный индекс и тематические кластеры — в [README ADR](../adr/README.md)."
        )
        full = "[Полный индекс ADR](../adr/README.md)"
        life = "[Жизненный цикл статусов](../adr/status-lifecycle.md)"
    else:
        h1 = "ADR navigator by status"
        intro = (
            "Architecture decisions grouped by [lifecycle status](../../adr/status-lifecycle.md). "
            "Full index and topic clusters: [ADR README](../../adr/README.md)."
        )
        full = "[Full ADR index](../../adr/README.md)"
        life = "[Status lifecycle](../../adr/status-lifecycle.md)"

    lines = [f"# {h1}", "", intro, "", life + " · " + full, ""]
    for bid, title_en, title_ru, _be, _br in BUCKETS:
        label = title_ru if lang == "ru" else title_en
        n = counts.get(bid, 0)
        lines.append(f"- [{label}]({bid}.md) — **{n}**")
    lines.extend(["", "---", "", "_Generated by `tools/gen_adr_pages.py`._", ""])

    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "index.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    records_ru = load_records(lang="ru")
    records_en = load_records(lang="en")
    by_bucket_ru: dict[str, list[AdrRecord]] = {b[0]: [] for b in BUCKETS}
    by_bucket_en: dict[str, list[AdrRecord]] = {b[0]: [] for b in BUCKETS}
    for r in records_ru:
        by_bucket_ru.setdefault(r.bucket, []).append(r)
    for r in records_en:
        by_bucket_en.setdefault(r.bucket, []).append(r)

    for lang, out_dir, records, by_bucket in (
        ("ru", OUT_RU, records_ru, by_bucket_ru),
        ("en", OUT_EN, records_en, by_bucket_en),
    ):
        write_index(out_dir, records, lang=lang)
        for bid, title_en, title_ru, blurb_en, blurb_ru in BUCKETS:
            write_bucket(
                out_dir,
                bid,
                title_en,
                title_ru,
                blurb_en,
                blurb_ru,
                by_bucket.get(bid, []),
                lang=lang,
            )

    print(
        f"OK: {len(records_ru)} RU / {len(records_en)} EN ADRs -> "
        f"{OUT_RU.relative_to(ROOT)} + {OUT_EN.relative_to(ROOT)}"
    )
    for bid, *_ in BUCKETS:
        print(f"  {bid}: ru={len(by_bucket_ru.get(bid, []))} en={len(by_bucket_en.get(bid, []))}")


if __name__ == "__main__":
    main()
