#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Build a complete inventory of Roman-Pāli XML files in romn/ and a separate
post-canonical inventory for the Theravāda Research Corpus.

Important:
- The 61 already-mapped Pāli Canon inputs are read from
  PROJECT_THERAVADA/00_INDEX/THERAVADA_CST_61_to_13_MAPPING.csv
  and excluded from the post-canonical candidate list.
- Repository suffixes (.mul/.att/.tik/.nrf) are recorded as repository
  metadata, not treated as final scholarly classification.
- e*.xml and unusual NRF families are deliberately routed to REVIEW_REQUIRED.
- No XML source file is modified.

Outputs:
PROJECT_THERAVADA/00_INDEX/POSTCANONICAL_INVENTORY/
    FULL_ROMN_INVENTORY.csv
    FULL_POSTCANONICAL_INVENTORY.csv
    POSTCANONICAL_REVIEW_QUEUE.csv
    FULL_POSTCANONICAL_INVENTORY.md
    INVENTORY_MANIFEST.json
"""

from __future__ import annotations

import csv
import hashlib
import json
import re
import subprocess
import unicodedata
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from pathlib import Path

MAPPING = Path(
    "PROJECT_THERAVADA/00_INDEX/THERAVADA_CST_61_to_13_MAPPING.csv"
)
OUT_DIR = Path(
    "PROJECT_THERAVADA/00_INDEX/POSTCANONICAL_INVENTORY"
)

STRUCTURAL_RENDS = {
    "nikaya", "book", "title", "chapter", "subhead", "subsubhead",
    "gatha1", "gatha2", "gatha3"
}

# These keyword hints identify obvious works/relationships, but never override
# the need for manual bibliographic review.
KEYWORD_HINTS = [
    ("visuddhimagga-mahāṭīkā", "VISUDDHIMAGGA_MAHATIKA"),
    ("visuddhimagga mahāṭīkā", "VISUDDHIMAGGA_MAHATIKA"),
    ("paramatthamañjūsā", "PARAMATTHAMANJUSA"),
    ("visuddhimagga", "VISUDDHIMAGGA_RELATED"),
    ("vimuttimagga", "VIMUTTIMAGGA_RELATED"),
    ("abhidhammatthasaṅgaha", "ABHIDHAMMATTHASANGAHA"),
    ("abhidhammatthasangaha", "ABHIDHAMMATTHASANGAHA"),
    ("samantapāsādikā", "SAMANTAPASADIKA"),
    ("kaṅkhāvitaraṇī", "KANKHAVITARANI"),
    ("sumaṅgalavilāsinī", "SUMANGALAVILASINI"),
    ("papañcasūdanī", "PAPANCASUDANI"),
    ("sāratthappakāsinī", "SARATTHAPPAKASINI"),
    ("manorathapūraṇī", "MANORATHAPURANI"),
    ("aṭṭhasālinī", "ATTHASALINI"),
    ("sammohavinodanī", "SAMMOHAVINODANI"),
    ("pañcappakaraṇa", "PANCAPPAKARANA_ATTHAKATHA"),
]


def repo_root() -> Path:
    here = Path(__file__).resolve()
    candidates = [here.parent, *here.parents, Path.cwd().resolve(), *Path.cwd().resolve().parents]
    for p in candidates:
        if (p / "romn").is_dir() and (p / "PROJECT_THERAVADA").is_dir():
            return p
    raise SystemExit("ERROR: repository root not found")


def git_value(root: Path, *args: str) -> str:
    try:
        return subprocess.check_output(
            ["git", "-C", str(root), *args],
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
    except Exception:
        return ""


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def normalize_text(text: str) -> str:
    return " ".join(
        unicodedata.normalize("NFC", text.replace("\u00a0", " ")).split()
    )


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1] if "}" in tag else tag


def parse_repo_suffix(filename: str) -> str:
    m = re.search(r"\.(mul|att|tik|nrf)\.xml$", filename, flags=re.I)
    return m.group(1).upper() if m else "OTHER"


def filename_family(filename: str) -> str:
    n = filename.lower()
    if n.startswith("vin"):
        return "VINAYA"
    if n.startswith("s01"):
        return "DN"
    if n.startswith("s02"):
        return "MN"
    if n.startswith("s03"):
        return "SN"
    if n.startswith("s04"):
        return "AN"
    if n.startswith("s05"):
        return "KN"
    if n.startswith("abh"):
        return "ABHIDHAMMA"
    if n.startswith("e"):
        return "EXTRA_E_SERIES"
    return "OTHER"


def filename_role_marker(filename: str) -> str:
    # Example: abh04t.nrf.xml -> t ; e0105n.nrf.xml -> n
    m = re.search(r"([a-z])\.(?:mul|att|tik|nrf)\.xml$", filename, flags=re.I)
    return m.group(1).lower() if m else ""


def read_canonical_mapping(root: Path) -> tuple[set[str], int]:
    path = root / MAPPING
    if not path.is_file():
        raise SystemExit(f"ERROR: mapping not found: {MAPPING}")

    canonical = set()
    with path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        if "romn XML" not in (reader.fieldnames or []):
            raise SystemExit("ERROR: mapping CSV has no 'romn XML' column")
        rows = list(reader)

    for row in rows:
        src = (row.get("romn XML") or "").strip().replace("\\", "/")
        if src:
            canonical.add(src)

    if len(rows) != 61:
        raise SystemExit(f"ERROR: expected 61 mapping rows, got {len(rows)}")
    if len(canonical) != 61:
        raise SystemExit(
            f"ERROR: expected 61 unique mapped XML paths, got {len(canonical)}"
        )
    return canonical, len(rows)


def inspect_xml(path: Path) -> dict[str, str | int]:
    """
    Streaming inspection to avoid holding large XML trees in memory.
    Captures paragraph count, structural headings, and a short opening sample.
    """
    p_count = 0
    headings: list[str] = []
    opening: list[str] = []

    try:
        for event, elem in ET.iterparse(path, events=("end",)):
            if local_name(elem.tag) == "p":
                p_count += 1
                text = normalize_text("".join(elem.itertext()))
                if text and len(opening) < 18:
                    opening.append(text[:500])

                rend = (elem.attrib.get("rend") or "").strip().lower()
                if rend in STRUCTURAL_RENDS and text and len(headings) < 20:
                    headings.append(f"{rend.upper()}: {text[:500]}")
                elem.clear()
    except Exception as exc:
        return {
            "xml_status": f"PARSE_ERROR: {exc}",
            "xml_paragraphs": 0,
            "structural_headings": "",
            "opening_sample": "",
            "keyword_hints": "",
        }

    combined = normalize_text(" ".join(headings + opening)).lower()
    hints = []
    for needle, label in KEYWORD_HINTS:
        if needle.lower() in combined:
            hints.append(label)

    return {
        "xml_status": "OK",
        "xml_paragraphs": p_count,
        "structural_headings": " | ".join(headings),
        "opening_sample": " || ".join(opening[:8]),
        "keyword_hints": ";".join(dict.fromkeys(hints)),
    }


def preliminary_layer(
    rel_path: str,
    filename: str,
    suffix: str,
    family: str,
    role_marker: str,
    keyword_hints: str,
    canonical_paths: set[str],
) -> tuple[str, str, str]:
    """
    Returns:
        (preliminary_layer, review_status, rationale)

    This is intentionally conservative. Repository suffix != scholarly layer.
    """
    if rel_path in canonical_paths:
        return (
            "CANONICAL_ALREADY_MAPPED",
            "EXCLUDE_FROM_POSTCANONICAL",
            "Present in validated 61→13 canonical mapping.",
        )

    hints = set(filter(None, keyword_hints.split(";")))

    if "VISUDDHIMAGGA_MAHATIKA" in hints or "PARAMATTHAMANJUSA" in hints:
        return (
            "TIKA_CLASSICAL_SYSTEMATIC",
            "HIGH_PRIORITY_REVIEW",
            "Opening metadata indicates Visuddhimagga-mahāṭīkā / Paramatthamañjūsā.",
        )

    if "VISUDDHIMAGGA_RELATED" in hints and suffix == "MUL":
        return (
            "CLASSICAL_SYSTEMATIC",
            "HIGH_PRIORITY_REVIEW",
            "Opening metadata indicates Visuddhimagga-related root/systematic text.",
        )

    if "VIMUTTIMAGGA_RELATED" in hints:
        return (
            "CLASSICAL_SYSTEMATIC",
            "HIGH_PRIORITY_REVIEW",
            "Opening metadata indicates Vimuttimagga-related text.",
        )

    if "ABHIDHAMMATTHASANGAHA" in hints:
        return (
            "LATER_ABHIDHAMMA_MANUAL",
            "HIGH_PRIORITY_REVIEW",
            "Opening metadata indicates Abhidhammatthasaṅgaha.",
        )

    # Anything in e-series requires work-level identification regardless of suffix.
    if family == "EXTRA_E_SERIES":
        return (
            f"EXTRA_SERIES_{suffix}",
            "REVIEW_REQUIRED",
            "e-series file; repository suffix is not sufficient for scholarly classification.",
        )

    if suffix == "ATT":
        return (
            "ATTHAKATHA_CANDIDATE",
            "CORE_CANDIDATE",
            "Non-e-series .att.xml; verify exact title/authorship/work relation.",
        )

    if suffix == "TIK":
        return (
            "TIKA_CANDIDATE",
            "CORE_CANDIDATE",
            "Repository .tik.xml; verify exact title and commentary relation.",
        )

    if suffix == "NRF" and role_marker == "t":
        return (
            "TIKA_RELATED_NRF_CANDIDATE",
            "REVIEW_REQUIRED",
            "NRF file with filename role marker 't'; may be Ṭīkā-related but requires identification.",
        )

    if suffix == "NRF":
        return (
            "NRF_POSTCANONICAL_CANDIDATE",
            "REVIEW_REQUIRED",
            "Repository NRF file; exact literary category must be identified.",
        )

    if suffix == "MUL":
        return (
            "UNMAPPED_MULA_CANDIDATE",
            "REVIEW_REQUIRED",
            "MŪLA-suffixed file not present in validated 61 canonical mapping.",
        )

    return (
        "UNCLASSIFIED",
        "REVIEW_REQUIRED",
        "Unrecognized repository filename/suffix pattern.",
    )


def write_csv(path: Path, rows: list[dict], fields: list[str]) -> None:
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader()
        w.writerows(rows)


def main() -> None:
    root = repo_root()
    romn = root / "romn"
    out_dir = root / OUT_DIR
    out_dir.mkdir(parents=True, exist_ok=True)

    canonical_paths, mapping_rows = read_canonical_mapping(root)
    xml_files = sorted(romn.glob("*.xml"))
    if not xml_files:
        raise SystemExit("ERROR: no romn/*.xml files found")

    fields = [
        "relative_path",
        "filename",
        "family",
        "repository_suffix",
        "filename_role_marker",
        "canonical_mapping_status",
        "preliminary_layer",
        "review_status",
        "rationale",
        "size_bytes",
        "size_mb",
        "xml_status",
        "xml_paragraphs",
        "keyword_hints",
        "structural_headings",
        "opening_sample",
        "sha256",
    ]

    rows: list[dict] = []
    for path in xml_files:
        rel_path = path.relative_to(root).as_posix()
        suffix = parse_repo_suffix(path.name)
        family = filename_family(path.name)
        role_marker = filename_role_marker(path.name)
        meta = inspect_xml(path)

        layer, review, rationale = preliminary_layer(
            rel_path=rel_path,
            filename=path.name,
            suffix=suffix,
            family=family,
            role_marker=role_marker,
            keyword_hints=str(meta["keyword_hints"]),
            canonical_paths=canonical_paths,
        )

        rows.append({
            "relative_path": rel_path,
            "filename": path.name,
            "family": family,
            "repository_suffix": suffix,
            "filename_role_marker": role_marker,
            "canonical_mapping_status": (
                "MAPPED_61_CANONICAL" if rel_path in canonical_paths else "NOT_IN_61_MAPPING"
            ),
            "preliminary_layer": layer,
            "review_status": review,
            "rationale": rationale,
            "size_bytes": path.stat().st_size,
            "size_mb": f"{path.stat().st_size / (1024 * 1024):.3f}",
            "xml_status": meta["xml_status"],
            "xml_paragraphs": meta["xml_paragraphs"],
            "keyword_hints": meta["keyword_hints"],
            "structural_headings": meta["structural_headings"],
            "opening_sample": meta["opening_sample"],
            "sha256": sha256_file(path),
        })

    mapped_found = {
        r["relative_path"]
        for r in rows
        if r["canonical_mapping_status"] == "MAPPED_61_CANONICAL"
    }
    missing_mapped = sorted(canonical_paths - mapped_found)
    if missing_mapped:
        raise SystemExit(
            "ERROR: mapped canonical XML files missing from romn/: "
            + ", ".join(missing_mapped)
        )

    post_rows = [
        r for r in rows
        if r["canonical_mapping_status"] != "MAPPED_61_CANONICAL"
    ]
    review_rows = [
        r for r in post_rows
        if r["review_status"] in {"REVIEW_REQUIRED", "HIGH_PRIORITY_REVIEW"}
    ]

    write_csv(out_dir / "FULL_ROMN_INVENTORY.csv", rows, fields)
    write_csv(out_dir / "FULL_POSTCANONICAL_INVENTORY.csv", post_rows, fields)
    write_csv(out_dir / "POSTCANONICAL_REVIEW_QUEUE.csv", review_rows, fields)

    suffix_counts = Counter(r["repository_suffix"] for r in post_rows)
    family_counts = Counter(r["family"] for r in post_rows)
    layer_counts = Counter(r["preliminary_layer"] for r in post_rows)
    review_counts = Counter(r["review_status"] for r in post_rows)

    total_bytes = sum(int(r["size_bytes"]) for r in rows)
    post_bytes = sum(int(r["size_bytes"]) for r in post_rows)
    post_paras = sum(int(r["xml_paragraphs"]) for r in post_rows if str(r["xml_paragraphs"]).isdigit())
    parse_errors = [r for r in rows if r["xml_status"] != "OK"]
    branch = git_value(root, "rev-parse", "--abbrev-ref", "HEAD")
    commit = git_value(root, "rev-parse", "HEAD")

    largest = sorted(post_rows, key=lambda r: int(r["size_bytes"]), reverse=True)[:30]
    high_priority = [
        r for r in post_rows
        if r["review_status"] == "HIGH_PRIORITY_REVIEW"
    ]

    md = [
        "# Full Post-Canonical Inventory — `romn/`",
        "",
        "## Scope",
        "",
        "This report inventories every Roman-Pāli XML file in `romn/`, then removes",
        "the 61 XML inputs already assigned to the validated Pāli Canon v1.0.0 mapping.",
        "",
        "**Repository suffixes are metadata, not final scholarly classifications.**",
        "In particular, `.att.xml`, `.tik.xml`, `.mul.xml`, and `.nrf.xml` are never",
        "treated as sufficient evidence by themselves for canonical, commentarial,",
        "subcommentarial, or other literary status.",
        "",
        "## Repository snapshot",
        "",
        f"- Branch: `{branch or 'unknown'}`",
        f"- Git SHA: `{commit or 'unknown'}`",
        f"- Total `romn/*.xml`: **{len(rows)}**",
        f"- Validated canonical inputs excluded: **{len(mapped_found)} / {mapping_rows}**",
        f"- Post-canonical/unmapped candidates: **{len(post_rows)}**",
        f"- Review queue: **{len(review_rows)}**",
        f"- XML parse errors: **{len(parse_errors)}**",
        f"- Total Roman XML size: **{total_bytes / (1024 * 1024):.2f} MB**",
        f"- Post-canonical candidate size: **{post_bytes / (1024 * 1024):.2f} MB**",
        f"- Post-canonical XML paragraphs: **{post_paras}**",
        "",
        "## Post-canonical repository suffixes",
        "",
        "| Repository suffix | Files |",
        "|---|---:|",
    ]
    for k in sorted(suffix_counts):
        md.append(f"| {k} | {suffix_counts[k]} |")

    md.extend([
        "",
        "## Filename families",
        "",
        "| Family | Files |",
        "|---|---:|",
    ])
    for k in sorted(family_counts):
        md.append(f"| {k} | {family_counts[k]} |")

    md.extend([
        "",
        "## Preliminary scholarly layers",
        "",
        "| Preliminary layer | Files |",
        "|---|---:|",
    ])
    for k in sorted(layer_counts):
        md.append(f"| {k} | {layer_counts[k]} |")

    md.extend([
        "",
        "## Review status",
        "",
        "| Status | Files |",
        "|---|---:|",
    ])
    for k in sorted(review_counts):
        md.append(f"| {k} | {review_counts[k]} |")

    md.extend([
        "",
        "## High-priority title hints",
        "",
        "These are keyword-based hints from opening XML headings/text and require",
        "manual bibliographic verification before slot allocation.",
        "",
        "| File | Suffix | Hints | Preliminary layer | MB |",
        "|---|---|---|---|---:|",
    ])
    if high_priority:
        for r in high_priority:
            md.append(
                f"| `{r['filename']}` | {r['repository_suffix']} | "
                f"`{r['keyword_hints'] or '—'}` | {r['preliminary_layer']} | "
                f"{r['size_mb']} |"
            )
    else:
        md.append("| — | — | — | — | — |")

    md.extend([
        "",
        "## 30 largest post-canonical candidates",
        "",
        "| File | Family | Suffix | Layer | Review | MB | Paragraphs |",
        "|---|---|---|---|---|---:|---:|",
    ])
    for r in largest:
        md.append(
            f"| `{r['filename']}` | {r['family']} | {r['repository_suffix']} | "
            f"{r['preliminary_layer']} | {r['review_status']} | "
            f"{r['size_mb']} | {r['xml_paragraphs']} |"
        )

    md.extend([
        "",
        "## Required next audit",
        "",
        "Before any new source bundle is built:",
        "",
        "1. identify every `REVIEW_REQUIRED` and `HIGH_PRIORITY_REVIEW` file by work title;",
        "2. verify whether `.att.xml` candidates really belong to the Aṭṭhakathā layer;",
        "3. identify all Ṭīkā and NRF families;",
        "4. identify all unmapped `.mul.xml` works;",
        "5. decide which works are essential enough to consume one of the remaining project slots;",
        "6. only then design deterministic consolidation mappings.",
        "",
        "## Machine-readable files",
        "",
        "- `FULL_ROMN_INVENTORY.csv` — all Roman XML, including the 61 canonical inputs;",
        "- `FULL_POSTCANONICAL_INVENTORY.csv` — canonical 61 excluded;",
        "- `POSTCANONICAL_REVIEW_QUEUE.csv` — ambiguous/high-priority files only;",
        "- `INVENTORY_MANIFEST.json` — snapshot and counts.",
        "",
    ])

    report_path = out_dir / "FULL_POSTCANONICAL_INVENTORY.md"
    report_path.write_text("\n".join(md), encoding="utf-8", newline="\n")

    manifest = {
        "inventory_type": "FULL_POSTCANONICAL_INVENTORY",
        "repository_branch": branch,
        "repository_git_sha": commit,
        "mapping_path": MAPPING.as_posix(),
        "mapping_rows": mapping_rows,
        "total_romn_xml": len(rows),
        "canonical_mapped_found": len(mapped_found),
        "postcanonical_candidates": len(post_rows),
        "review_queue": len(review_rows),
        "xml_parse_errors": len(parse_errors),
        "total_romn_size_bytes": total_bytes,
        "postcanonical_size_bytes": post_bytes,
        "postcanonical_xml_paragraphs": post_paras,
        "postcanonical_suffix_counts": dict(sorted(suffix_counts.items())),
        "postcanonical_family_counts": dict(sorted(family_counts.items())),
        "postcanonical_layer_counts": dict(sorted(layer_counts.items())),
        "postcanonical_review_counts": dict(sorted(review_counts.items())),
        "output_sha256": {},
    }

    # Hash deterministic tabular/report outputs. Manifest intentionally does not hash itself.
    for name in [
        "FULL_ROMN_INVENTORY.csv",
        "FULL_POSTCANONICAL_INVENTORY.csv",
        "POSTCANONICAL_REVIEW_QUEUE.csv",
        "FULL_POSTCANONICAL_INVENTORY.md",
    ]:
        manifest["output_sha256"][name] = sha256_file(out_dir / name)

    (out_dir / "INVENTORY_MANIFEST.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )

    print("FULL POST-CANONICAL INVENTORY COMPLETE")
    print(f"Repository Git SHA: {commit or 'unknown'}")
    print(f"Total romn XML: {len(rows)}")
    print(f"Canonical mapped/excluded: {len(mapped_found)}")
    print(f"Post-canonical candidates: {len(post_rows)}")
    print(f"Review queue: {len(review_rows)}")
    print(f"Parse errors: {len(parse_errors)}")
    print(f"Output directory: {OUT_DIR}")


if __name__ == "__main__":
    main()
