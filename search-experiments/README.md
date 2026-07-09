# Tipiṭaka multi-script search experiments

Two self-hosted, Dockerised prototypes for replacing the current
`search.tipitaka.org/solr/web` service with one that:

- accepts a query in **any of the 15 scripts** (deva / romn / beng / mymr /
  sinh / thai / cyrl / gujr / guru / khmr / knda / mlym / taml / telu / tibt),
- searches the **native scripts directly** (no canonicalisation — the index
  keeps each script's text in its own field),
- returns each hit in **two scripts** (the one the user typed in + the one
  their UI is set to),
- supports **exact, wildcard, and fuzzy** modes.

See **[STRATEGY.md](STRATEGY.md)** for the full evaluation of seven options
and why these two were picked, and **[COMPARISON.md](COMPARISON.md)** for how
both prototypes line up against the current production Solr service (now
vendored under [`original-solr/`](original-solr/)).

## The two prototypes

| Folder                                                              | Pick for     | Engine          | RAM   | $/mo (typical VPS) |
|---------------------------------------------------------------------|--------------|-----------------|-------|---------------------|
| [`flexibility-elasticsearch/`](flexibility-elasticsearch/README.md) | Flexibility  | Elasticsearch 8 + ICU | 2–3 GB | $24–48 |
| [`cost-typesense/`](cost-typesense/README.md)                       | Cost         | Typesense       | 0.5 GB | **$5–10** |

Both use the **same multi-script pattern**:

```
user query ─► Aksharamukha sidecar ─► 15 transliterated query strings
                                       │
                                       ▼
                          search engine matches each against its own
                          per-script field (text_deva, text_romn, …)
                                       │
                                       ▼
                          merge / score / highlight in (input, ui) scripts
```

The transliteration sidecar code is identical between the two — engine choice
is orthogonal to language handling.

## Quick start

Pick one of the two stacks and:

```bash
cd search-experiments/flexibility-elasticsearch    # or cost-typesense
docker compose up --build

# In another terminal, smoke-test with the first 5 books:
curl -X POST 'http://localhost:8000/index?limit=5'

# Then open the toy UI:
open http://localhost:8000
```

Both stacks expose port 8000 for the search API and serve a small HTML
search box at `/` (with **pagination** — Prev/Next, page size, "X–Y of N").
Each result's script tags are **clickable deep links to tipitaka.org**
(`https://tipitaka.org/<script>/#<node>`), so you can jump from a hit to the
book on the live site; the book→node map lives in `app/links.py` (generated
from the site's `tree.json`, ids are shared across all 15 script trees).
Each container mounts the repo root read-only at `/corpus`, so the indexer
reads `deva/`, `romn/`, … directly from the working tree.

## Run all three side by side (for reviewers)

To host Elasticsearch, Typesense, **and** the original Solr in parallel on one
VM with a landing page that links to each:

```bash
cd search-experiments
docker compose -f docker-compose.all.yml up --build -d                 # ES + Typesense + landing
docker compose -f docker-compose.all.yml --profile solr up --build -d  # also the Solr baseline

curl -X POST localhost:8001/index    # index Elasticsearch (Solr is pre-indexed)
curl -X POST localhost:8002/index    # index Typesense
open http://localhost:8080           # landing page → all three
```

Ports: **8080** landing · **8001** ES UI · **8002** Typesense UI ·
**8983** Solr (`/solr/web`). The Solr arm serves the committed production
index (`original-solr/solr/data/`, ~523 MB) with no re-indexing. On AWS, open
those ports in the security group; the landing-page links resolve against the
VM's own hostname automatically.

## Sample queries to try

| Query string         | Mode      | What it should hit |
|----------------------|-----------|--------------------|
| `vipassana`          | fuzzy     | All `vipassanā`-containing passages, regardless of UI script. |
| `vipassanā`          | exact     | Diacritic-exact Roman match. |
| `विपस्सना`            | exact     | Same passages, queried in Devanagari. |
| `ৱিপস্সনা`            | exact     | Same passages, queried in Bengali. |
| `dhammacakka*`       | wildcard  | All forms starting with that prefix. |
| `dhamacakka`         | fuzzy     | Should still match `dhammacakka` (1 typo). |
| `sotaapatti*`        | wildcard  | ASCII doubled-vowel form — matches `sotāpatti…`. |
| `sotāpatti*`         | wildcard  | Macron form — matches the **same** passages. |

The last two rows are the production-Solr gap that reading the original code
exposed: today only the macron form matches. Both prototypes now make
`sotaapatti` find `sotāpatti` — **without** force-rewriting `aa`→`ā`, which
would hide genuine vowel-hiatus words (two separate `a`s). A typed `aa` is
treated as ambiguous and matches *both* readings by default; a **"strict vowels
(aa ≠ ā)"** checkbox (API: `&literal=true`) turns expansion off when you want an
exact hiatus spelling. See [COMPARISON.md](COMPARISON.md) and `app/roman.py`.

## Stretch: semantic search

Both engines have a path to add this without a second cluster:

- Elasticsearch: add a `dense_vector` field, embed each paragraph with a
  multilingual model (e.g. `intfloat/multilingual-e5-large`), use
  `knn` query alongside the existing `bool/should`.
- Typesense: built-in hybrid search since v0.25.

Estimated additional cost: GPU-less embed run is a one-shot ~2–4 hour job
on a single CPU; query-time vector search adds ~30–50 ms latency at
this corpus size.
