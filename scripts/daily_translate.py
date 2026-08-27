#!/usr/bin/env python3
"""
Daily Tipitaka reading job.

Reads the next chunk of verses from the current position in a rotating list of
romn/*.mul.xml files, translates it to English via the Anthropic API, emails
the result via Resend, and advances the on-disk progress state.

A "verse" is a <p rend="bodytext" n="N"> element plus any immediately
following unnumbered <p rend="bodytext"> continuation paragraphs. A chunk
never crosses a chapter/kanda boundary (i.e. never spans two different
immediate parent <div> elements), even if that means fewer verses than
CHUNK_SIZE.
"""
import json
import os
import re
import sys
import traceback
import urllib.request
import xml.etree.ElementTree as ET
from datetime import datetime, timezone

ANTHROPIC_URL = "https://api.anthropic.com/v1/messages"
RESEND_URL = "https://api.resend.com/emails"

WHITESPACE_RE = re.compile(r"\s+")


def normalize(text):
    return WHITESPACE_RE.sub(" ", text).strip()


def clean_text(elem):
    parts = [elem.text or ""]
    for child in elem:
        skip = child.tag == "note" or (
            child.tag == "hi" and child.get("rend") in ("paranum", "dot")
        )
        if not skip:
            parts.append(clean_text(child))
        parts.append(child.tail or "")
    return "".join(parts)


def heading_path(div_node, parent_map):
    chain = []
    node = div_node
    while node is not None:
        if node.tag == "div":
            head = node.find("head")
            if head is not None and (head.text or "").strip():
                chain.append(head.text.strip())
        node = parent_map.get(node)
    chain.reverse()
    return chain


def parse_verses(filepath):
    tree = ET.parse(filepath)
    root = tree.getroot()
    parent_map = {child: parent for parent in root.iter() for child in parent}

    verses = []
    current = None
    for p in root.iter("p"):
        if p.get("rend") != "bodytext":
            continue
        n = p.get("n")
        text = normalize(clean_text(p))
        if n is not None:
            current = {"n": int(n), "parent": parent_map.get(p), "texts": [text]}
            verses.append(current)
        elif current is not None:
            current["texts"].append(text)

    for v in verses:
        v["heading"] = heading_path(v["parent"], parent_map)
    return verses


def select_chunk(files, source_dir, state, chunk_size):
    idx = files.index(state["file"]) if state["file"] in files else 0
    next_verse = state["next_verse"]

    for _ in range(len(files) + 1):
        filename = files[idx]
        verses = parse_verses(os.path.join(source_dir, filename))

        start_i = next((i for i, v in enumerate(verses) if v["n"] >= next_verse), None)
        if start_i is None:
            idx = (idx + 1) % len(files)
            next_verse = 1
            continue

        chunk = [verses[start_i]]
        parent = verses[start_i]["parent"]
        i = start_i + 1
        while len(chunk) < chunk_size and i < len(verses) and verses[i]["parent"] is parent:
            chunk.append(verses[i])
            i += 1

        if i < len(verses):
            new_state = {"file": filename, "next_verse": verses[i]["n"]}
        else:
            new_idx = (idx + 1) % len(files)
            new_state = {"file": files[new_idx], "next_verse": 1}

        return filename, chunk, chunk[0]["heading"], new_state

    raise RuntimeError("No numbered verses found in any source file")


def format_pali(chunk):
    return "\n\n".join(f"[{v['n']}] {' '.join(v['texts'])}" for v in chunk)


def call_anthropic(api_key, model, filename, heading, chunk):
    system = (
        "You are an expert translator of Pali Buddhist canonical texts (the Tipitaka), "
        "working from IAST Latin-transliterated Pali. Translate the passage into clear, "
        "faithful, readable English prose. Translate strictly verse by verse: output one "
        "entry per verse, each starting with its verse number in brackets like '[12]', "
        "followed only by the English translation of that verse. Preserve proper nouns "
        "(place names, personal names) transliterated sensibly. Do not add commentary, "
        "headers, or any text beyond the verse-by-verse translations."
    )
    heading_str = " — ".join(heading) if heading else "(untitled section)"
    user_msg = f"Text: {filename}\nSection: {heading_str}\n\n{format_pali(chunk)}"

    body = json.dumps(
        {
            "model": model,
            "max_tokens": 4096,
            "system": system,
            "messages": [{"role": "user", "content": user_msg}],
        }
    ).encode("utf-8")

    req = urllib.request.Request(
        ANTHROPIC_URL,
        data=body,
        method="POST",
        headers={
            "content-type": "application/json",
            "x-api-key": api_key,
            "anthropic-version": "2023-06-01",
        },
    )
    with urllib.request.urlopen(req, timeout=120) as resp:
        result = json.loads(resp.read().decode("utf-8"))
    return "".join(block["text"] for block in result["content"] if block["type"] == "text")


def send_email(api_key, email_from, email_to, subject, text_body):
    body = json.dumps(
        {"from": email_from, "to": [email_to], "subject": subject, "text": text_body}
    ).encode("utf-8")
    req = urllib.request.Request(
        RESEND_URL,
        data=body,
        method="POST",
        headers={"content-type": "application/json", "authorization": f"Bearer {api_key}"},
    )
    with urllib.request.urlopen(req, timeout=60) as resp:
        return resp.read()


def next_run_number(translation_dir, date_str):
    pattern = re.compile(rf"^(?:pali|eng|full)-{re.escape(date_str)}-run(\d+)\.txt$")
    max_run = 0
    if os.path.isdir(translation_dir):
        for name in os.listdir(translation_dir):
            m = pattern.match(name)
            if m:
                max_run = max(max_run, int(m.group(1)))
    return max_run + 1


def write_translation_files(translation_dir, date_str, run_n, header, translation, pali_text):
    os.makedirs(translation_dir, exist_ok=True)
    suffix = f"{date_str}-run{run_n}.txt"

    with open(os.path.join(translation_dir, f"pali-{suffix}"), "w", encoding="utf-8") as f:
        f.write(f"{header}\n{pali_text}\n")

    with open(os.path.join(translation_dir, f"eng-{suffix}"), "w", encoding="utf-8") as f:
        f.write(f"{header}\n{translation}\n")

    with open(os.path.join(translation_dir, f"full-{suffix}"), "w", encoding="utf-8") as f:
        f.write(
            f"{header}\n{translation}\n\n"
            f"{'-' * 40}\nOriginal (Pali, IAST):\n\n{pali_text}\n"
        )


def send_error_report(api_key, email_from, email_to, context, exc):
    subject = f"Tipitaka job ERROR — {context}"
    body = (
        f"The daily Tipitaka translation job failed.\n\n"
        f"Context: {context}\n\n"
        f"{type(exc).__name__}: {exc}\n\n"
        f"{traceback.format_exc()}"
    )
    send_email(api_key, email_from, email_to, subject, body)


def main():
    source_dir = os.environ.get("SOURCE_DIR", "romn")
    state_path = os.environ.get("STATE_FILE", "state/translation-progress.json")
    translation_dir = os.environ.get("TRANSLATION_DIR", "translation")
    chunk_size = int(os.environ.get("CHUNK_SIZE", "20"))
    model = os.environ.get("ANTHROPIC_MODEL", "claude-sonnet-5")
    dry_run = os.environ.get("DRY_RUN") == "1"

    context = "startup"
    try:
        files = sorted(f for f in os.listdir(source_dir) if f.endswith(".mul.xml"))
        if not files:
            raise RuntimeError(f"No *.mul.xml files found in {source_dir}")

        with open(state_path, encoding="utf-8") as f:
            state = json.load(f)
        context = f"file={state.get('file')}, next_verse={state.get('next_verse')}"

        filename, chunk, heading, new_state = select_chunk(files, source_dir, state, chunk_size)
        verse_numbers = [v["n"] for v in chunk]
        verse_range = (
            str(verse_numbers[0])
            if len(verse_numbers) == 1
            else f"{verse_numbers[0]}-{verse_numbers[-1]}"
        )
        heading_str = " — ".join(heading) if heading else filename
        context = f"{filename} verses {verse_range}"

        print(f"Translating {filename} verses {verse_range} ({heading_str}), {len(chunk)} verse(s)")

        header = f"{heading_str}\n{filename}, verses {verse_range}\n"
        pali_text = format_pali(chunk)

        if dry_run:
            translation = "[DRY RUN — translation skipped]"
            print(f"{header}\n{translation}")
            return

        translation = call_anthropic(os.environ["ANTHROPIC_API_KEY"], model, filename, heading, chunk)

        subject = f"Tipitaka reading: {filename} verses {verse_range} — {heading_str}"
        send_email(
            os.environ["RESEND_API_KEY"],
            os.environ["EMAIL_FROM"],
            os.environ["EMAIL_TO"],
            subject,
            f"{header}\n{translation}",
        )

        date_str = datetime.now(timezone.utc).date().isoformat()
        run_n = next_run_number(translation_dir, date_str)
        write_translation_files(translation_dir, date_str, run_n, header, translation, pali_text)

        with open(state_path, "w", encoding="utf-8") as f:
            json.dump(new_state, f, indent=2)
            f.write("\n")

        print(f"State updated: {new_state}")

    except Exception as exc:
        print(f"ERROR during {context}: {exc}", file=sys.stderr)
        traceback.print_exc()
        if not dry_run:
            try:
                send_error_report(
                    os.environ["RESEND_API_KEY"],
                    os.environ["EMAIL_FROM"],
                    os.environ["EMAIL_TO"],
                    context,
                    exc,
                )
            except Exception as report_exc:
                print(f"Additionally failed to send error report: {report_exc}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
