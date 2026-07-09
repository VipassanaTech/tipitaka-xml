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

### Don't *substitute* `aa`→`ā` — *expand* it

The naive fix (rewrite every `aa` to `ā`) is **wrong**, and breaks real
searches. A typed `aa` is genuinely ambiguous in Pali romanisation:

  * usually it's the long vowel `ā` (`sotaapatti` for *sotāpatti*), but
  * sometimes it's a true **vowel hiatus** — two separate short `a` across a
    morpheme/word boundary — which must stay `aa`.

Forcing `aa`→`ā` makes the hiatus words unfindable (a bug seen in the wild on
tipitakapali.org: you have to switch to Devanagari to search them). So the
prototypes **expand instead of collapse** — a query with `aa` matches *both*
readings. [`roman.py`](flexibility-elasticsearch/app/roman.py) (identical in both
stacks) reproduces the Solr diacritic fold and adds this expansion:

| Input | romn clauses searched (OR-ed) | other 14 scripts (single, all-long) |
|-------|-------------------------------|-------------------------------------|
| `sotaapatti*` | `sotaapatti*` **or** `sotapatti*` | from `sotāpatti*` |
| `sotāpatti*`  | `sotapatti*` | from `sotāpatti*` |
| `sotapatti*`  | `sotapatti*` | from `sotapatti*` |
| `na-aagamma`  | `na-aagamma` **or** `na-agamma` | from `na-āgamma` |

So `sotaapatti` still finds `sotāpatti`, **and** a genuine `…aa…` hiatus word
stays findable via its literal clause — nothing is forced into one reading.
`fold()` only strips diacritics (it does **not** collapse `aa`), so the index
keeps the `ā` vs `aa` distinction; in ES that fold is exactly what `icu_folding`
already does to `text_romn`, while Typesense gets a dedicated `romn_fold` field
because it doesn't fold macrons reliably on its own. The other 14 scripts get a
single transliteration of the conventional all-long reading (to bound fan-out);
the romn field — the one the user typed in — is where the both-readings match
lives.

Expansion is the default, but a **`literal=true`** query param (and a "strict
vowels (aa ≠ ā)" checkbox in the UI) turns it off for power users: in strict
mode a typed `aa` matches only `aa`, never `ā`, so you can pin an exact hiatus
spelling without the long-vowel word bleeding in.

## Side-by-side

| | **Solr (current)** | **Elasticsearch** | **Typesense** |
|---|---|---|---|
| Hit unit | whole file | paragraph `<p>` | paragraph `<p>` |
| Searchable text | 1 field (`text`) | 15 per-script fields | 15 + `romn_fold` |
| Type in any script | one at a time | fan-out to 15 | fan-out to 15 |
| Roman diacritic-insensitive | ✓ (FoldToASCII + removeDiacritcals) | ✓ (icu_folding) | ✓ (`romn_fold`) |
| `sotaapatti` finds `sotāpatti` | ✗ | ✓ (hiatus-safe) | ✓ (hiatus-safe) |
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
- **Added:** cross-script fan-out, fuzzy, dual-script output, the ambiguous-`aa`
  expansion (long `ā` *and* hiatus `aa`, never forced one way), pagination.
- **Not yet ported:** the `volume/pitaka/book/chapter/section` TOC metadata and
  the `volume` facet. Production derives these from a tipitaka.org TOC visitor
  (`TipitakaOrgTocVisitor`); the prototypes only carry `book`, `p_idx`, `rend`.
  Worth adding before launch if facets matter — it's metadata plumbing, not a
  search-engine question.

## Precision: why "exact" must not fan out (the `tene` case)

Searching `tene` should return the ~19 whole-word occurrences (CST 4.1 shows 8
over its smaller book set), **not** the 2,890 `teneva`, 1,083 `tenevaha`, … —
4,722 `tene*` tokens in all — which are *different words* (`tena`+`eva` sandhi).

On the Roman field, exact-token matching (ES `match_phrase`, Typesense
`num_typos=0, prefix=false`) already returns only the 19 — `teneva` is a
distinct token and cannot match. The noise came entirely from the **15-way
cross-script fan-out**: scripts with no word spaces (Thai, Khmer, Myanmar,
Tibetan) are segmented by ICU into *syllables*, so an "exact phrase" there
degrades into a syllable-*subsequence* match — the Thai rendering of `te-ne`
matches inside `te-ne-va`. OR-ed across all scripts, the least-precise script
wins and floods the results, even for a Roman query displayed in Devanagari.

Fix: **exact mode searches only the input script.** Roman and Devanagari are
space-delimited, so single-script `match_phrase` is a true whole-word hit and
`tene` returns the 19. Fuzzy and wildcard keep the fan-out (they're recall-
first by design). Truly spaceless input scripts can't do whole-word exact
without a segmentation dictionary — an inherent limit, same as everywhere else.

## Running the original arm

The production **Lucene index is committed** at `original-solr/solr/data/`
(~523 MB), so the Solr container serves real data with no re-indexing — it just
builds the WAR (incl. the `roman` QParser) and runs it via webapp-runner, the
recipe from [readme.md](original-solr/readme.md). See `docker-compose.all.yml`
(`--profile solr`). It's the heaviest arm (old Solr 3.4 + JVM) and is gated
behind a profile so the two candidates come up without it.
