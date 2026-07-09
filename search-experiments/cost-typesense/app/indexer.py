"""Index TEI XML paragraphs into Typesense, one field per script."""
from __future__ import annotations

import os
import time
from pathlib import Path
import typesense
from typesense.exceptions import ServiceUnavailable

from roman import fold
from translit import ALL_SCRIPTS
from xml_parser import list_books, parse_paragraphs

COLLECTION = os.environ.get("COLLECTION", "tipitaka")
CORPUS_ROOT = Path(os.environ.get("CORPUS_ROOT", "/corpus"))


def wait_until_ready(client: typesense.Client, attempts: int = 30, delay: float = 2.0) -> None:
    """Block until Typesense accepts operations.

    Right after startup the node can still be replaying its write-ahead log and
    answers writes with HTTP 503 "Not Ready or Lagging" — even while /health
    reports ok. Poll a cheap read until it stops 503-ing (or give up).
    """
    last: Exception | None = None
    for _ in range(attempts):
        try:
            client.collections.retrieve()   # cheap, exercises the write-path readiness
            return
        except ServiceUnavailable as e:
            last = e
            time.sleep(delay)
    if last is not None:
        raise last


def schema() -> dict:
    fields: list[dict] = [
        {"name": "book", "type": "string", "facet": True},
        {"name": "p_idx", "type": "int32"},
        {"name": "rend", "type": "string", "facet": True},
    ]
    for s in ALL_SCRIPTS:
        # 'string' (not 'string[]') with index:true; locale='' lets Typesense's
        # built-in unicode handling kick in. We rely on Aksharamukha for the
        # cross-script bit, not on Typesense locales.
        fields.append({"name": f"text_{s}", "type": "string", "index": True, "optional": True, "locale": ""})
    # Dedicated diacritic-folded search field for Roman. Typesense doesn't fold
    # ā→a / ṃ→m reliably on its own, so we search this and display text_romn.
    # Lets `sotaapatti`, `sotāpatti`, `sotapatti` all match. See roman.py.
    fields.append({"name": "romn_fold", "type": "string", "index": True, "optional": True, "locale": ""})
    return {
        "name": COLLECTION,
        "fields": fields,
        "default_sorting_field": "p_idx",
    }


def ensure_collection(client: typesense.Client, recreate: bool = False) -> None:
    if recreate:
        try:
            client.collections[COLLECTION].delete()
        except typesense.exceptions.ObjectNotFound:
            pass
    try:
        client.collections[COLLECTION].retrieve()
    except typesense.exceptions.ObjectNotFound:
        client.collections.create(schema())


def _docs(limit: int | None):
    canonical = CORPUS_ROOT / "romn"
    for book in list_books(canonical, limit=limit):
        per_script: dict[str, list] = {}
        for s in ALL_SCRIPTS:
            path = CORPUS_ROOT / s / f"{book}.xml"
            per_script[s] = parse_paragraphs(path) if path.exists() else []

        n = max((len(ps) for ps in per_script.values()), default=0)
        for i in range(n):
            doc: dict = {
                "id": f"{book}__p{i}",   # Typesense doesn't allow '#' in ids
                "book": book,
                "p_idx": i,
                "rend": "",
            }
            for s in ALL_SCRIPTS:
                ps = per_script[s]
                if i < len(ps):
                    doc[f"text_{s}"] = ps[i].text
                    if s == "romn":
                        doc["romn_fold"] = fold(ps[i].text)
                    if not doc["rend"]:
                        doc["rend"] = ps[i].rend
            yield doc


def reindex(client: typesense.Client, limit: int | None = None) -> dict:
    wait_until_ready(client)            # tolerate a node still catching up at startup
    ensure_collection(client, recreate=True)
    batch: list[dict] = []
    total = 0
    errors = 0
    for d in _docs(limit):
        batch.append(d)
        if len(batch) >= 200:
            res = client.collections[COLLECTION].documents.import_(batch, {"action": "upsert"})
            total += sum(1 for r in res if r.get("success"))
            errors += sum(1 for r in res if not r.get("success"))
            batch = []
    if batch:
        res = client.collections[COLLECTION].documents.import_(batch, {"action": "upsert"})
        total += sum(1 for r in res if r.get("success"))
        errors += sum(1 for r in res if not r.get("success"))
    return {"indexed": total, "errors": errors}
