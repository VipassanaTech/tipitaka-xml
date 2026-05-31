"""Pali Roman (IAST) normalization shared by the indexer and the query path.

This mirrors — and then closes a gap in — what the production Solr stack does
for Roman text:

  * At **index** time Solr applies ``mapping-FoldToASCII.txt`` (ā→a, ī→i, ṃ→m,
    ṭ→t, …) so the ``text`` field is stored diacritic-folded.
  * At **query** time the custom ``roman`` QParser
    (``RomanExtendedDismaxQParserPlugin``) runs
    ``RomanScriptHelper.removeDiacritcals()`` + ``toLowerCase()``, folding the
    query the same way.

Net effect in production: Roman search is **diacritic-insensitive**
(``sotāpatti`` == ``sotapatti``), but the common ASCII **doubled-vowel**
convention people type when they have no diacritics on their keyboard
(``sotaapatti`` for *sotāpatti*) is **not** handled — it silently misses.

This module makes ``sotaapatti*``, ``sotāpatti*`` and ``sotapatti*`` all
resolve to the same indexed tokens, and additionally understands the Velthuis
ASCII scheme (``.t .d .n .m .l .h "n ~n``).

Two transforms:

``to_iast(raw)``
    ASCII / Velthuis / mixed input → canonical IAST (WITH diacritics). Feed
    this to Aksharamukha so it transliterates to the other 14 scripts
    correctly (Aksharamukha's IAST input expects ``ā``, not ``aa``).

``fold(text)``
    IAST / diacritics → diacritic-folded lowercase ASCII. This is the form the
    ``romn`` query clause matches against; it is byte-for-byte what
    Elasticsearch's ``icu_folding`` produces for the indexed ``text_romn``
    field, so query and index meet in the middle.
"""
from __future__ import annotations

import unicodedata

# Velthuis / ASCII digraphs → IAST. Longest / most specific first so e.g.
# "aa" is consumed before a lone "a" is ever considered.
_VELTHUIS: list[tuple[str, str]] = [
    ("aa", "ā"), ("ii", "ī"), ("uu", "ū"),
    (".t", "ṭ"), (".d", "ḍ"), (".n", "ṇ"), (".m", "ṃ"), (".l", "ḷ"), (".h", "ḥ"),
    ('"n', "ṅ"), ("~n", "ñ"),
]

# IAST diacritic letter → ASCII fold. Superset of
# RomanScriptHelper.removeDiacritcals (adds ḥ/ṁ); equivalent to the subset of
# mapping-FoldToASCII.txt that the Pali corpus actually uses.
_FOLD: dict[str, str] = {
    "ā": "a", "ī": "i", "ū": "u",
    "ṃ": "m", "ṁ": "m", "ṅ": "n", "ñ": "n", "ṇ": "n",
    "ṭ": "t", "ḍ": "d", "ḷ": "l", "ḥ": "h",
}


def to_iast(raw: str) -> str:
    """ASCII/Velthuis Roman → canonical lowercase IAST (with diacritics)."""
    s = unicodedata.normalize("NFC", raw).lower()
    for ascii_form, iast in _VELTHUIS:
        s = s.replace(ascii_form, iast)
    return s


def fold(text: str) -> str:
    """IAST/diacritics → diacritic-folded lowercase ASCII (search form)."""
    s = unicodedata.normalize("NFC", text).lower()
    # Collapse ASCII doubled long vowels so a raw "aa" folds the same as "ā".
    s = s.replace("aa", "a").replace("ii", "i").replace("uu", "u")
    s = "".join(_FOLD.get(ch, ch) for ch in s)
    # Safety net: drop any leftover combining marks (decomposed diacritics).
    return "".join(
        c for c in unicodedata.normalize("NFD", s)
        if unicodedata.category(c) != "Mn"
    )
