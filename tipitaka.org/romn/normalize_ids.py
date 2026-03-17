#!/usr/bin/env python3
"""
Normalize `id` fields in tree.json by deriving from the `text` field above.
- Lowercase
- Strip leading numbering/punctuation (e.g. "1.", "(6) 1.") by finding first letter
- Remove Unicode diacritics (ā, ṭ, ṇ, ḍ, ṅ, etc.)
- Keep only ASCII letters a-z (concatenate words)

Usage:
  python normalize_ids.py 
  python normalize_ids.py --input tree.json --output tree_edit.json

The script rewrites every object that contains both 'text' and 'id'. If the normalized
id would be empty, the script falls back to 'id' + original numeric id.

"""
from __future__ import annotations
import argparse
import json
import unicodedata
from typing import Any
import re
import os
import hashlib

# Number of characters to take from the second-highest-level parent's sanitized
# text when creating disambiguation prefixes. Increasing this reduces collisions.
PREFIX_LEN = 8


def is_letter(ch: str) -> bool:
    """Return True if character is a Unicode letter."""
    return ch and unicodedata.category(ch).startswith("L")


def strip_leading_nonletters(s: str) -> str:
    """Return substring starting at the first Unicode letter.
    This removes leading numbers, punctuation, parentheses and spaces.
    """
    for i, ch in enumerate(s):
        if is_letter(ch):
            return s[i:]
    return ""


def remove_diacritics(s: str) -> str:
    """Decompose and remove combining marks (diacritics)."""
    normalized = unicodedata.normalize("NFD", s)
    return "".join(ch for ch in normalized if not unicodedata.category(ch).startswith("M"))


def normalize_text_to_id(text: str) -> str:
    """Produce an id string from `text`:
    - lowercase
    - remove diacritics
    - preserve numeric groups (e.g. (10) 5.) and alphabetic words
    - join numeric groups and word runs with periods, and words are separated by underscore parts
    Example: "(10) 5. Bālavaggo" -> digits ['10','5'], words ['balavaggo'] -> "10.5.balavaggo"
    """
    if not isinstance(text, str):
        return ""
    s = text.strip()
    s = s.lower()
    s = remove_diacritics(s)

    # find digit groups and alphabetic runs
    digits = re.findall(r"\d+", s)
    words = re.findall(r"[a-z]+", s)

    parts: list[str] = []
    # preserve digits first (they usually indicate grouping like (10) 5.)
    parts.extend(digits)
    # then append the word-run joined with underscore (so multi-word titles become a single token)
    if words:
        parts.append('_'.join(words))

    return '.'.join(parts)


def transform(obj: Any, changed: dict) -> Any:
    """Recursively walk and replace numeric id values with normalized ids.
    `changed` is a dict used to tally how many replacements were done.
    """
    if isinstance(obj, dict):
        # Only replace id when this node is a leaf (type == 'leaf') and has text/id
        if 'text' in obj and 'id' in obj and obj.get('type') == 'leaf':
            text_val = obj.get('text')
            old_id = obj.get('id')
            base_id = normalize_text_to_id(text_val)
            if not base_id:
                # fallback to prefix "id" + original id value (string)
                base_id = f"id{old_id}"

            # Ensure uniqueness across the whole tree by consulting changed['used'] set
            used = changed.setdefault('used', set())
            final_id = base_id
            suffix = 1
            collided = False
            if final_id in used:
                collided = True
                # try suffixes until unique
                while final_id in used:
                    final_id = f"{base_id}_{suffix}"
                    suffix += 1

            # assign final id and register it
            final_id = str(final_id)
            if obj['id'] != final_id:
                obj['id'] = final_id
                changed['count'] += 1
            used.add(final_id)

            # Register mapping from final id to the node's original old id and node reference (for later reference)
            assigned_map = changed.setdefault('assigned_map', {})
            assigned_node_map = changed.setdefault('assigned_node_map', {})
            assigned_map[final_id] = str(old_id)
            assigned_node_map[final_id] = obj

            # If we had to disambiguate a base because it was already used, try parent-prefix disambiguation
            if collided:
                # see if the base was previously assigned (another node in this run)
                other_oldid = assigned_map.get(base_id)
                other_node = assigned_node_map.get(base_id)

                duplicated_with_oldid = None

                if other_oldid and other_node:
                    # get parent texts for both nodes from existing_map
                    existing_map = changed.get('existing_map', {})

                    # existing_map maps original old numeric ids (as strings) to list of dicts
                    my_entries = existing_map.get(str(old_id))
                    other_entries = existing_map.get(str(other_oldid))

                    my_immediate = my_entries[0].get('immediate_parent_text') if my_entries else None
                    my_second = my_entries[0].get('second_parent_text') if my_entries else None
                    other_immediate = other_entries[0].get('immediate_parent_text') if other_entries else None
                    other_second = other_entries[0].get('second_parent_text') if other_entries else None

                    # sanitize parent texts to PREFIX_LEN-character prefixes
                    def prefix_from_parent(pt):
                        if not pt:
                            return None
                        p = remove_diacritics(pt.lower())
                        p = ''.join(ch for ch in p if 'a' <= ch <= 'z')
                        return p[:PREFIX_LEN] if p else None

                    # Create full (not truncated) sanitized second-parent strings
                    def sanitize_full_parent(pt):
                        if not pt:
                            return None
                        p = remove_diacritics(pt.lower())
                        parts = re.findall(r"[a-z]+", p)
                        # remove trailing 'mula' token (e.g. 'tipitaka_mula' -> 'tipitaka') as requested
                        if parts and parts[-1] == 'mula':
                            parts = parts[:-1]
                        return '_'.join(parts) if parts else None

                    other_second_full = sanitize_full_parent(other_second)
                    my_second_full = sanitize_full_parent(my_second)

                    # Prefer using the full second-parent name as the suffix (no truncation)
                    if other_second_full:
                        new_other_final = f"{base_id}_{other_second_full}"
                        # apply to other node
                        other_node['id'] = new_other_final
                        # update maps and used set
                        assigned_map.pop(base_id, None)
                        assigned_node_map.pop(base_id, None)
                        assigned_map[new_other_final] = str(other_oldid)
                        assigned_node_map[new_other_final] = other_node
                        used.discard(base_id)
                        used.add(new_other_final)

                    if my_second_full:
                        new_my_final = f"{base_id}_{my_second_full}"
                        final_id = new_my_final
                        obj['id'] = final_id
                        assigned_map[final_id] = str(old_id)
                        assigned_node_map[final_id] = obj
                        used.add(final_id)

                    # If after using full second-parent suffix we still have a duplicate
                    # (rare), append first 6 hex chars of SHA1 for a stable short unique suffix.
                    def append_sha6(node_obj, current_final, orig_old_id):
                        if current_final in used:
                            # compute stable hash from base_id + original old id
                            h = hashlib.sha1(f"{base_id}|{orig_old_id}".encode('utf-8')).hexdigest()[:6]
                            new_final = f"{current_final}_{h}"
                            node_obj['id'] = new_final
                            # update maps/used
                            assigned_map.pop(current_final, None)
                            assigned_node_map.pop(current_final, None)
                            assigned_map[new_final] = str(orig_old_id)
                            assigned_node_map[new_final] = node_obj
                            used.discard(current_final)
                            used.add(new_final)
                            return new_final
                        return current_final

                    # ensure uniqueness for the other node (if updated)
                    if other_second_full:
                        other_final_current = assigned_map and list(filter(lambda k: assigned_map.get(k) == str(other_oldid), assigned_map.keys()))
                        # attempt to find the key we just set; fallback to constructed value
                        other_current = new_other_final if 'new_other_final' in locals() else base_id
                        new_other_final = append_sha6(other_node, other_current, other_oldid)

                    # ensure uniqueness for current node
                    if my_second_full:
                        final_id = append_sha6(obj, final_id, old_id)

                    # record duplication details
                    duplicated_with_oldid = other_oldid

                    coll_list = changed.setdefault('collisions', [])
                    coll_list.append({'text': text_val, 'old_id': old_id, 'base': base_id, 'final': final_id, 'duplicated_with_oldid': duplicated_with_oldid})
                else:
                    # fallback: record collision with whatever we have
                    existing_map = changed.get('existing_map', {})
                    ex = existing_map.get(base_id)
                    if ex:
                        duplicated_with_oldid = ex[0]
                    coll_list = changed.setdefault('collisions', [])
                    coll_list.append({'text': text_val, 'old_id': old_id, 'base': base_id, 'final': final_id, 'duplicated_with_oldid': duplicated_with_oldid})
        # Recurse into values
        for k, v in list(obj.items()):
            obj[k] = transform(v, changed)
        return obj
    elif isinstance(obj, list):
        return [transform(x, changed) for x in obj]
    else:
        return obj


def main() -> None:
    parser = argparse.ArgumentParser(description="Normalize id fields in a JSON tree based on the preceding text value.")
    parser.add_argument('--input', '-i', default='tree.json', help='Input JSON file path (default: tree.json)')
    parser.add_argument('--output', '-o', default='treebyname.json', help='Output JSON file path (default: treebyname.json)')
    args = parser.parse_args()

    in_path = args.input
    out_path = args.output

    if not os.path.exists(in_path):
        print(f"Input file not found: {in_path}")
        return

    def load_json_with_encodings(path: str):
        """Try loading JSON from file using several encodings until one works.
        Returns tuple (data, encoding_used).
        """
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
                # try next encoding
                continue

        # If none succeeded, raise a clear error
        raise RuntimeError(f"Failed to decode JSON file '{path}' with encodings {encodings_to_try}. Last error: {last_exc}")

    data, used_encoding = load_json_with_encodings(in_path)
    print(f"Loaded {in_path} using encoding: {used_encoding}")

    # Collect all existing ids (as strings) to avoid collisions with generated ids
    def collect_existing_ids(obj: Any, used: set, existing_map: dict, ancestors: list | None = None) -> None:
        """Traverse the tree and record existing ids.
        Track an `ancestors` list of ancestor `text` values (root-first). For each node
        we store the second-highest-level parent's full text (i.e. the ancestor at index 1,
        if present). This is used later for deterministic disambiguation.
        """
        if ancestors is None:
            ancestors = []

        if isinstance(obj, dict):
            # record this node's id using the current ancestor stack
            if 'id' in obj:
                id_str = str(obj['id'])
                used.add(id_str)
                # determine second-highest-level parent: ancestor at index 1 (child of root)
                second_parent = None
                if len(ancestors) >= 2:
                    second_parent = ancestors[1]
                elif len(ancestors) == 1:
                    # if only one ancestor, use the root
                    second_parent = ancestors[0]
                # immediate parent is the last ancestor (if any)
                immediate_parent = ancestors[-1] if len(ancestors) >= 1 else None
                # record both immediate and second_parent texts and node reference for this original id
                existing_map.setdefault(id_str, []).append({'immediate_parent_text': immediate_parent, 'second_parent_text': second_parent, 'node': obj})

            # build next ancestor stack: prefer this node's text if present
            my_text = obj.get('text') if isinstance(obj.get('text'), str) else None
            next_ancestors = list(ancestors)
            if my_text is not None:
                next_ancestors.append(my_text)

            for v in obj.values():
                collect_existing_ids(v, used, existing_map, next_ancestors)
        elif isinstance(obj, list):
            for x in obj:
                collect_existing_ids(x, used, existing_map, ancestors)

    used_ids = set()
    existing_map: dict = {}
    collect_existing_ids(data, used_ids, existing_map)

    changed = {'count': 0, 'used': used_ids, 'collisions': [], 'existing_map': existing_map, 'assigned_map': {}, 'assigned_node_map': {}}
    data = transform(data, changed)

    # Final deduplication pass: scan for any remaining duplicate `id` values and
    # append a stable 6-hex SHA1 suffix derived from (final_id | original_old_id).
    # We use changed['existing_map'] which maps original numeric ids (str) to entries
    # containing the node reference. This allows us to find all nodes that currently
    # share the same final id.
    final_map: dict = {}
    for oldid_str, entries in changed.get('existing_map', {}).items():
        for entry in entries:
            node = entry.get('node')
            if not isinstance(node, dict):
                continue
            final = str(node.get('id')) if 'id' in node else None
            if final is None:
                continue
            final_map.setdefault(final, []).append((oldid_str, node))

    # For any final id that maps to multiple nodes, append sha6 to all but the first
    for final_id, list_entries in final_map.items():
        if len(list_entries) <= 1:
            continue
        # keep the first as-is; change the rest
        for oldid_str, node in list_entries[1:]:
            sha6 = hashlib.sha1(f"{final_id}|{oldid_str}".encode('utf-8')).hexdigest()[:6]
            new_final = f"{final_id}_{sha6}"
            # apply change to node
            node['id'] = new_final
            # record this in collisions for auditing
            coll_list = changed.setdefault('collisions', [])
            coll_list.append({'text': node.get('text'), 'old_id': oldid_str, 'base': final_id, 'final': new_final, 'resolved_by': 'sha1'})
            # update used set
            changed['used'].add(new_final)

    # Write output
    with open(out_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    print(f"Wrote {out_path}. Replaced {changed['count']} id values.")

    # Report collisions (where multiple nodes normalized to same base)
    collisions = changed.get('collisions', [])
    if collisions:
        print(f"Detected {len(collisions)} potential collisions while normalizing ids. A report will be written to treebyname_duplicates.json")
        # Write a small report with details
        report = {
            'replaced_count': changed['count'],
            'collision_count': len(collisions),
            'collisions': collisions,
        }
        report_path = os.path.join(os.path.dirname(out_path), 'treebyname_duplicates.json') if os.path.dirname(out_path) else 'treebyname_duplicates.json'
        with open(report_path, 'w', encoding='utf-8') as rf:
            json.dump(report, rf, ensure_ascii=False, indent=2)
        print(f"Wrote collision report to {report_path}")
    else:
        print("No collisions detected.")


if __name__ == '__main__':
    main()
