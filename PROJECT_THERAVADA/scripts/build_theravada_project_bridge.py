#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Build synchronized bridge/index files for the two Theravāda ChatGPT projects.

This DOES NOT connect ChatGPT projects programmatically.
It creates deterministic shared metadata files that can be uploaded to both
projects and kept in sync through GitHub.

Input:
  PROJECT_THERAVADA/00_INDEX/THERAVADA_SOURCE_REGISTRY.csv

Outputs:
  PROJECT_THERAVADA/00_INDEX/BRIDGE/
    THERAVADA_MASTER_BRIDGE.md
    THERAVADA_I_BRIDGE.md
    THERAVADA_II_BRIDGE.md
    THERAVADA_BRIDGE_MANIFEST.json
"""

from __future__ import annotations

import csv
import hashlib
import json
import subprocess
from collections import Counter, defaultdict
from pathlib import Path

REGISTRY = Path("PROJECT_THERAVADA/00_INDEX/THERAVADA_SOURCE_REGISTRY.csv")
OUT_DIR = Path("PROJECT_THERAVADA/00_INDEX/BRIDGE")

REQUIRED_FIELDS = [
    "source_id",
    "home_project",
    "slot",
    "title",
    "author",
    "year",
    "layer",
    "function",
    "status",
    "filename_or_location",
    "sha256",
    "rights_or_license",
    "notes",
]

VALID_PROJECTS = {"THERAVADA_I", "THERAVADA_II", "EXTERNAL"}
VALID_STATUS = {"ACTIVE", "PLANNED", "ON_DEMAND", "ARCHIVED"}


def repo_root() -> Path:
    here = Path(__file__).resolve()
    for p in [here.parent, *here.parents, Path.cwd().resolve(), *Path.cwd().resolve().parents]:
        if (p / "PROJECT_THERAVADA").is_dir():
            return p
    raise SystemExit("ERROR: repository root not found")


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def git_value(root: Path, *args: str) -> str:
    try:
        return subprocess.check_output(
            ["git", "-C", str(root), *args],
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
    except Exception:
        return ""


def load_registry(path: Path) -> list[dict[str, str]]:
    if not path.is_file():
        raise SystemExit(f"ERROR: registry missing: {path}")

    with path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        fields = reader.fieldnames or []
        missing = [x for x in REQUIRED_FIELDS if x not in fields]
        if missing:
            raise SystemExit(f"ERROR: registry missing fields: {', '.join(missing)}")
        rows = []
        for raw in reader:
            row = {k: (raw.get(k) or "").strip() for k in REQUIRED_FIELDS}
            if not row["source_id"]:
                continue
            rows.append(row)

    seen_ids = set()
    seen_slots = set()
    for row in rows:
        sid = row["source_id"]
        if sid in seen_ids:
            raise SystemExit(f"ERROR: duplicate source_id: {sid}")
        seen_ids.add(sid)

        project = row["home_project"]
        if project not in VALID_PROJECTS:
            raise SystemExit(f"ERROR: invalid home_project for {sid}: {project}")

        status = row["status"]
        if status not in VALID_STATUS:
            raise SystemExit(f"ERROR: invalid status for {sid}: {status}")

        slot = row["slot"]
        if project != "EXTERNAL" and status == "ACTIVE":
            if not slot.isdigit() or not (1 <= int(slot) <= 25):
                raise SystemExit(f"ERROR: ACTIVE {project} source {sid} needs slot 1..25")
            key = (project, int(slot))
            if key in seen_slots:
                raise SystemExit(f"ERROR: duplicate active slot: {project} {slot}")
            seen_slots.add(key)

    return rows


def md_table(rows: list[dict[str, str]]) -> list[str]:
    lines = [
        "| Slot | ID | Layer | Source | Function | Status |",
        "|---:|---|---|---|---|---|",
    ]
    for r in sorted(
        rows,
        key=lambda x: (
            999 if not x["slot"].isdigit() else int(x["slot"]),
            x["source_id"],
        ),
    ):
        author = f"{r['author']} — " if r["author"] else ""
        year = f" ({r['year']})" if r["year"] else ""
        source = f"{author}{r['title']}{year}".replace("|", "/")
        lines.append(
            f"| {r['slot'] or '—'} | `{r['source_id']}` | "
            f"{r['layer'].replace('|','/')} | {source} | "
            f"{r['function'].replace('|','/')} | {r['status']} |"
        )
    return lines


def detail_blocks(rows: list[dict[str, str]]) -> list[str]:
    lines = []
    for r in sorted(rows, key=lambda x: x["source_id"]):
        lines.extend([
            f"### `{r['source_id']}` — {r['title']}",
            "",
            f"- Home project: `{r['home_project']}`",
            f"- Slot: `{r['slot'] or '—'}`",
            f"- Layer: `{r['layer']}`",
            f"- Function: `{r['function']}`",
            f"- Status: `{r['status']}`",
            f"- File/location: `{r['filename_or_location'] or '—'}`",
            f"- SHA-256: `{r['sha256'] or '—'}`",
            f"- Rights/license: {r['rights_or_license'] or '—'}",
            f"- Notes: {r['notes'] or '—'}",
            "",
        ])
    return lines


def write_project_view(
    out_path: Path,
    title: str,
    project_code: str,
    rows: list[dict[str, str]],
    git_sha: str,
) -> None:
    own = [r for r in rows if r["home_project"] == project_code]
    other = [
        r for r in rows
        if r["home_project"] not in {project_code, "EXTERNAL"} and r["status"] == "ACTIVE"
    ]
    external = [r for r in rows if r["home_project"] == "EXTERNAL"]

    active_slots = sorted(int(r["slot"]) for r in own if r["status"] == "ACTIVE" and r["slot"].isdigit())
    free = [i for i in range(1, 26) if i not in active_slots]

    md = [
        f"# {title}",
        "",
        "> Cross-project bridge generated from `THERAVADA_SOURCE_REGISTRY.csv`.",
        "> It is an index/provenance layer, not a substitute for the source texts.",
        "",
        "## Snapshot",
        "",
        f"- Repository Git SHA: `{git_sha or 'unknown'}`",
        f"- Active home sources in this project: **{len(active_slots)}/25**",
        f"- Free slots by registry: **{len(free)}**",
        f"- Free slot numbers: `{', '.join(map(str, free)) if free else 'none'}`",
        "",
        "## Sources homed in this project",
        "",
    ]
    md += md_table(own)

    md += [
        "",
        "## Active sources homed in the sibling project",
        "",
        "Use these as cross-project controls. For exact quotation or page-level analysis,",
        "open/upload the original source or use the repository/File Library; this bridge",
        "contains metadata only.",
        "",
    ]
    md += md_table(other) if other else ["_No active sibling-project sources registered._"]

    if external:
        md += [
            "",
            "## External / on-demand references",
            "",
        ]
        md += md_table(external)

    md += [
        "",
        "## Source hierarchy",
        "",
        "1. MŪLA — canonical/root Pāli text",
        "2. AṬṬHAKATHĀ — classical commentary",
        "3. ṬĪKĀ / ANUṬĪKĀ — subcommentary",
        "4. CLASSICAL SYSTEMATIC / MANUAL",
        "5. PHILOLOGY / DICTIONARY / TRANSLATION CONTROL",
        "6. MODERN THERAVĀDA SYSTEMATIC",
        "7. ACADEMIC BUDDHIST STUDIES",
        "8. MODERN PRACTICE TRADITIONS",
        "",
        "Never silently collapse these layers into one authority level.",
        "",
    ]

    out_path.write_text("\n".join(md), encoding="utf-8", newline="\n")


def main() -> None:
    root = repo_root()
    registry_path = root / REGISTRY
    out_dir = root / OUT_DIR
    out_dir.mkdir(parents=True, exist_ok=True)

    rows = load_registry(registry_path)
    git_sha = git_value(root, "rev-parse", "HEAD")
    branch = git_value(root, "rev-parse", "--abbrev-ref", "HEAD")

    by_project = Counter(r["home_project"] for r in rows)
    active_by_project = Counter(
        r["home_project"] for r in rows if r["status"] == "ACTIVE"
    )

    master = [
        "# Theravāda I ↔ Theravāda II — Master Bridge",
        "",
        "This file is the shared synchronization/index layer between:",
        "",
        "- **Theravāda I — Pāli Canon & Classical Commentarial Corpus**",
        "- **Theravāda II — Modern Scholarship, Philology & Practice**",
        "",
        "It does **not** technically merge ChatGPT project file stores.",
        "Its purpose is to give both projects the same source IDs, layer labels,",
        "slot map, provenance, rights notes, and sibling-project awareness.",
        "",
        "## Repository snapshot",
        "",
        f"- Branch: `{branch or 'unknown'}`",
        f"- Git SHA: `{git_sha or 'unknown'}`",
        f"- Registry rows: **{len(rows)}**",
        f"- Theravāda I active: **{active_by_project.get('THERAVADA_I', 0)}/25**",
        f"- Theravāda II active: **{active_by_project.get('THERAVADA_II', 0)}/25**",
        "",
        "## Full registry",
        "",
    ]
    master += md_table(rows)
    master += [
        "",
        "## Detailed provenance cards",
        "",
    ]
    master += detail_blocks(rows)

    master_path = out_dir / "THERAVADA_MASTER_BRIDGE.md"
    master_path.write_text("\n".join(master), encoding="utf-8", newline="\n")

    write_project_view(
        out_dir / "THERAVADA_I_BRIDGE.md",
        "Theravāda I — Classical Corpus Bridge",
        "THERAVADA_I",
        rows,
        git_sha,
    )
    write_project_view(
        out_dir / "THERAVADA_II_BRIDGE.md",
        "Theravāda II — Modern Scholarship, Philology & Practice Bridge",
        "THERAVADA_II",
        rows,
        git_sha,
    )

    outputs = [
        "THERAVADA_MASTER_BRIDGE.md",
        "THERAVADA_I_BRIDGE.md",
        "THERAVADA_II_BRIDGE.md",
    ]
    manifest = {
        "bridge_version": "1.0",
        "repository_branch": branch,
        "repository_git_sha": git_sha,
        "registry": REGISTRY.as_posix(),
        "registry_sha256": sha256_file(registry_path),
        "registry_rows": len(rows),
        "home_project_counts": dict(sorted(by_project.items())),
        "active_project_counts": dict(sorted(active_by_project.items())),
        "outputs": {
            name: sha256_file(out_dir / name)
            for name in outputs
        },
        "important_limitation": (
            "Bridge files synchronize metadata only; they do not grant one ChatGPT "
            "project direct access to another project's source files."
        ),
    }
    (out_dir / "THERAVADA_BRIDGE_MANIFEST.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )

    print("THERAVADA PROJECT BRIDGE BUILD: PASS")
    print(f"Git SHA: {git_sha or 'unknown'}")
    print(f"Registry rows: {len(rows)}")
    print(f"Theravada I active: {active_by_project.get('THERAVADA_I', 0)}/25")
    print(f"Theravada II active: {active_by_project.get('THERAVADA_II', 0)}/25")
    print(f"Output: {OUT_DIR}")


if __name__ == "__main__":
    main()
