# Original Solr vs. the two prototypes

We got the production Solr source (`original-solr/`) after the first cut of
these experiments. This is what it actually does, where the prototypes already
match it, and what they change — plus the concrete enhancements that reading the
Solr code drove.

## How the production Solr service actually works

### Document granularity — one doc per *file*, not per paragraph
`TextXmlDoc` / `TextXmlDocFactory` read each script's XML and concatenate
**every text node in the whole file into a single `text` field**
([TextXmlDocFactory.java](original-solr/src/main/java/org/tipitaka/search/solr/TextXmlDocFactory.java)).
So a Solr "hit" is an entire book/file. Metadata fields: `script`, `volume`,
`pitaka`, `book`, `chapter`, `section`, `path`, `id` (= `{script}-{stem}`).
Highlighting is one fragment out of a multi-megabyte field.

### One analyzed field, folded to ASCII at index time
The `text` fieldType ([schema.xml](original-solr/solr/conf/schema.xml)) is:

```
index:  MappingCharFilter(mapping-FoldToASCII.txt) → Whitespace → WordDelimiter → LowerCase
query:  Whitespace → WordDelimiter → LowerCase          (no char filter here)
```

`mapping-FoldToASCII.txt` folds Latin-with-diacritics to bare ASCII
(`ā`→`a`, `ī`→`i`, `ṃ`→`m`, `ṭ`→`t`, …). It only touches the Latin blocks, so
Devanagari/Thai/etc. pass through untouched and are effectively whitespace-
tokenised. **There is no per-script linguistic handling** — Roman is the script
that gets the clever folding.

### The custom `roman` query parser closes the loop
The query analyzer has *no* fold char-filter, so the folding is applied to the
query a different way — a custom QParser registered as `defType=roman`:

```java
// RomanExtendedDismaxQParserPlugin.java
super.createParser(RomanScriptHelper.removeDiacritcals(qstr).toLowerCase(), …)
```

`removeDiacritcals` ([RomanScriptHelper.java](original-solr/src/main/java/org/tipitaka/search/RomanScriptHelper.java))
strips `ṇḍḷūñṭṃṅāī` → ASCII. Net effect: **Roman search is diacritic-
insensitive**. `sotāpatti*` → `sotapatti*` matches the indexed `sotapatti`.

### The `/web` request handler
[solrconfig.xml](original-solr/solr/conf/solrconfig.xml) `/web`: `defType=roman`,
`q.alt=*:*`, `rows=10`, `qf=text^0.5 …`, Velocity UI (`template-web`/`layout-web`),
highlighting on `text` (fragsize 100), facet on `volume`. eDismax gives
wildcards; **there is no fuzzy/typo tolerance** and **no cross-script search** —
you search within whatever script you're looking at.

## The gap that drove the enhancement

The constraint **"`sotaapatti*` and `sotāpatti*` should both work"** is *not*
satisfied today. `removeDiacritcals` handles the macron (`sotāpatti`→`sotapatti`)
but does nothing with the ASCII **doubled-vowel** convention people type when
they lack diacritics (`sotaapatti`). That query stays `sotaapatti*` and misses
the indexed `sotapatti`.

So the experiments now ship [`roman.py`](flexibility-elasticsearch/app/roman.py)
(identical in both stacks), which reproduces the Solr folding **and** adds the
doubled-vowel (and Velthuis `.t .d .n .m "n ~n`) equivalence:

| Input | `to_iast()` (for transliteration) | `fold()` (the romn search term) |
|-------|-----------------------------------|---------------------------------|
| `sotaapatti*` | `sotāpatti*` | `sotapatti*` |
| `sotāpatti*`  | `sotāpatti*` | `sotapatti*` |
| `sotapatti*`  | `sotapatti*` | `sotapatti*` |

All three collapse to one indexed form. `to_iast` feeds Aksharamukha canonical
IAST (so `aa`→`ā`, not two `a`s, when transliterating to the other 14 scripts);
`fold` produces the diacritic-folded ASCII the Roman field is matched on. In ES
that fold is exactly what `icu_folding` already does to the indexed `text_romn`,
so query and index meet in the middle; Typesense gets a dedicated `romn_fold`
field because it doesn't fold macrons reliably on its own.

## Side-by-side

| | **Solr (current)** | **Elasticsearch** | **Typesense** |
|---|---|---|---|
| Hit unit | whole file | paragraph `<p>` | paragraph `<p>` |
| Searchable text | 1 field (`text`) | 15 per-script fields | 15 + `romn_fold` |
| Type in any script | one at a time | fan-out to 15 | fan-out to 15 |
| Roman diacritic-insensitive | ✓ (FoldToASCII + removeDiacritcals) | ✓ (icu_folding) | ✓ (`romn_fold`) |
| `sotaapatti*` ≡ `sotāpatti*` | ✗ | ✓ | ✓ |
| Wildcard | ✓ eDismax | ✓ `wildcard` | prefix only |
| Fuzzy / typo | ✗ | ✓ AUTO Damerau | ✓ `num_typos` |
| Result in 2 scripts | ✗ | ✓ highlight per field | ✓ |
| Facets | volume | — (easy to add) | book/rend facets defined |
| Scoring | eDismax `qf` boosts | BM25 `bool/should` | `text_match` merged |
| RAM | 4–8 GB JVM | 2–3 GB JVM | ~0.5 GB |
| Hosting | $24–48/mo | $24–48/mo | $5–10/mo |

## What the prototypes deliberately keep / drop

- **Kept:** Roman diacritic-insensitivity (now provably matching the Solr fold,
  see `roman.py` vs `mapping-FoldToASCII.txt`); UTF-16 TEI parsing; the
  paragraph as the natural unit (an *improvement* — Solr's file-level hits make
  highlighting almost useless).
- **Added:** cross-script fan-out, fuzzy, dual-script output, the doubled-vowel
  equivalence, pagination.
- **Not yet ported:** the `volume/pitaka/book/chapter/section` TOC metadata and
  the `volume` facet. Production derives these from a tipitaka.org TOC visitor
  (`TipitakaOrgTocVisitor`); the prototypes only carry `book`, `p_idx`, `rend`.
  Worth adding before launch if facets matter — it's metadata plumbing, not a
  search-engine question.

## Running the original arm

The production **Lucene index is committed** at `original-solr/solr/data/`
(~523 MB), so the Solr container serves real data with no re-indexing — it just
builds the WAR (incl. the `roman` QParser) and runs it via webapp-runner, the
recipe from [readme.md](original-solr/readme.md). See `docker-compose.all.yml`
(`--profile solr`). It's the heaviest arm (old Solr 3.4 + JVM) and is gated
behind a profile so the two candidates come up without it.
