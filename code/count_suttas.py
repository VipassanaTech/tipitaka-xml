#!/usr/bin/env python3
"""
count_suttas.py

Parses a script's tree.json to find all leaf nodes.
For each leaf node's corresponding XML section, counts <p rend="subhead"> elements
and captures their text. Outputs a {script}_subheads.json file.

Usage:
  C:/dev/python3x/python.exe count_suttas.py [script]
  C:/dev/python3x/python.exe count_suttas.py romn
  C:/dev/python3x/python.exe count_suttas.py deva
  C:/dev/python3x/python.exe count_suttas.py thai
  (defaults to "romn" if no argument given)
"""

import json
import os
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

BASE_DIR = r"c:\tipitaka\tipitaka-xml"

def get_paths(script):
    """Return (tree_json, master_xml_dir, toc_dir, output_file) for a given script."""
    tree_json = os.path.join(BASE_DIR, "tipitaka.org", script, "tree.json")
    master_xml_dir = os.path.join(BASE_DIR, script)
    toc_dir = os.path.join(BASE_DIR, "tipitaka.org", script, "cscd")
    output_file = os.path.join(BASE_DIR, "tipitaka.org", script, f"{script}_subheads.json")
    return tree_json, master_xml_dir, toc_dir, output_file


def parse_tree_json(filepath):
    """Parse the UTF-16LE tree.json and return list of (id, href) for leaf nodes."""
    with open(filepath, "r", encoding="utf-16") as f:
        data = json.load(f)

    leaves = []

    def walk(node):
        if isinstance(node, list):
            for item in node:
                walk(item)
        elif isinstance(node, dict):
            if node.get("type") == "leaf" and "a_attr" in node and "href" in node["a_attr"]:
                leaves.append((node["id"], node["a_attr"]["href"]))
            if "children" in node:
                walk(node["children"])

    walk(data)
    return leaves


def parse_href(href, master_xml_dir):
    """
    Parse a cscd href to extract (master_xml_path, section_index, is_whole_file).
    """
    filename = href.replace("cscd/", "")

    # Pattern: BASE.TYPE_N.xml → master: BASE.TYPE.xml, index: N
    m = re.match(r"^(.+?)\.(mul|att|tik|nrf)(\d+)\.xml$", filename)
    if m:
        base = m.group(1)
        ext = m.group(2)
        index = int(m.group(3))
        master_path = os.path.join(master_xml_dir, f"{base}.{ext}.xml")
        return master_path, index, False

    # Pattern: BASENAME.xml → whole file
    if filename.endswith(".xml"):
        master_path = os.path.join(master_xml_dir, filename)
        return master_path, None, True

    print(f"WARNING: Cannot parse href: {href}")
    return None, None, False


def parse_toc_indices(toc_path):
    """
    Parse a TOC file, return sorted list of section indices from action hrefs.
    """
    if not os.path.exists(toc_path):
        return []

    indices = []
    try:
        for event, elem in ET.iterparse(toc_path, events=("start",)):
            action = elem.get("action", "")
            if action:
                fn = action.replace("cscd/", "")
                m = re.match(r"^.+?\.(?:mul|att|tik|nrf)(\d+)\.xml$", fn)
                if m:
                    indices.append(int(m.group(1)))
            elem.clear()
    except ET.ParseError as e:
        print(f"  ERROR parsing TOC {toc_path}: {e}")
        return []

    return sorted(indices)


def count_subheads_per_chapter(master_xml_path):
    """
    Count <p rend="subhead"> per chapter and collect their text.
    Chapter boundary: <head rend="chapter"> or <p rend="chapter">.
    Returns list[i] = [subhead_texts...] for chapter i.
    """
    if not os.path.exists(master_xml_path):
        return []

    chapters = []      # list of lists of subhead texts
    current = []
    in_chapter = False

    try:
        for event, elem in ET.iterparse(master_xml_path, events=("start",)):
            if elem.tag in ("head", "p") and elem.get("rend") == "chapter":
                if in_chapter:
                    chapters.append(current)
                    current = []
                else:
                    in_chapter = True
                elem.clear()
                continue

            if elem.tag == "p" and in_chapter and elem.get("rend") == "subhead":
                text = (elem.text or "").strip()
                current.append(text)

            elem.clear()

        if in_chapter:
            chapters.append(current)

    except ET.ParseError as e:
        print(f"  ERROR parsing {master_xml_path}: {e}")
        return []

    return chapters


def get_toc_path(master_xml_path, toc_dir):
    """Get TOC path from master XML path."""
    basename = os.path.basename(master_xml_path)
    return os.path.join(toc_dir, basename.replace(".xml", ".toc.xml"))


def build_index_map(toc_indices, num_chapters):
    """
    Map TOC indices → chapter indices.

    Returns dict: {toc_index: chapter_index}, where chapter_index=-1 means preamble (0 subheads).
    """
    mapping = {}
    n_toc = len(toc_indices)
    if n_toc == 0:
        return mapping

    if n_toc == num_chapters:
        for i, toc_idx in enumerate(toc_indices):
            mapping[toc_idx] = i
    elif n_toc == num_chapters + 1:
        mapping[toc_indices[0]] = -1
        for i in range(1, n_toc):
            mapping[toc_indices[i]] = i - 1
    else:
        print(f"  WARNING: TOC has {n_toc} entries but {num_chapters} chapters")
        for i, toc_idx in enumerate(toc_indices):
            mapping[toc_idx] = i if i < num_chapters else -1

    return mapping


def main():
    script = sys.argv[1] if len(sys.argv) > 1 else "romn"
    tree_json, master_xml_dir, toc_dir, output_file = get_paths(script)

    print(f"=== Sutta Counting Script ({script}) ===")
    print()

    print(f"Parsing {script}/tree.json...")
    leaves = parse_tree_json(tree_json)
    print(f"  Found {len(leaves)} leaf nodes")

    master_to_leaves = defaultdict(list)
    unparsed = []
    for node_id, href in leaves:
        master_path, index, is_whole = parse_href(href, master_xml_dir)
        if master_path is None:
            unparsed.append((node_id, href))
        else:
            master_to_leaves[master_path].append((node_id, href, index, is_whole))

    if unparsed:
        print(f"  WARNING: {len(unparsed)} unparsed hrefs")

    unique_masters = len(master_to_leaves)
    print(f"  Unique master XML files: {unique_masters}")

    result = {}
    processed = 0

    for master_path, leaf_list in sorted(master_to_leaves.items()):
        processed += 1
        if processed % 20 == 0:
            print(f"  Processing {processed}/{unique_masters}...")

        has_split = any(not is_whole for _, _, _, is_whole in leaf_list)
        chapter_subheads = count_subheads_per_chapter(master_path)

        def make_entry(subhead_list):
            return {"count": len(subhead_list), "subheads": subhead_list}

        if has_split:
            toc_path = get_toc_path(master_path, toc_dir)
            toc_indices = parse_toc_indices(toc_path)
            index_map = build_index_map(toc_indices, len(chapter_subheads))

            for node_id, href, idx, is_whole in leaf_list:
                if is_whole or idx is None:
                    result[node_id] = make_entry(chapter_subheads[0] if chapter_subheads else [])
                elif idx in index_map:
                    ci = index_map[idx]
                    if ci == -1:
                        result[node_id] = make_entry([])
                    elif ci < len(chapter_subheads):
                        result[node_id] = make_entry(chapter_subheads[ci])
                    else:
                        result[node_id] = make_entry([])
                else:
                    sl = chapter_subheads[idx] if idx < len(chapter_subheads) else []
                    result[node_id] = make_entry(sl)
        else:
            all_subheads = []
            for ch in chapter_subheads:
                all_subheads.extend(ch)
            for node_id, href, idx, is_whole in leaf_list:
                result[node_id] = make_entry(all_subheads)

    print(f"\nWriting output to {output_file}...")
    sorted_result = {str(k): result[k] for k in sorted(result.keys())}

    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(sorted_result, f, indent=2, ensure_ascii=False)

    total_subheads = sum(v["count"] for v in result.values())
    non_zero = sum(1 for v in result.values() if v["count"] > 0)
    print(f"\n=== Summary ===")
    print(f"  Script: {script}")
    print(f"  Total leaf nodes: {len(result)}")
    print(f"  Leaf nodes with suttas: {non_zero}")
    print(f"  Total subheads counted: {total_subheads}")
    print(f"  Output: {output_file}")


if __name__ == "__main__":
    main()
