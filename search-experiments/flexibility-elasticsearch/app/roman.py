"""Pali Roman (IAST) normalization shared by the indexer and the query path.

This mirrors what the production Solr stack does for Roman text:

  * At **index** time Solr applies ``mapping-FoldToASCII.txt`` (ā→a, ī→i, ṃ→m,
    ṭ→t, …) so the ``text`` field is stored diacritic-folded.
  * At **query** time the custom ``roman`` QParser runs
    ``RomanScriptHelper.removeDiacritcals()`` + ``toLowerCase()``.

Net effect: Roman search is **diacritic-insensitive** (``sotāpatti`` ==
``sotapatti``).

### The ``aa`` problem (vowel hiatus)

A typed ``aa`` is genuinely **ambiguous** in Pali romanisation:

  * usually it's the long vowel ``ā`` (people type ``sotaapatti`` for
    *sotāpatti* when they have no macron key), but
  * sometimes it's a real **vowel hiatus** — two separate short ``a`` across a
    morpheme/word boundary (e.g. ``-a`` + ``a-``) — which must stay ``aa``.

So we must NOT force ``aa → ā``. Doing that makes hiatus words unfindable
(you'd have to switch scripts to search them — a real bug seen on
tipitakapali.org). Instead we **expand**: a query containing ``aa`` matches
*both* the long reading (``ā``) and the literal reading (``aa``). Recall goes
up, nothing becomes unsearchable, and the index keeps the ``ā`` vs ``aa``
distinction intact.

Functions:

``fold(text)``
    Diacritic-fold to ASCII (ā→a, ṃ→m, …). Does **not** collapse doubled
    vowels, so ``ā`` and ``aa`` stay distinct in the index. Equals what
    Elasticsearch's ``icu_folding`` produces for the indexed ``text_romn``.

``iast_variants(raw)``
    Canonical IAST candidate spellings for a query, branching each ambiguous
    ASCII long-vowel digraph (``aa``/``ii``/``uu``) into {literal, long}.
    Bounded. Feeds ``fold()`` (the romn search clauses, OR-ed together).

``long_reading(raw)``
    The single conventional all-long IAST spelling (``aa`` → ``ā`` …), used to
    transliterate to the other 14 scripts (one form each, to bound fan-out).
"""
from __future__ import annotations

import unicodedata

# Unambiguous Velthuis consonant digraphs → IAST (vowels handled separately).
_VELTHUIS_CONS: list[tuple[str, str]] = [
    (".t", "ṭ"), (".d", "ḍ"), (".n", "ṇ"), (".m", "ṃ"), (".l", "ḷ"), (".h", "ḥ"),
    ('"n', "ṅ"), ("~n", "ñ"),
]

# Ambiguous ASCII long-vowel digraphs: long ā/ī/ū OR genuine hiatus.
_LONG_VOWELS: list[tuple[str, str]] = [("aa", "ā"), ("ii", "ī"), ("uu", "ū")]

# IAST diacritic letter → ASCII fold. Superset of removeDiacritcals (adds ḥ/ṁ).
_FOLD: dict[str, str] = {
    "ā": "a", "ī": "i", "ū": "u",
    "ṃ": "m", "ṁ": "m", "ṅ": "n", "ñ": "n", "ṇ": "n",
    "ṭ": "t", "ḍ": "d", "ḷ": "l", "ḥ": "h",
}

# Cap on how many spellings one query expands to (keeps engine fan-out sane).
_MAX_VARIANTS = 4


def fold(text: str) -> str:
    """Diacritic-fold to ASCII. Preserves the ā-vs-aa (hiatus) distinction."""
    s = unicodedata.normalize("NFC", text).lower()
    s = "".join(_FOLD.get(c, c) for c in s)
    return "".join(
        c for c in unicodedata.normalize("NFD", s)
        if unicodedata.category(c) != "Mn"
    )


def _apply_velthuis_cons(s: str) -> str:
    for ascii_form, iast in _VELTHUIS_CONS:
        s = s.replace(ascii_form, iast)
    return s


def long_reading(raw: str) -> str:
    """Conventional all-long IAST spelling (aa→ā, ii→ī, uu→ū)."""
    s = _apply_velthuis_cons(unicodedata.normalize("NFC", raw).lower())
    for ascii_form, iast in _LONG_VOWELS:
        s = s.replace(ascii_form, iast)
    return s


def literal_reading(raw: str) -> str:
    """Literal IAST spelling: doubled vowels stay as written (no aa→ā).

    Used by the strict/literal toggle — `aa` is matched only as two short `a`s,
    never the long `ā`. The unambiguous Velthuis consonants are still applied.
    """
    return _apply_velthuis_cons(unicodedata.normalize("NFC", raw).lower())


def _dedupe(xs: list[str]) -> list[str]:
    seen: set[str] = set()
    out: list[str] = []
    for x in xs:
        if x not in seen:
            seen.add(x)
            out.append(x)
    return out


def iast_variants(raw: str) -> list[str]:
    """IAST candidate spellings, expanding each ambiguous aa/ii/uu to {literal, long}."""
    s = _apply_velthuis_cons(unicodedata.normalize("NFC", raw).lower())

    # Split into segments; ambiguous long-vowel digraphs carry two options.
    segments: list[tuple[str, str] | str] = []
    i = 0
    while i < len(s):
        two = s[i:i + 2]
        long = next((lng for dig, lng in _LONG_VOWELS if dig == two), None)
        if long is not None:
            segments.append((two, long))   # (literal, long)
            i += 2
        else:
            segments.append(s[i])
            i += 1

    choice_points = [seg for seg in segments if isinstance(seg, tuple)]
    if not choice_points:
        return [s]

    # Too many doubled vowels → just take the two endpoints (all-literal, all-long).
    if 2 ** len(choice_points) > _MAX_VARIANTS:
        literal = "".join(seg[0] if isinstance(seg, tuple) else seg for seg in segments)
        longish = "".join(seg[1] if isinstance(seg, tuple) else seg for seg in segments)
        return _dedupe([literal, longish])

    variants = [""]
    for seg in segments:
        if isinstance(seg, tuple):
            variants = [v + opt for v in variants for opt in seg]
        else:
            variants = [v + seg for v in variants]
    return _dedupe(variants)
