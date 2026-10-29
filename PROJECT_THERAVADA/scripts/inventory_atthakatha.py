#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Inventory VRI/CST Roman-Pāli Aṭṭhakathā XML files.

Scans:
    romn/*.att.xml

Writes:
    PROJECT_THERAVADA/02_ATTHAKATHA/ATTHAKATHA_INVENTORY.csv
    PROJECT_THERAVADA/02_ATTHAKATHA/ATTHAKATHA_INVENTORY.md

No third-party packages required.
"""

from __future__ import annotations

import csv
import hashlib
import unicodedata
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path


OUT_DIR = Path("PROJECT_THERAVADA/02_ATTHAKATHA")
CSV_OUT = OUT_DIR / "ATTHAKATHA_INVENTORY.csv"
MD_OUT = OUT_DIR / "ATTHAKATHA_INVENTORY.md"

STRUCTURAL_RENDS = {"book", "nikaya", "title", "chapter", "subhead", "subsubhead"}


def repo_root() -> Path:
    here = Path(__file__).resolve()
    for p in [here.parent, *here.parents, Path.cwd().resolve(), *Path.cwd().resolve().parents]:
        if (p / "romn").is_dir() and (p / "PROJECT_THERAVADA").is_dir():
            return p
    raise SystemExit("ERROR: repository root not found")


def local_name(tag: str) -> str:
    return tag.split("}", 1)[-1] if "}" in tag else tag


def norm(text: str) -> str:
    return " ".join(unicodedata.normalize("NFC", text.replace("\u00a0", " ")).split())


def flatten(elem: ET.Element) -> str:
    parts = []
    if elem.text:
        parts.append(elem.text)
    for child in elem:
        parts.append(flatten(child))
        if child.tail:
            parts.append(child.tail)
    return "".join(parts)


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def classify(filename: str) -> tuple[str, str]:
    """
    Return (group, preliminary_scope).

    Only obvious Vinaya/Sutta/Abhidhamma commentary families are marked CORE_CANDIDATE.
    e*.att.xml and any unknown families are deliberately REVIEW_REQUIRED.
    """
    name = filename.lower()

    if name.startswith("vin"):
        return "VINAYA", "CORE_CANDIDATE"
    if name.startswith("s01"):
        return "DN", "CORE_CANDIDATE"
    if name.startswith("s02"):
        return "MN", "CORE_CANDIDATE"
    if name.startswith("s03"):
        return "SN", "CORE_CANDIDATE"
    if name.startswith("s04"):
        return "AN", "CORE_CANDIDATE"
    if name.startswith("s05"):
        return "KN", "CORE_CANDIDATE"
    if name.startswith("abh"):
        return "ABHIDHAMMA", "CORE_CANDIDATE"
    if name.startswith("e"):
        return "EXTRA_E_SERIES", "REVIEW_REQUIRED"

    return "OTHER", "REVIEW_REQUIRED"


def inspect_xml(path: Path) -> dict[str, str]:
    try:
        tree = ET.parse(path)
    except Exception as exc:
        return {
            "xml_status": f"PARSE_ERROR: {exc}",
            "xml_paragraphs": "",
            "headings": "",
        }

    root = tree.getroot()
    p_count = 0
    headings = []

    for elem in root.iter():
        if local_name(elem.tag) != "p":
            continue
        p_count += 1
        rend = elem.attrib.get("rend", "").strip().lower()
        if rend in STRUCTURAL_RENDS and len(headings) < 12:
            text = norm(flatten(elem))
            if text:
                headings.append(f"{rend.upper()}: {text}")

    return {
        "xml_status": "OK",
        "xml_paragraphs": str(p_count),
        "headings": " | ".join(headings),
    }


def main() -> None:
    root = repo_root()
    romn = root / "romn"
    out_dir = root / OUT_DIR
    out_dir.mkdir(parents=True, exist_ok=True)

    files = sorted(romn.glob("*.att.xml"))
    if not files:
        raise SystemExit("ERROR: no romn/*.att.xml files found")

    rows = []
    for path in files:
        group, scope = classify(path.name)
        meta = inspect_xml(path)
        rows.append(
            {
                "filename": path.name,
                "group": group,
                "preliminary_scope": scope,
                "size_bytes": str(path.stat().st_size),
                "size_mb": f"{path.stat().st_size / (1024 * 1024):.3f}",
                "xml_status": meta["xml_status"],
                "xml_paragraphs": meta["xml_paragraphs"],
                "headings": meta["headings"],
                "sha256": sha256_file(path),
            }
        )

    fieldnames = list(rows[0].keys())
    with (root / CSV_OUT).open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fieldnames)
        w.writeheader()
        w.writerows(rows)

    groups = Counter(r["group"] for r in rows)
    scopes = Counter(r["preliminary_scope"] for r in rows)
    parse_errors = [r for r in rows if r["xml_status"] != "OK"]
    total_bytes = sum(int(r["size_bytes"]) for r in rows)

    md = [
        "# Aṭṭhakathā Inventory",
        "",
        "Automated inventory of `romn/*.att.xml` in the VRI/CST repository.",
        "",
        "## Summary",
        "",
        f"- Total `.att.xml` files: **{len(rows)}**",
        f"- Total size: **{total_bytes / (1024 * 1024):.2f} MB**",
        f"- CORE_CANDIDATE: **{scopes.get('CORE_CANDIDATE', 0)}**",
        f"- REVIEW_REQUIRED: **{scopes.get('REVIEW_REQUIRED', 0)}**",
        f"- XML parse errors: **{len(parse_errors)}**",
        "",
        "## Groups",
        "",
        "| Group | Files |",
        "|---|---:|",
    ]

    for group in sorted(groups):
        md.append(f"| {group} | {groups[group]} |")

    md.extend([
        "",
        "## Scope policy",
        "",
        "`CORE_CANDIDATE` is only a filename-based preliminary classification.",
        "It does **not** prove authorship, title, canonical relation, or inclusion in the final corpus.",
        "",
        "`EXTRA_E_SERIES` and `OTHER` are deliberately marked `REVIEW_REQUIRED` and must not be",
        "silently merged into the Aṭṭhakathā corpus before identification.",
        "",
        "## Full inventory",
        "",
        "| File | Group | Scope | MB | Paragraphs | First structural headings |",
        "|---|---|---|---:|---:|---|",
    ])

    for r in rows:
        headings = r["headings"].replace("|", " / ")
        md.append(
            f"| `{r['filename']}` | {r['group']} | {r['preliminary_scope']} | "
            f"{r['size_mb']} | {r['xml_paragraphs'] or '—'} | {headings or '—'} |"
        )

    (root / MD_OUT).write_text("\n".join(md) + "\n", encoding="utf-8", newline="\n")

    print(f"Found {len(rows)} Aṭṭhakathā XML files")
    print(f"CORE_CANDIDATE: {scopes.get('CORE_CANDIDATE', 0)}")
    print(f"REVIEW_REQUIRED: {scopes.get('REVIEW_REQUIRED', 0)}")
    print(f"Parse errors: {len(parse_errors)}")
    print(f"Wrote: {CSV_OUT}")
    print(f"Wrote: {MD_OUT}")


if __name__ == "__main__":
    main()
