# Adding `text_exact` Field to the Solr Schema

This document describes the steps required to enable exact-match search in the Tipitaka Solr index.

---

## Background

The search UI has a **Search Type** toggle (Fuzzy / Exact). Fuzzy is the existing behaviour. When a user selects **Exact**, the frontend submits the query as:

```
q=text_exact:"dhamma vinaya"
```

This requires a `text_exact` field to exist in the Solr index. The field uses the `text_ws` fieldType which is already defined in `solr_schema.xml` — it tokenises on whitespace only, with no stemming, no ASCII folding, and no WordDelimiter splitting.

---

## Step 1 — Edit the Schema File

Open `solr_schema.xml` and make **two additions**.

### 1a — Add the field definition

Inside the `<fields>` block, after the existing `text` field, add:

```xml
<field name="text" type="text" indexed="true" stored="true" multiValued="true"/>

<!-- Exact match field: whitespace-only tokenisation, no WordDelimiter splitting -->
<field name="text_exact" type="text_ws" indexed="true" stored="false" multiValued="true"/>
```

> `stored="false"` saves disk space — the field is only needed for searching, not for retrieval.

### 1b — Add the copyField directive

In the `<copyField>` section near the bottom of the file, add one new line:

```xml
<copyField source="text" dest="text"/>
<copyField source="pitaka" dest="text"/>
<copyField source="book" dest="text"/>
<copyField source="chapter" dest="text"/>
<copyField source="section" dest="text"/>

<!-- Copy main text into the exact match field -->
<copyField source="text" dest="text_exact"/>
```

---

## Step 2 — Deploy the Updated Schema

Copy the updated file to Solr's `conf/` directory for your collection, renaming it `schema.xml`:

```
$SOLR_HOME/server/solr/<collection-name>/conf/schema.xml
```

For older Solr setups the path may be:

```
$SOLR_HOME/solr/<collection-name>/conf/schema.xml
```

---

## Step 3 — Restart Solr

For standalone Solr (static schema mode), restart the server to pick up the schema change:

```bash
bin/solr restart
```

> If you are running **SolrCloud** or using the **Schema API**, schema changes are applied via HTTP POST to the API and do not require a restart.

---

## Step 4 — Reindex All Documents

The `text_exact` field is only populated for documents indexed **after** the schema change. All existing documents must be re-ingested by re-running the indexing pipeline against Solr.

---

## Step 5 — Verify the Field is Working

After reindexing, test the field via curl or the Solr Admin UI:

```bash
curl "http://localhost:8983/solr/<collection>/select?q=text_exact:%22dhamma%20vinaya%22&wt=json&indent=true"
```

If results are returned, the field is working correctly.

**If you get zero results, check:**
- Documents were reindexed *after* the schema change, not before.
- The search phrase matches the exact whitespace-separated tokens as they appear in the source text (see note on case sensitivity below).

---

## How `text_ws` Differs from `text`

| Feature | `text` (Fuzzy) | `text_ws` (Exact) |
|---|---|---|
| Tokeniser | WhitespaceTokenizer | WhitespaceTokenizer |
| WordDelimiter | Yes — splits `dhamma-kāya` into multiple tokens | No — kept as one token |
| ASCII folding | Yes — `ā→a`, `ī→i` at index time | No |
| Lowercase | Yes | No |
| Result | Broad, forgiving matches | Matches exact whitespace-separated sequence only |

> Because `text_ws` performs no lowercasing, exact searches are **case-sensitive**. A search for `dhamma` will not match `Dhamma`. If case-insensitive exact matching is required, add a `LowerCaseFilterFactory` to the `text_ws` analyser definition in the schema.
