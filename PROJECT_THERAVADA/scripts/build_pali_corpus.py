#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Build 61 CST/VRI Roman-Pāli source XML files into 13 consolidated UTF-8 TXT files.

Repository layout expected:
  romn/*.xml
  PROJECT_THERAVADA/00_INDEX/THERAVADA_CST_61_to_13_MAPPING.csv
  PROJECT_THERAVADA/01_PALI_CANON/
  PROJECT_THERAVADA/scripts/build_pali_corpus.py

No third-party Python packages are required.
"""

from __future__ import annotations

import csv
import hashlib
import os
import subprocess
import sys
import unicodedata
import xml.etree.ElementTree as ET
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path


EXPECTED_ROWS = 61
EXPECTED_OUTPUTS = 13
MAPPING_REL = Path("PROJECT_THERAVADA/00_INDEX/THERAVADA_CST_61_to_13_MAPPING.csv")
OUTPUT_REL = Path("PROJECT_THERAVADA/01_PALI_CANON")

REQUIRED_COLUMNS = {
    "№",
    "romn XML",
    "Текст / раздел",
    "Статус",
    "Итоговый источник",
    "Итоговый файл",
    "Примечание",
}

STRUCTURAL_REND = {
    "book": "BOOK",
    "nikaya": "NIKĀYA",
    "chapter": "CHAPTER",
    "title": "TITLE",
    "subhead": "SUBHEAD",
    "subsubhead": "SUBSUBHEAD",
}

APPROVED_NRF = {
    "romn/s0518m.nrf.xml",  # Milindapañha
    "romn/s0520m.nrf.xml",  # Peṭakopadesa
}


def repo_root() -> Path:
    """Locate repository root from this script or current working directory."""
    here = Path(__file__).resolve()
    for candidate in [here.parent, *here.parents, Path.cwd().resolve(), *Path.cwd().resolve().parents]:
        if (candidate / "romn").is_dir() and (candidate / "PROJECT_THERAVADA").is_dir():
            return candidate
    raise SystemExit("ERROR: Cannot locate repository root (romn/ and PROJECT_THERAVADA/ not found).")


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def git_sha(root: Path) -> str:
    env_sha = os.environ.get("GITHUB_SHA")
    if env_sha:
        return env_sha
    try:
        return subprocess.check_output(
            ["git", "rev-parse", "HEAD"],
            cwd=root,
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
    except Exception:
        return "UNKNOWN"


def local_name(tag: str) -> str:
    """Remove an XML namespace if one is present."""
    return tag.split("}", 1)[-1] if "}" in tag else tag


def normalize_inline(text: str) -> str:
    """NFC-normalize and collapse XML formatting whitespace without changing words."""
    text = unicodedata.normalize("NFC", text.replace("\u00a0", " "))
    return " ".join(text.split())


def flatten_element(elem: ET.Element) -> str:
    """
    Flatten an XML element while explicitly preserving page breaks and notes.
    Tail text is handled by the caller of each child.
    """
    parts = []
    if elem.text:
        parts.append(elem.text)

    for child in elem:
        tag = local_name(child.tag)

        if tag == "pb":
            ed = child.attrib.get("ed", "?")
            n = child.attrib.get("n", "?")
            parts.append(f" ⟦PB:{ed}:{n}⟧ ")
        elif tag == "note":
            note_text = flatten_element(child)
            note_text = normalize_inline(note_text)
            parts.append(f" ⟦NOTE:{note_text}⟧ " if note_text else " ⟦NOTE⟧ ")
        else:
            parts.append(flatten_element(child))

        if child.tail:
            parts.append(child.tail)

    return "".join(parts)


def xml_to_research_text(xml_path: Path) -> tuple[str, int]:
    """
    Convert VRI/CST TEI-like XML into research TXT.
    XML encoding declaration (including UTF-16) is honored by ElementTree.
    """
    try:
        tree = ET.parse(xml_path)
    except ET.ParseError as e:
        raise RuntimeError(f"XML parse error in {xml_path}: {e}") from e

    root = tree.getroot()
    lines = []
    p_count = 0

    for elem in root.iter():
        if local_name(elem.tag) != "p":
            continue

        p_count += 1
        rend = elem.attrib.get("rend", "").strip().lower()
        n = elem.attrib.get("n", "").strip()
        text = normalize_inline(flatten_element(elem))

        if not text:
            continue

        if rend in STRUCTURAL_REND:
            lines.append("")
            lines.append(f"⟦{STRUCTURAL_REND[rend]}⟧ {text}")
            lines.append("")
        elif rend == "hangnum" and n and text == n:
            lines.append(f"{text}.")
        else:
            lines.append(text)

    if p_count == 0:
        raise RuntimeError(f"No <p> elements found in {xml_path}")

    # Collapse excessive blank lines, preserving structural separation.
    clean = []
    blank = False
    for line in lines:
        if line == "":
            if not blank:
                clean.append("")
            blank = True
        else:
            clean.append(line)
            blank = False

    return "\n".join(clean).strip() + "\n", p_count


def load_mapping(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        columns = set(reader.fieldnames or [])
        missing = REQUIRED_COLUMNS - columns
        if missing:
            raise SystemExit(f"ERROR: Mapping CSV missing columns: {sorted(missing)}")
        rows = list(reader)

    if len(rows) != EXPECTED_ROWS:
        raise SystemExit(f"ERROR: Expected {EXPECTED_ROWS} mapping rows, found {len(rows)}")

    def row_number(row):
        try:
            return int(row["№"])
        except Exception:
            return 10**9

    rows.sort(key=row_number)
    return rows


def validate_mapping(root: Path, rows: list[dict[str, str]]) -> None:
    sources = [r["romn XML"].strip() for r in rows]
    outputs = [r["Итоговый файл"].strip() for r in rows]

    if len(set(sources)) != len(sources):
        dup = sorted({s for s in sources if sources.count(s) > 1})
        raise SystemExit(f"ERROR: Duplicate source mappings: {dup}")

    if len(set(outputs)) != EXPECTED_OUTPUTS:
        raise SystemExit(
            f"ERROR: Expected {EXPECTED_OUTPUTS} distinct output files, found {len(set(outputs))}"
        )

    for src in sources:
        if not src.startswith("romn/"):
            raise SystemExit(f"ERROR: Non-romn working source in mapping: {src}")
        p = root / src
        if not p.is_file():
            raise SystemExit(f"ERROR: Missing source file: {src}")

        if src.endswith(".nrf.xml"):
            if src not in APPROVED_NRF:
                raise SystemExit(f"ERROR: Unexpected NRF source: {src}")
        elif not src.endswith(".mul.xml"):
            raise SystemExit(f"ERROR: Unexpected textual-layer suffix: {src}")


def provenance_header(row: dict[str, str], source_hash: str, p_count: int) -> str:
    note = row["Примечание"].strip() or "—"
    return (
        "\n"
        + "=" * 78 + "\n"
        + "BEGIN_SOURCE\n"
        + f"SOURCE_FILE: {row['romn XML'].strip()}\n"
        + f"WORK: {row['Текст / раздел'].strip()}\n"
        + f"LAYER_STATUS: {row['Статус'].strip()}\n"
        + "EDITION: Chaṭṭha Saṅgāyana Tipiṭaka (CST)\n"
        + "PROVIDER: Vipassana Research Institute / Tipitaka.org\n"
        + "SCRIPT: Roman Pāli\n"
        + f"SOURCE_SHA256: {source_hash}\n"
        + f"XML_PARAGRAPHS: {p_count}\n"
        + f"NOTE: {note}\n"
        + "END_METADATA\n"
        + "=" * 78 + "\n\n"
    )


def build() -> int:
    root = repo_root()
    mapping_path = root / MAPPING_REL
    output_dir = root / OUTPUT_REL
    output_dir.mkdir(parents=True, exist_ok=True)

    if not mapping_path.is_file():
        raise SystemExit(f"ERROR: Mapping file not found: {mapping_path}")

    rows = load_mapping(mapping_path)
    validate_mapping(root, rows)

    grouped: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        grouped[row["Итоговый файл"].strip()].append(row)

    build_time = datetime.now(timezone.utc).isoformat()
    commit = git_sha(root)

    source_manifest = []
    output_manifest = []

    print(f"Repository: {root}")
    print(f"Mapping rows: {len(rows)}")
    print(f"Output groups: {len(grouped)}")
    print(f"Git SHA: {commit}")
    print()

    for output_name in sorted(grouped):
        output_path = output_dir / output_name
        chunks = [
            "THERAVĀDA RESEARCH CORPUS\n",
            f"FILE: {output_name}\n",
            "EDITION: Chaṭṭha Saṅgāyana Tipiṭaka (CST)\n",
            "WORKING SOURCE: VRI Roman-Pāli XML (romn/)\n",
            "IMPORTANT: Metadata markers are editorial; canonical text follows each source block.\n",
            "\n",
        ]

        print(f"Building {output_name} ...")
        for row in grouped[output_name]:
            src_rel = row["romn XML"].strip()
            src = root / src_rel
            src_hash = sha256_file(src)

            text, p_count = xml_to_research_text(src)
            if len(text.strip()) < 50:
                raise SystemExit(f"ERROR: Suspiciously short extracted text: {src_rel}")

            chunks.append(provenance_header(row, src_hash, p_count))
            chunks.append(text)
            chunks.append("\nEND_SOURCE\n")

            source_manifest.append(
                {
                    "source": src_rel,
                    "work": row["Текст / раздел"].strip(),
                    "status": row["Статус"].strip(),
                    "output": output_name,
                    "xml_paragraphs": str(p_count),
                    "source_bytes": str(src.stat().st_size),
                    "source_sha256": src_hash,
                }
            )

        final_text = unicodedata.normalize("NFC", "".join(chunks))
        output_path.write_text(final_text, encoding="utf-8", newline="\n")

        out_hash = sha256_file(output_path)
        output_manifest.append(
            {
                "output": output_name,
                "source_blocks": str(len(grouped[output_name])),
                "output_bytes": str(output_path.stat().st_size),
                "output_sha256": out_hash,
            }
        )
        print(
            f"  OK: {len(grouped[output_name])} source blocks, "
            f"{output_path.stat().st_size:,} bytes"
        )

    # Corpus-level Unicode sanity check.
    sample_chars = set("āīūṅñṭḍṇḷṃ")
    corpus_text = "".join((output_dir / x["output"]).read_text(encoding="utf-8")
                          for x in output_manifest)
    present = sorted(ch for ch in sample_chars if ch in corpus_text)
    missing_chars = sorted(sample_chars - set(present))
    if missing_chars:
        raise SystemExit(
            "ERROR: Expected Roman-Pāli diacritics not found in built corpus: "
            + " ".join(missing_chars)
        )

    # Write machine-readable source manifest.
    source_manifest_path = output_dir / "MANIFEST_SOURCES.csv"
    with source_manifest_path.open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(source_manifest[0].keys()))
        writer.writeheader()
        writer.writerows(source_manifest)

    output_manifest_path = output_dir / "MANIFEST_OUTPUTS.csv"
    with output_manifest_path.open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(output_manifest[0].keys()))
        writer.writeheader()
        writer.writerows(output_manifest)

    # Human-readable build report.
    report = [
        "# Pāli Canon Build Report",
        "",
        f"- Build UTC: `{build_time}`",
        f"- Git SHA: `{commit}`",
        f"- Mapping rows: **{len(rows)}**",
        f"- Unique source XML files: **{len({r['romn XML'].strip() for r in rows})}**",
        f"- Output TXT files: **{len(output_manifest)}**",
        f"- Roman-Pāli diacritics check: **PASS** ({' '.join(present)})",
        "",
        "## Output files",
        "",
        "| File | Source blocks | Bytes | SHA-256 |",
        "|---|---:|---:|---|",
    ]
    for item in output_manifest:
        report.append(
            f"| `{item['output']}` | {item['source_blocks']} | "
            f"{int(item['output_bytes']):,} | `{item['output_sha256']}` |"
        )

    report.extend([
        "",
        "## Validation",
        "",
        f"- Expected mapping rows: {EXPECTED_ROWS}",
        f"- Actual mapping rows: {len(rows)}",
        f"- Expected output files: {EXPECTED_OUTPUTS}",
        f"- Actual output files: {len(output_manifest)}",
        "- Duplicate source assignments: 0",
        "- Missing mapped source files: 0",
        "- Unexpected textual-layer suffixes: 0",
        "",
        "### Canonical-status note",
        "",
       "`s0518m.nrf.xml` (Milindapañha) and `s0520m.nrf.xml` "
"(Peṭakopadesa) are deliberately retained as separately flagged NRF texts. "
"Their canonical status varies across Theravāda traditions.",
"",
"`s0519m.mul.xml` (Nettippakaraṇa) is classified as MŪLA in the VRI/CST "
"repository layer, while its canonical status varies across Theravāda "
"regional traditions.",
        "",
    ])

    (output_dir / "BUILD_REPORT.md").write_text(
        "\n".join(report) + "\n", encoding="utf-8", newline="\n"
    )

    if len(source_manifest) != EXPECTED_ROWS:
        raise SystemExit("ERROR: Built source count does not equal 61")
    if len(output_manifest) != EXPECTED_OUTPUTS:
        raise SystemExit("ERROR: Built output count does not equal 13")

    print()
    print("SUCCESS")
    print(f"  61/61 mapped sources built")
    print(f"  13/13 output files created")
    print(f"  Reports: {OUTPUT_REL}/BUILD_REPORT.md")
    print(f"  Manifests: {OUTPUT_REL}/MANIFEST_SOURCES.csv, MANIFEST_OUTPUTS.csv")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(build())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise
