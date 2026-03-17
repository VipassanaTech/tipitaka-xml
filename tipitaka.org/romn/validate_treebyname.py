#!/usr/bin/env python3
"""Validate uniqueness of `id` fields in a JSON tree (default: treebyname.json).
Prints a summary and lists any duplicate ids with sample locations.

Usage:
  python validate_treebyname.py --input treebyname.json
"""
from __future__ import annotations
import argparse
import json
import os
from typing import Any, Dict, List


def load_json_with_encodings(path: str):
    with open(path, 'rb') as f:
        raw = f.read()
    encodings_to_try = ['utf-8', 'utf-8-sig', 'utf-16', 'utf-16-le', 'utf-16-be', 'latin-1']
    last_exc = None
    for enc in encodings_to_try:
        try:
            text = raw.decode(enc)
            return json.loads(text), enc
        except Exception as e:
            last_exc = e
            continue
    raise RuntimeError(f"Failed to decode JSON file '{path}' with encodings {encodings_to_try}. Last error: {last_exc}")


def traverse(obj: Any, path: str, id_map: Dict[str, List[Dict[str, str]]]):
    if isinstance(obj, dict):
        if 'id' in obj:
            id_str = str(obj['id'])
            entry = {'path': path, 'text': str(obj.get('text', '')) , 'type': str(obj.get('type', ''))}
            id_map.setdefault(id_str, []).append(entry)
        for k, v in obj.items():
            new_path = f"{path}.{k}" if path else k
            traverse(v, new_path, id_map)
    elif isinstance(obj, list):
        for i, item in enumerate(obj):
            new_path = f"{path}[{i}]"
            traverse(item, new_path, id_map)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--input', '-i', default='treebyname.json')
    args = parser.parse_args()

    in_path = args.input
    if not os.path.exists(in_path):
        print(f"Input not found: {in_path}")
        return

    data, enc = load_json_with_encodings(in_path)
    print(f"Loaded {in_path} with encoding {enc}")

    id_map: Dict[str, List[Dict[str, str]]] = {}
    traverse(data, '', id_map)

    total_ids = sum(len(v) for v in id_map.values())
    unique_ids = len(id_map)
    duplicates = {k: v for k, v in id_map.items() if len(v) > 1}

    print(f"Scanned nodes with id: {total_ids}")
    print(f"Unique id values: {unique_ids}")
    print(f"Duplicate id values: {len(duplicates)}")

    if duplicates:
        # write report
        report_path = os.path.splitext(in_path)[0] + '_validate_duplicates.json'
        report = {'duplicate_count': len(duplicates), 'duplicates': {k: v for k, v in duplicates.items()}}
        with open(report_path, 'w', encoding='utf-8') as rf:
            json.dump(report, rf, ensure_ascii=False, indent=2)
        print(f"Wrote duplicate report to {report_path}")
        # print a few examples
        for k, v in list(duplicates.items())[:20]:
            print(f"Duplicate id: {k}  occurrences: {len(v)}")
            for e in v[:3]:
                print(f"  - path={e['path']} type={e['type']} text={e['text']}")
    else:
        print("No duplicate ids found.")

if __name__ == '__main__':
    main()
