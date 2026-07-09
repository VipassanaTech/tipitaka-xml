"""FastAPI sidecar over Typesense — same query-fan-out pattern as the ES build."""
from __future__ import annotations

import os
from typing import Literal

import typesense
from fastapi import FastAPI, HTTPException, Query
from fastapi.responses import HTMLResponse

from indexer import COLLECTION, reindex
from translit import ALL_SCRIPTS, detect_script, fan_out
from ui import _render_ui

# Roman is searched against a dedicated diacritic-folded field; every other
# script searches (and displays from) its own native field.
def _search_field(script: str) -> str:
    return "romn_fold" if script == "romn" else f"text_{script}"

TS_HOST = os.environ.get("TYPESENSE_HOST", "typesense")
TS_PORT = int(os.environ.get("TYPESENSE_PORT", "8108"))
TS_KEY = os.environ.get("TYPESENSE_API_KEY", "devkey")

client = typesense.Client({
    "nodes": [{"host": TS_HOST, "port": TS_PORT, "protocol": "http"}],
    "api_key": TS_KEY,
    "connection_timeout_seconds": 30,
})

app = FastAPI(title="Tipitaka multi-script search (Typesense)")


@app.get("/health")
def health() -> dict:
    try:
        client.collections[COLLECTION].retrieve()
        exists = True
    except typesense.exceptions.ObjectNotFound:
        exists = False
    return {"typesense": True, "collection_exists": exists}


@app.post("/index")
def post_index(limit: int | None = Query(None, description="Index only the first N books")) -> dict:
    return reindex(client, limit=limit)


# Typesense paginates each sub-query independently, but we merge 15 of them
# client-side, so a "global" page N can't be asked for directly. We instead
# pull a window deep enough to cover the requested page from every sub-query,
# merge, then slice. 250 is Typesense's per_page ceiling, so pagination is
# exact up to 250 merged results deep — plenty for a feedback prototype.
_TS_MAX_WINDOW = 250


@app.get("/search")
def search(
    q: str = Query(..., min_length=1),
    input_script: str | None = Query(None),
    ui_script: str = Query("deva"),
    mode: Literal["exact", "wildcard", "fuzzy"] = Query("fuzzy"),
    literal: bool = Query(False, description="Strict vowels: match aa/ii/uu literally, don't also match ā/ī/ū"),
    page: int = Query(1, ge=1, description="1-based page number"),
    per_page: int = Query(20, ge=1, le=100, description="Results per page"),
) -> dict:
    if input_script and input_script not in ALL_SCRIPTS:
        raise HTTPException(400, f"Unknown input_script {input_script!r}")
    if ui_script not in ALL_SCRIPTS:
        raise HTTPException(400, f"Unknown ui_script {ui_script!r}")
    try:
        client.collections[COLLECTION].retrieve()
    except typesense.exceptions.ObjectNotFound:
        raise HTTPException(409, "Collection not built yet — run: curl -X POST .../index")
    except typesense.exceptions.ServiceUnavailable:
        raise HTTPException(503, "Typesense not ready (still starting or recovering). Retry shortly.")

    src = input_script or detect_script(q)
    expanded = fan_out(q, src=src, expand=not literal)

    window = min(page * per_page, _TS_MAX_WINDOW)

    # Typesense does multi-search natively: one HTTP call, N independent
    # queries, merged client-side. Each per-script query searches its own field
    # (romn searches the folded field) with the script-specific query string.
    # One sub-query per (script, candidate spelling); Roman contributes several
    # (the ambiguous-aa expansion from roman.py). `query_scripts` stays aligned
    # with `queries` so the merge knows which script each result came from.
    #
    # EXACT is precision-first: search ONLY the input script. The 15-way OR
    # otherwise pulls in matches via other scripts and destroys precision
    # (e.g. `tene` surfacing `teneva`). Roman/Devanagari are space-delimited so
    # a single-script exact-token search is a true whole-word hit. Fuzzy/
    # wildcard keep the cross-script fan-out for recall.
    search_scripts = [src] if mode == "exact" else list(expanded.keys())
    queries: list[dict] = []
    query_scripts: list[str] = []
    for script in search_scripts:
        field = _search_field(script)
        for q_str in expanded[script]:
            per_query = {
                "collection": COLLECTION,
                "q": q_str,
                "query_by": field,
                # Always fetch the (input, ui) display fields, regardless of
                # which script this sub-query searched, so the winning sub-query
                # still carries the text we render in both rows.
                "include_fields": f"id,book,p_idx,rend,text_{src},text_{ui_script}",
                "highlight_fields": field,
                "highlight_full_fields": field,
                "per_page": window,
                "page": 1,
            }
            if mode == "exact":
                per_query["num_typos"] = 0
                per_query["prefix"] = False
            elif mode == "wildcard":
                per_query["num_typos"] = 0
                per_query["prefix"] = True   # Typesense's idiom for trailing-wildcard
                per_query["q"] = q_str.replace("*", "")
            else:  # fuzzy
                per_query["num_typos"] = 2
                per_query["prefix"] = True
            queries.append(per_query)
            query_scripts.append(script)

    res = client.multi_search.perform({"searches": queries}, {})

    # Merge: dedupe by id, keep best text_match score across the per-script results.
    # Each doc may surface in several sub-queries. Keep one entry per doc:
    # take the best score, but accumulate the two display highlights from
    # whichever sub-query actually searched the input / UI script. A highlight
    # from a different script (e.g. matched via deva while we're rendering the
    # romn row) is NOT borrowed — that row just shows its plain field text.
    merged: dict[str, dict] = {}
    src_display = f"text_{src}"
    ui_field = f"text_{ui_script}"
    for sub, script in zip(res["results"], query_scripts):
        search_field = _search_field(script)
        for h in sub.get("hits", []):
            doc = h["document"]
            doc_id = doc["id"]
            score = h.get("text_match", 0)
            hl = {item.get("field"): item for item in h.get("highlights", [])}
            snippet = (hl.get(search_field) or {}).get("snippet")

            entry = merged.get(doc_id)
            if entry is None:
                entry = {
                    "id": doc_id,
                    "_score": score,
                    "_matched_via_script": script,
                    "book": doc.get("book"),
                    "p_idx": doc.get("p_idx"),
                    "rend": doc.get("rend"),
                    "input_script_text": doc.get(src_display, ""),
                    "ui_script_text": doc.get(ui_field, ""),
                    "input_script_highlight": None,
                    "ui_script_highlight": None,
                }
                merged[doc_id] = entry
            elif score > entry["_score"]:
                entry["_score"] = score
                entry["_matched_via_script"] = script

            if snippet and script == src and not entry["input_script_highlight"]:
                entry["input_script_highlight"] = snippet
            if snippet and script == ui_script and not entry["ui_script_highlight"]:
                entry["ui_script_highlight"] = snippet

    ordered = sorted(merged.values(), key=lambda d: d["_score"], reverse=True)
    total = len(ordered)                 # approximate: capped at the merge window
    start = (page - 1) * per_page
    hits = ordered[start:start + per_page]
    return {
        "query": q,
        "detected_script": src,
        "ui_script": ui_script,
        "mode": mode,
        "literal": literal,
        "expanded_queries": expanded,
        "total": total,
        "page": page,
        "per_page": per_page,
        "total_pages": (total + per_page - 1) // per_page,
        "total_is_capped": total >= _TS_MAX_WINDOW,
        "hits": hits,
    }


@app.get("/", response_class=HTMLResponse)
def home() -> str:
    return _render_ui("Typesense", "#0d9488")
